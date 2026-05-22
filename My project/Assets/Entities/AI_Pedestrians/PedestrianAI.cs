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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
        }
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
