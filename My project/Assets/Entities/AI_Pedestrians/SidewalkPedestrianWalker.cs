using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SidewalkPedestrianWalker : MonoBehaviour
{
    [Header("Ruta")]
    public LanePath ruta;
    public int waypointInicial = 0;
    public bool loop = false;
    public bool irYVolver = false;

    [Header("Movimiento")]
    public float speed = 1.4f;
    public float rotateSpeed = 8f;
    public float stopDistance = 0.15f;
    public float alturaSobreSuelo = 0f;

    private Rigidbody rb;
    private int waypointActual;
    private bool avanzando = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Start()
    {
        if (ruta == null || ruta.PointCount == 0)
        {
            enabled = false;
            return;
        }

        waypointActual = Mathf.Clamp(waypointInicial, 0, ruta.PointCount - 1);
        Vector3 posicion = ruta.GetPoint(waypointActual);
        posicion.y += alturaSobreSuelo;
        transform.position = posicion;
        transform.rotation = ruta.GetRotacionEnPosicion(transform.position);
    }

    private void FixedUpdate()
    {
        if (ruta == null || ruta.PointCount == 0)
        {
            return;
        }

        int siguienteIndice = ObtenerSiguienteIndice();
        Vector3 objetivo = ruta.GetPoint(siguienteIndice);
        objetivo.y += alturaSobreSuelo;

        Vector3 direccion = Vector3.ProjectOnPlane(objetivo - transform.position, Vector3.up);
        float distancia = direccion.magnitude;

        if (distancia <= stopDistance)
        {
            AvanzarWaypoint();
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 direccionNormalizada = direccion.normalized;
        Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionNormalizada, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacionObjetivo, rotateSpeed * Time.fixedDeltaTime));

        Vector3 velocidadVertical = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = direccionNormalizada * speed + velocidadVertical;
    }

    private int ObtenerSiguienteIndice()
    {
        if (ruta.PointCount <= 1)
        {
            return 0;
        }

        if (irYVolver)
        {
            return avanzando
                ? Mathf.Min(waypointActual + 1, ruta.PointCount - 1)
                : Mathf.Max(waypointActual - 1, 0);
        }

        return ruta.GetNextIndex(waypointActual);
    }

    private void AvanzarWaypoint()
    {
        if (ruta.PointCount <= 1)
        {
            return;
        }

        if (irYVolver)
        {
            if (avanzando)
            {
                if (waypointActual >= ruta.PointCount - 1)
                {
                    avanzando = false;
                    waypointActual = Mathf.Max(ruta.PointCount - 2, 0);
                }
                else
                {
                    waypointActual++;
                }
            }
            else
            {
                if (waypointActual <= 0)
                {
                    avanzando = true;
                    waypointActual = Mathf.Min(1, ruta.PointCount - 1);
                }
                else
                {
                    waypointActual--;
                }
            }

            return;
        }

        waypointActual = ruta.GetNextIndex(waypointActual);
        if (!loop && waypointActual >= ruta.PointCount - 1)
        {
            enabled = false;
            rb.linearVelocity = Vector3.zero;
        }
    }
}