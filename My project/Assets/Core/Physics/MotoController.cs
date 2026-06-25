using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MotoController : MonoBehaviour
{
    [Header("Conexion con Pedales")]
    public PedalInput pedalAcelerador;
    public PedalInput pedalFreno;

    [Header("Conduccion Base")]
    public float aceleracionPorSegundo = 35f;
    public float velocidadMaxima = 150f;
    public float frenadoBasePorSegundo = 17f;
    public float sensibilidadFrenoPorSegundo = 1.5f;
    public float presionMaximaFreno = 3f;
    public float factorDesaceleracionLibrePorVelocidad = 0.1f;

    [Header("Direccion")]
    public FixedJoystick joystick;
    public float sensibilidadGiro = 100f;
    public float inclinacionMaxima = 25f;

    [Header("Modelo Visual")]
    public Transform modeloVisual;

    [Header("UI de Velocidad")]
    public TextMeshProUGUI textoVelocidad;

    private Rigidbody rb;
    private float velocidadActualKMH;
    private float presionFrenoActual;
    public float DireccionActual { get; private set; }
    public float VelocidadActual => velocidadActualKMH;
    private float inclinacionActual = 0f;
    private Quaternion modeloVisualRestLocalRotation;
    private bool conduccionForzada;
    private float velocidadForzadaObjetivoKMH = 75f;
    private float direccionForzadaJoystick;

    public float VelocidadActualKMH => velocidadActualKMH;
    public bool ConduccionForzadaActiva => conduccionForzada;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[MotoController] No se encontr? Rigidbody en este GameObject.");
        }
        else
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearDamping = 0f;
        }

        // Si no se asign? en el Inspector, busca el primer hijo con un MeshRenderer
        if (modeloVisual == null)
        {
            MeshRenderer mesh = GetComponentInChildren<MeshRenderer>();
            if (mesh != null)
            {
                modeloVisual = mesh.transform;
                Debug.Log($"[MotoController] modeloVisual asignado autom?ticamente a: {modeloVisual.name}");
            }
            else
            {
                Debug.LogWarning("[MotoController] No se encontr? modeloVisual. As?gnalo en el Inspector.");
            }
        }
        // Guardar rotaci?n local base del modelo visual (si existe)
        if (modeloVisual != null)
            modeloVisualRestLocalRotation = modeloVisual.localRotation;
        else
            modeloVisualRestLocalRotation = Quaternion.identity;
    }

    void Update()
    {
        ActualizarUI();
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null
            && Keyboard.current.tKey.wasPressedThisFrame
            && modeloVisual != null)
#else
        if (Input.GetKeyDown(KeyCode.T) && modeloVisual != null)
#endif
        {
            Vector3 axisLocal = modeloVisual.InverseTransformDirection(transform.forward).normalized;
            modeloVisual.localRotation = modeloVisualRestLocalRotation * Quaternion.AngleAxis(25f, axisLocal);
            Debug.Log($"[MotoController] Debug tilt aplicado. axisLocal={axisLocal}");
        }
    }

    void FixedUpdate()
    {
        ActualizarVelocidad();
        AplicarMovimiento();
        ManejarGiro();
    }

    public void ActivarConduccionForzada(float velocidadObjetivoKmh, float direccionJoystick = 0f)
    {
        conduccionForzada = true;
        velocidadForzadaObjetivoKMH = Mathf.Max(10f, velocidadObjetivoKmh);
        direccionForzadaJoystick = Mathf.Clamp(direccionJoystick, -1f, 1f);
        presionFrenoActual = 0f;
    }

    public void DesactivarConduccionForzada()
    {
        if (!conduccionForzada)
        {
            return;
        }

        conduccionForzada = false;
        direccionForzadaJoystick = 0f;
        presionFrenoActual = 0f;

        if (pedalAcelerador != null)
        {
            pedalAcelerador.isPressed = false;
        }
    }

    void ActualizarVelocidad()
    {
        if (conduccionForzada)
        {
            presionFrenoActual = 0f;
            float diferencia = velocidadForzadaObjetivoKMH - velocidadActualKMH;
            float aceleracionForzada = Mathf.Sign(diferencia) *
                Mathf.Min(Mathf.Abs(diferencia), aceleracionPorSegundo * 2f);
            velocidadActualKMH += aceleracionForzada * Time.fixedDeltaTime;
            velocidadActualKMH = Mathf.Clamp(velocidadActualKMH, 0f, velocidadMaxima);
            return;
        }

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
            velocidadActualKMH = 0f;
    }

    void AplicarMovimiento()
    {
        if (rb == null) return;

        float velocidadActualMS = velocidadActualKMH / 3.6f;
        Vector3 velocidadVertical = Vector3.Project(rb.linearVelocity, Vector3.up);
        rb.linearVelocity = transform.forward * velocidadActualMS + velocidadVertical;
    }

    void ManejarGiro()
    {
        DireccionActual = 0f;

        if (joystick == null && !conduccionForzada) return;

        float direccion = conduccionForzada ? direccionForzadaJoystick : joystick.Horizontal;
        DireccionActual = direccion;

        if (rb == null) return;

        if (velocidadActualKMH > 1f)
        {
            // 1. Giro Y del Rigidbody (Rotaci?n sobre el suelo)
            float factorVelocidad = Mathf.Clamp01(1f - (velocidadActualKMH / (velocidadMaxima * 1.5f)));
            float sensibilidadReal = Mathf.Lerp(sensibilidadGiro * 0.2f, sensibilidadGiro * 0.35f, factorVelocidad);
            float rotacionY = direccion * sensibilidadReal * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, rotacionY, 0f));
        }
    }

    void AplicarInclinacion(float angulo)
    {
        if (modeloVisual == null) return;

        // NOTA: Si tu modelo visual se inclina hacia adelante/atr?s en lugar de los lados, 
        // cambia "Vector3.forward" por "Vector3.right" (Eje X)
        Quaternion rotacionInclinada = Quaternion.AngleAxis(angulo, Vector3.forward);

        // Combinamos la rotaci?n inicial de f?brica del modelo con la nueva inclinaci?n en su propio eje
        modeloVisual.localRotation = modeloVisualRestLocalRotation * rotacionInclinada;
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