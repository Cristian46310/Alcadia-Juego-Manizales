using UnityEngine;
using TMPro;

public class MotoController : MonoBehaviour
{
    [Header("Conexion con Pedales")]
    public PedalInput pedalAcelerador;
    public PedalInput pedalFreno;

    [Header("Conduccion Base")]
    public float aceleracionPorSegundo = 35f;
    public float velocidadMaxima = 150f;
    public float frenadoBasePorSegundo = 12f;
    public float sensibilidadFrenoPorSegundo = 1.5f;
    public float presionMaximaFreno = 3f;
    public float factorDesaceleracionLibrePorVelocidad = 0.1f;

    [Header("Direccion")]
    public FixedJoystick joystick;
    public float sensibilidadGiro = 100f;
    public float inclinacionMaxima = 25f;

    [Header("UI de Velocidad")]
    public TextMeshProUGUI textoVelocidad;

    private Rigidbody rb;
    private float velocidadActualKMH;
    private float presionFrenoActual;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearDamping = 0f;
    }

    void Update()
    {
        ActualizarUI();
    }

    void FixedUpdate()
    {
        ActualizarVelocidad();
        AplicarMovimiento();
        ManejarGiro();
    }

    void ActualizarVelocidad()
    {
        if (pedalAcelerador != null && pedalAcelerador.isPressed)
        {
            presionFrenoActual = 0f;
            float resistenciaAceleracion = (aceleracionPorSegundo / velocidadMaxima) * velocidadActualKMH;
            float aceleracionNeta = aceleracionPorSegundo - resistenciaAceleracion;
            velocidadActualKMH += aceleracionNeta * Time.fixedDeltaTime;
        }
        else if (pedalFreno != null && pedalFreno.isPressed)
        {
            presionFrenoActual = Mathf.Min(
                presionFrenoActual + sensibilidadFrenoPorSegundo * Time.fixedDeltaTime,
                presionMaximaFreno
            );

            float frenadoActualPorSegundo = frenadoBasePorSegundo * presionFrenoActual;
            velocidadActualKMH -= frenadoActualPorSegundo * Time.fixedDeltaTime;
        }
        else
        {
            presionFrenoActual = 0f;
            float desaceleracionLibrePorSegundo = velocidadActualKMH * factorDesaceleracionLibrePorVelocidad;
            velocidadActualKMH -= desaceleracionLibrePorSegundo * Time.fixedDeltaTime;
        }

        velocidadActualKMH = Mathf.Clamp(velocidadActualKMH, 0f, velocidadMaxima);

        if (velocidadActualKMH < 0.05f)
        {
            velocidadActualKMH = 0f;
        }
    }

    void AplicarMovimiento()
    {
        float velocidadActualMS = velocidadActualKMH / 3.6f;
        Vector3 velocidadVertical = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = transform.forward * velocidadActualMS + velocidadVertical;
    }

    void ManejarGiro()
    {
        if (joystick == null)
        {
            return;
        }

        float direccion = joystick.Horizontal;

        if (velocidadActualKMH > 1f)
        {
            float factorVelocidad = Mathf.Clamp01(1f - (velocidadActualKMH / (velocidadMaxima * 1.5f)));
            float sensibilidadReal = Mathf.Lerp(sensibilidadGiro * 0.5f, sensibilidadGiro, factorVelocidad);

            float rotacionY = direccion * sensibilidadReal * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotacionY, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);

            float inclinacionZ = -direccion * inclinacionMaxima;
            Quaternion targetRot = Quaternion.Euler(0f, rb.rotation.eulerAngles.y, inclinacionZ);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.fixedDeltaTime * 10f);
        }
        else
        {
            Quaternion targetRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 5f);
        }
    }

    void ActualizarUI()
    {
        if (textoVelocidad != null)
        {
            textoVelocidad.text = velocidadActualKMH.ToString("F0") + " KM/H";
            textoVelocidad.color = velocidadActualKMH > 100f ? Color.red : Color.white;
        }
    }
}
