using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PedestrianAI : MonoBehaviour
{
    [Tooltip("Punto donde aparece el peatón")]
    public Transform startPoint;

    [Tooltip("Destino al otro lado del cruce")]
    public Transform endPoint;

    [Tooltip("Referencia al Crosswalk que regula este cruce (opcional)")]
    public Crosswalk crosswalk;

    public float speed = 1.6f;
    public float rotateSpeed = 6f;
    public float stopDistance = 0.2f;
    public float waitAtEnd = 2f;
    public float waitAtStart = 2f;
    public bool loopCrossing = true;

    private Rigidbody rb;
    private bool movingToEnd = true;
    private float waitTimer = 0f;

    private void Awake()
    {
        PrepararColisionFinPartida();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
        }
    }

    /// <summary>
    /// Collider sólido + tag Peaton para que ChoqueFinDePartida detecte el impacto.
    /// </summary>
    public void PrepararColisionFinPartida()
    {
        if (!CompareTag("Peaton"))
        {
            gameObject.tag = "Peaton";
        }

        if (GetComponent<EntidadChoqueFin>() == null)
        {
            gameObject.AddComponent<EntidadChoqueFin>();
        }

        AsegurarColliderSolido();

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 70f;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void AsegurarColliderSolido()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                return;
            }
        }

        var capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.height = 1.75f;
        capsule.radius = 0.32f;
        capsule.center = new Vector3(0f, 0.875f, 0f);
        capsule.isTrigger = false;
    }

    private void FixedUpdate()
    {
        Transform targetPoint = movingToEnd ? endPoint : startPoint;
        if (targetPoint == null) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (crosswalk != null && !crosswalk.CanPedestriansCross())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 dir = Vector3.ProjectOnPlane(targetPoint.position - transform.position, Vector3.up);
        float dist = dir.magnitude;
        if (dist <= stopDistance)
        {
            rb.linearVelocity = Vector3.zero;
            if (loopCrossing)
            {
                waitTimer = movingToEnd ? waitAtEnd : waitAtStart;
                movingToEnd = !movingToEnd;
            }
            return;
        }

        Vector3 desired = dir.normalized;
        Quaternion targetRot = Quaternion.LookRotation(desired, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));

        Vector3 velUp = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = transform.forward * speed + velUp;
    }
}
