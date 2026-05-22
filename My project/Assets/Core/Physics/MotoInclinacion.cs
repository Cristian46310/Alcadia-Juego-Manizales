using UnityEngine;

public class MotoInclinacion : MonoBehaviour
{
    [Header("Inclinacion")]
    public float inclinacionMaxima = 25f;
    public float velocidadSuavizado = 8f;
    public float velocidadRegreso = 12f;

    private float inclinacionActual = 0f;
    private Rigidbody rb;
    private MotoController motoController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        motoController = GetComponent<MotoController>();

        if (rb == null)
            Debug.LogWarning("[MotoInclinacion] No se encontró Rigidbody.");
        if (motoController == null)
            Debug.LogWarning("[MotoInclinacion] No se encontró MotoController.");

        // Permitir rotación en Z para la inclinación
        rb.constraints = RigidbodyConstraints.FreezeRotationX;
    }

    void FixedUpdate()
    {
        if (rb == null || motoController == null) return;

        float direccion = motoController.DireccionActual;
        float velocidad = motoController.VelocidadActual;

        float inclinacionObjetivo = 0f;

        if (velocidad > 1f)
        {
            float escala = Mathf.Clamp01(velocidad / (motoController.velocidadMaxima * 0.3f));
            inclinacionObjetivo = -direccion * inclinacionMaxima * escala;
        }

        float vel = Mathf.Abs(direccion) > 0.05f ? velocidadSuavizado : velocidadRegreso;
        inclinacionActual = Mathf.LerpAngle(inclinacionActual, inclinacionObjetivo, Time.fixedDeltaTime * vel);

        // Aplicar inclinación Z al Rigidbody completo manteniendo la rotación Y actual
        Quaternion rotacionActual = rb.rotation;
        float anguloY = rotacionActual.eulerAngles.y;
        rb.MoveRotation(Quaternion.Euler(0f, anguloY, inclinacionActual));
    }
}