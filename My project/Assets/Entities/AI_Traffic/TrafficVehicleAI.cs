using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrafficVehicleAI : MonoBehaviour
{
    [Header("Carril")]
    public LanePath lanePath;
    public int waypointInicial;
    public float distanciaCambioWaypoint = 3f;
    public bool destruirAlFinalDelCarril = true;

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

        // SI LA BANDERA ES FALSE: Significa que arrastraste el coche a mano en el editor
        if (lanePath != null && lanePath.PointCount > 0)
        {
            waypointActual = Mathf.Clamp(waypointInicial, 0, lanePath.PointCount - 1);
            transform.position = lanePath.GetPoint(waypointActual);
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

    /// <summary>
    /// Llamado por el spawner. Asigna el carril y el punto de inicio correcto.
    /// </summary>
    public void ConfigurarCarril(LanePath nuevoCarril, int indiceInicial = 0)
    {
        lanePath = nuevoCarril;

        if (lanePath == null || lanePath.PointCount == 0)
        {
            return;
        }

        // Activamos la bandera para bloquear el Start() de Unity
        carrilConfiguradoExternamente = true;

        waypointActual = Mathf.Clamp(indiceInicial, 0, lanePath.PointCount - 1);

        // Teletransporta el vehículo al punto correcto del carril (ej: 30)
        transform.position = lanePath.GetPoint(waypointActual);

        // Orienta el vehículo hacia el siguiente waypoint desde el inicio (ej: hacia el 31)
        int siguienteIndice = lanePath.GetNextIndex(waypointActual);
        Vector3 direccionInicial = lanePath.GetPoint(siguienteIndice) - transform.position;
        direccionInicial = Vector3.ProjectOnPlane(direccionInicial, Vector3.up);

        if (direccionInicial.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direccionInicial, Vector3.up);
        }
    }

    public void ConfigurarVelocidadObjetivo(float nuevaVelocidadKMH)
    {
        velocidadObjetivo = Mathf.Max(0f, nuevaVelocidadKMH);
    }

    private void ActualizarWaypoint()
    {
        Vector3 objetivo = lanePath.GetPoint(waypointActual);
        Vector3 desplazamientoPlano = Vector3.ProjectOnPlane(objetivo - transform.position, Vector3.up);

        if (desplazamientoPlano.magnitude > distanciaCambioWaypoint)
        {
            return;
        }

        bool llegoAlUltimo = waypointActual >= lanePath.PointCount - 1;

        if (llegoAlUltimo && !lanePath.loop)
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
        Vector3 objetivo = lanePath.GetPoint(waypointActual);
        Vector3 direccionPlano = Vector3.ProjectOnPlane(objetivo - transform.position, Vector3.up).normalized;

        if (direccionPlano.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionPlano, Vector3.up);
            Quaternion nuevaRotacion = Quaternion.Slerp(
                rb.rotation,
                rotacionObjetivo,
                velocidadGiro * Time.fixedDeltaTime
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

            if (!hitEncontrado || hit.distance < mejorHit.distance)
            {
                mejorHit = hit;
                hitEncontrado = true;
            }
        }

        if (!hitEncontrado) return velocidadObjetivo;
        RaycastHit objetivo = mejorHit;

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origenSensor = transform.position + Vector3.up * alturaSensor;
        Gizmos.DrawWireSphere(origenSensor + transform.forward * largoSensor, radioSensor);
        Gizmos.DrawLine(origenSensor, origenSensor + transform.forward * largoSensor);
    }
}