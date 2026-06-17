using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficVehicleAI : MonoBehaviour
{
    [Header("Carril")]
    public LanePath lanePath;
    public int waypointInicial;
    public float distanciaCambioWaypoint = 3f;
    public bool destruirAlFinalDelCarril;
    [Tooltip("Desplazamiento lateral respecto al centro del carril (coches de la torre en doble fila).")]
    public float offsetLateralCarril;

    [Header("Movimiento")]
    public float velocidadObjetivo = 45f;
    public float velocidadActualKMH;
    public float aceleracionPorSegundo = 10f;
    public float frenadoPorSegundo = 18f;
    public float velocidadGiro = 6f;

    [Header("Sensor")]
    public float largoSensor = 12f;
    public float radioSensor = 0.8f;
    public float alturaSensor = 0.8f;
    public LayerMask capasObstaculo = ~0;
    public float distanciaMinima = 2.5f;
    public float sensorExtraPorKMH = 0.18f;
    public float sensorMaxDistance = 32f;
    public float stoppingMargin = 3f;

    [Header("Moto (jugador)")]
    [Tooltip("Tráfico normal: se detiene a esta distancia de la moto para no hacerte perder.")]
    public float distanciaDetenerAnteMoto = 10f;
    [Tooltip("Metros extra antes de la distancia de parada donde ya empieza a frenar.")]
    public float zonaFrenadoAnteMoto = 8f;

    private Rigidbody rb;
    private int waypointActual;

    // El "escudo" para saber si el Spawner ya configuró este coche
    private bool carrilConfiguradoExternamente = false;

    public LanePath Lane => lanePath;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        // SI LA BANDERA ES TRUE: Ignora este método porque el Spawner ya le dio su waypoint real
        if (carrilConfiguradoExternamente) return;

        if (lanePath != null && lanePath.PointCount > 0)
        {
            InicializarDesdeCarril(mantenerPosicionSiCerca: true);
        }
    }

    private void FixedUpdate()
    {
        if (lanePath == null || lanePath.PointCount == 0)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        ActualizarWaypoint();
        MoverVehiculo();
    }

    private VehiculoCruceTorre cruceTorreCache;

    private VehiculoCruceTorre CruceTorre
    {
        get
        {
            if (cruceTorreCache == null)
            {
                cruceTorreCache = GetComponent<VehiculoCruceTorre>();
            }

            return cruceTorreCache;
        }
    }

    /// <summary>
    /// Llamado por el spawner. Asigna el carril y el punto de inicio correcto.
    /// </summary>
    public void ConfigurarCarril(LanePath nuevoCarril, int indiceInicial = 0, bool mantenerPosicionSiCerca = false)
    {
        lanePath = nuevoCarril;
        carrilConfiguradoExternamente = true;

        if (lanePath == null || lanePath.PointCount == 0)
        {
            return;
        }

        waypointInicial = indiceInicial;
        InicializarDesdeCarril(mantenerPosicionSiCerca);
    }

    private void InicializarDesdeCarril(bool mantenerPosicionSiCerca)
    {
        waypointActual = mantenerPosicionSiCerca
            ? lanePath.GetIndiceWaypointMasCercano(transform.position)
            : Mathf.Clamp(waypointInicial, 0, lanePath.PointCount - 1);

        Vector3 puntoCarril = lanePath.GetPoint(waypointActual);
        bool quedarseEnSitio = mantenerPosicionSiCerca &&
            Vector3.Distance(Plano(transform.position), Plano(puntoCarril)) < 15f;

        if (!quedarseEnSitio)
        {
            transform.position = puntoCarril;
        }

        OrientarHaciaSiguienteWaypoint();
    }

    private void OrientarHaciaSiguienteWaypoint()
    {
        if (lanePath == null || lanePath.PointCount == 0)
        {
            return;
        }

        int siguienteIndice = lanePath.GetNextIndex(waypointActual);
        Vector3 direccionInicial = lanePath.GetPoint(siguienteIndice) - transform.position;
        direccionInicial = Vector3.ProjectOnPlane(direccionInicial, Vector3.up);

        if (direccionInicial.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direccionInicial, Vector3.up);
        }
    }

    private static Vector3 Plano(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

    public void ConfigurarVelocidadObjetivo(float nuevaVelocidadKMH)
    {
        velocidadObjetivo = Mathf.Max(0f, nuevaVelocidadKMH);
    }

    /// <summary>
    /// Elige el waypoint más adelante hacia la torre (coches del cruce).
    /// </summary>
    public void ApuntarWaypointHacia(Vector3 destinoMundo)
    {
        if (lanePath == null || lanePath.PointCount == 0)
        {
            return;
        }

        Vector3 dirDestino = Vector3.ProjectOnPlane(destinoMundo - transform.position, Vector3.up);
        if (dirDestino.sqrMagnitude < 0.01f)
        {
            return;
        }

        dirDestino.Normalize();
        int mejor = waypointActual;
        float mejorDot = float.NegativeInfinity;

        for (int i = 0; i < lanePath.PointCount; i++)
        {
            Vector3 alPunto = Vector3.ProjectOnPlane(lanePath.GetPoint(i) - transform.position, Vector3.up);
            if (alPunto.sqrMagnitude < 4f)
            {
                continue;
            }

            float dot = Vector3.Dot(dirDestino, alPunto.normalized);
            if (dot > mejorDot)
            {
                mejorDot = dot;
                mejor = i;
            }
        }

        waypointActual = mejor;
        cruceTorreCache = null;
    }

    /// <summary>
    /// Coloca el vehículo en el carril sin teletransportar al waypoint más cercano.
    /// </summary>
    public void ColocarEnCarril(LanePath carril, int indiceWaypoint, Vector3 posicion, Quaternion rotacion)
    {
        lanePath = carril;
        carrilConfiguradoExternamente = true;

        if (lanePath == null || lanePath.PointCount == 0)
        {
            return;
        }

        waypointActual = Mathf.Clamp(indiceWaypoint, 0, lanePath.PointCount - 1);
        transform.SetPositionAndRotation(posicion, rotacion);
        velocidadActualKMH = 0f;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void ActualizarWaypointEnCarril(int indiceWaypoint)
    {
        if (lanePath == null || lanePath.PointCount == 0)
        {
            return;
        }

        waypointActual = Mathf.Clamp(indiceWaypoint, 0, lanePath.PointCount - 1);
        if (waypointActual >= lanePath.PointCount - 1 && lanePath.PointCount > 1)
        {
            waypointActual = lanePath.PointCount - 2;
        }
    }

    private Vector3 GetPuntoObjetivoCarril(int indiceWaypoint)
    {
        Vector3 centro = lanePath.GetPoint(indiceWaypoint);
        if (Mathf.Abs(offsetLateralCarril) < 0.01f)
        {
            return centro;
        }

        int siguiente = lanePath.GetNextIndex(indiceWaypoint);
        Vector3 adelante = Vector3.ProjectOnPlane(lanePath.GetPoint(siguiente) - centro, Vector3.up);
        if (adelante.sqrMagnitude < 0.001f)
        {
            return centro;
        }

        Vector3 lateral = Vector3.Cross(Vector3.up, adelante.normalized);
        return centro + lateral * offsetLateralCarril;
    }

    private void ActualizarWaypoint()
    {
        Vector3 objetivo = GetPuntoObjetivoCarril(waypointActual);
        Vector3 desplazamientoPlano = Vector3.ProjectOnPlane(objetivo - transform.position, Vector3.up);

        if (desplazamientoPlano.magnitude > distanciaCambioWaypoint)
        {
            return;
        }

        bool llegoAlUltimo = waypointActual >= lanePath.PointCount - 1;
        bool esCocheTorre = CruceTorre != null;
        bool finDeRuta = llegoAlUltimo && (!lanePath.loop || esCocheTorre);

        if (finDeRuta)
        {
            if (destruirAlFinalDelCarril)
            {
                Destroy(gameObject);
            }

            return;
        }

        waypointActual = lanePath.GetNextIndex(waypointActual);
    }

    private void MoverVehiculo()
    {
        Vector3 objetivo = GetPuntoObjetivoCarril(waypointActual);
        Vector3 direccionPlano = Vector3.ProjectOnPlane(objetivo - transform.position, Vector3.up).normalized;

        if (direccionPlano.sqrMagnitude > 0.001f)
        {
            float giro = velocidadGiro;
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionPlano, Vector3.up);
            Quaternion nuevaRotacion = Quaternion.Slerp(
                rb.rotation,
                rotacionObjetivo,
                giro * Time.fixedDeltaTime
            );
            rb.MoveRotation(nuevaRotacion);
        }

        float velocidadDeseadaKMH = CalcularVelocidadDeseada();
        float cambioPorSegundo = velocidadDeseadaKMH >= velocidadActualKMH
            ? aceleracionPorSegundo
            : frenadoPorSegundo;

        velocidadActualKMH = Mathf.MoveTowards(
            velocidadActualKMH,
            velocidadDeseadaKMH,
            cambioPorSegundo * Time.fixedDeltaTime
        );

        Vector3 velocidadVertical = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = transform.forward * (velocidadActualKMH / 3.6f) + velocidadVertical;
    }

    private float CalcularVelocidadDeseada()
    {
        float sensorDistance = GetDynamicSensorDistance();
        Vector3 origenSensor = transform.position + Vector3.up * alturaSensor;
        RaycastHit[] hits = Physics.SphereCastAll(
            origenSensor,
            radioSensor,
            transform.forward,
            sensorDistance,
            capasObstaculo,
            QueryTriggerInteraction.Collide);

        RaycastHit mejorHit = default;
        bool hitEncontrado = false;

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.attachedRigidbody == rb) continue; // Ignora el propio vehículo
            if (hit.distance <= 0f) continue;

            if (CruceTorre != null && CruceTorre.ignorarMotoEnSensor &&
                hit.collider.GetComponentInParent<MotoController>() != null)
            {
                continue;
            }

            if (CruceTorre != null &&
                hit.collider.GetComponentInParent<VehiculoCruceTorre>() != null)
            {
                continue;
            }

            if (!hitEncontrado || hit.distance < mejorHit.distance)
            {
                mejorHit = hit;
                hitEncontrado = true;
            }
        }

        if (!hitEncontrado) return velocidadObjetivo;
        RaycastHit objetivo = mejorHit;

        if (CruceTorre == null && EsColliderMoto(objetivo.collider))
        {
            return GetSpeedForMoto(objetivo.distance);
        }

        TrafficLightObstacle semaforo = objetivo.collider.GetComponent<TrafficLightObstacle>();
        if (semaforo != null)
        {
            if (!semaforo.DebeDetenerse())
            {
                return velocidadObjetivo;
            }
            return GetSpeedForStoppingObstacle(objetivo.distance);
        }

        if (objetivo.distance <= distanciaMinima) return 0f;

        float safeDistance = GetSafeStoppingDistance(objetivo);
        if (objetivo.distance <= safeDistance) return 0f;

        float factorDistancia = Mathf.InverseLerp(safeDistance, sensorDistance, objetivo.distance);
        return velocidadObjetivo * factorDistancia;
    }

    private float GetDynamicSensorDistance()
    {
        float velocidadActualMS = rb.linearVelocity.magnitude;
        float extra = rb.linearVelocity.magnitude * 3.6f * sensorExtraPorKMH;
        return Mathf.Clamp(largoSensor + extra, largoSensor, sensorMaxDistance);
    }

    private float GetSafeStoppingDistance(RaycastHit hit)
    {
        float velocidadActualMS = rb.linearVelocity.magnitude;
        float desaceleracionMS = Mathf.Max(0.01f, frenadoPorSegundo / 3.6f);
        float stoppingDistance = velocidadActualMS * velocidadActualMS / (2f * desaceleracionMS);

        float objetivoVelocidadMS = 0f;
        float hitSpeedMS = 0f;
        if (hit.rigidbody != null)
        {
            hitSpeedMS = Vector3.Project(hit.rigidbody.linearVelocity, transform.forward).magnitude;
        }

        float relativeSpeed = Mathf.Max(0f, velocidadActualMS - hitSpeedMS);
        float relativeStoppingDistance = relativeSpeed * relativeSpeed / (2f * desaceleracionMS);

        float safeDistance = Mathf.Max(distanciaMinima, stoppingDistance, relativeStoppingDistance) + stoppingMargin;
        return safeDistance;
    }

    private float GetSpeedForStoppingObstacle(float distance)
    {
        float safeDistance = GetSafeStoppingDistance(default);
        if (distance <= safeDistance) return 0f;
        float sensorDistance = GetDynamicSensorDistance();
        return velocidadObjetivo * Mathf.InverseLerp(safeDistance, sensorDistance, distance);
    }

    private static bool EsColliderMoto(Collider collider)
    {
        return collider != null && collider.GetComponentInParent<MotoController>() != null;
    }

    private float GetSpeedForMoto(float distance)
    {
        float detener = Mathf.Max(distanciaDetenerAnteMoto, distanciaMinima);
        float inicioFrenado = detener + Mathf.Max(zonaFrenadoAnteMoto, stoppingMargin);

        if (distance <= detener)
        {
            return 0f;
        }

        if (distance <= inicioFrenado)
        {
            return velocidadObjetivo * Mathf.InverseLerp(detener, inicioFrenado, distance);
        }

        float sensorDistance = GetDynamicSensorDistance();
        return velocidadObjetivo * Mathf.InverseLerp(inicioFrenado, sensorDistance, distance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origenSensor = transform.position + Vector3.up * alturaSensor;
        Gizmos.DrawWireSphere(origenSensor + transform.forward * largoSensor, radioSensor);
        Gizmos.DrawLine(origenSensor, origenSensor + transform.forward * largoSensor);
    }
}