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
    public float aceleracionPorSegundo = 10f;
    public float frenadoPorSegundo = 18f;
    public float velocidadGiro = 6f;

    [Header("Sensor")]
    public float largoSensor = 12f;
    public float radioSensor = 0.8f;
    public float alturaSensor = 0.8f;
    public LayerMask capasObstaculo = ~0;
    public float distanciaMinima = 2.5f;

    private Rigidbody rb;
    private int waypointActual;
    private float velocidadActualKMH;

    public LanePath Lane => lanePath;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        waypointActual = Mathf.Max(0, waypointInicial);

        if (lanePath != null && lanePath.PointCount > 0)
        {
            transform.position = lanePath.GetPoint(Mathf.Clamp(waypointActual, 0, lanePath.PointCount - 1));
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

    public void ConfigurarCarril(LanePath nuevoCarril, int nuevoWaypoint = 0)
    {
        lanePath = nuevoCarril;
        waypointActual = Mathf.Max(0, nuevoWaypoint);

        if (lanePath != null && lanePath.PointCount > 0)
        {
            Vector3 punto = lanePath.GetPoint(waypointActual);
            transform.position = punto;
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
        float cambioPorSegundo = velocidadDeseadaKMH >= velocidadActualKMH ? aceleracionPorSegundo : frenadoPorSegundo;
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
        Vector3 origenSensor = transform.position + Vector3.up * alturaSensor;

        if (!Physics.SphereCast(origenSensor, radioSensor, transform.forward, out RaycastHit hit, largoSensor, capasObstaculo, QueryTriggerInteraction.Ignore))
        {
            return velocidadObjetivo;
        }

        if (hit.rigidbody == rb)
        {
            return velocidadObjetivo;
        }

        if (hit.distance <= distanciaMinima)
        {
            return 0f;
        }

        float factorDistancia = Mathf.InverseLerp(distanciaMinima, largoSensor, hit.distance);
        return velocidadObjetivo * factorDistancia;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origenSensor = transform.position + Vector3.up * alturaSensor;
        Gizmos.DrawWireSphere(origenSensor + transform.forward * largoSensor, radioSensor);
        Gizmos.DrawLine(origenSensor, origenSensor + transform.forward * largoSensor);
    }
}
