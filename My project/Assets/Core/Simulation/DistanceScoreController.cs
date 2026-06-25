using TMPro;
using UnityEngine;

public class DistanceScoreController : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;

    [Header("Puntaje")]
    public float puntosPorMetroBase = 10f;
    public float velocidadMinimaParaSumar = 0.5f;
    public float velocidadMaximaReferencia = 20f;
    [Range(1f, 4f)]
    public float exponenteVelocidad = 2f;
    public float multiplicadorMaximo = 10f;
    public bool soloContarMovimientoHaciaAdelante = true;

    [Header("UI")]
    public TextMeshProUGUI textoPuntaje;
    public string prefijo = "DINERO ";
    public int digitosMinimos = 6;

    private Vector3 posicionAnteriorPlano;
    private float puntajeAcumulado;
    private float metrosAcumulados; // <-- NUEVO

    public static DistanceScoreController Instancia { get; private set; }

    public int PuntajeActual => Mathf.FloorToInt(puntajeAcumulado);
    public float MetrosRecorridos => metrosAcumulados;
    public float KilometrosRecorridos => metrosAcumulados / 1000f;
    public float MultiplicadorActual { get; private set; }

    private void Start()
    {
        if (objetivo == null) objetivo = transform;
        posicionAnteriorPlano = ObtenerPosicionPlano();
        ActualizarUI();
    }

    private void Update()
    {
        if (objetivo == null) return;

        Vector3 posicionActualPlano = ObtenerPosicionPlano();
        Vector3 desplazamientoPlano = posicionActualPlano - posicionAnteriorPlano;
        posicionAnteriorPlano = posicionActualPlano;

        float distanciaValida = CalcularDistanciaValida(desplazamientoPlano);
        if (distanciaValida <= 0f) return;

        metrosAcumulados += distanciaValida; // <-- NUEVO
        puntajeAcumulado += distanciaValida * puntosPorMetroBase * MultiplicadorActual;
        ActualizarUI();
    }

    public void ReiniciarPuntaje()
    {
        puntajeAcumulado = 0f;
        metrosAcumulados = 0f; // <-- NUEVO
        posicionAnteriorPlano = ObtenerPosicionPlano();
        ActualizarUI();
    }

    private Vector3 ObtenerPosicionPlano()
    {
        return Vector3.ProjectOnPlane(objetivo.position, Vector3.up);
    }

    private float CalcularDistanciaValida(Vector3 desplazamientoPlano)
    {
        float distanciaRecorrida = desplazamientoPlano.magnitude;
        if (distanciaRecorrida <= 0f) return 0f;

        float velocidadPlano = distanciaRecorrida / Mathf.Max(Time.deltaTime, 0.0001f);

        if (velocidadPlano < velocidadMinimaParaSumar)
        {
            MultiplicadorActual = 0f;
            return 0f;
        }

        float velocidadNorm = Mathf.Clamp01(
            (velocidadPlano - velocidadMinimaParaSumar) /
            Mathf.Max(velocidadMaximaReferencia - velocidadMinimaParaSumar, 0.01f)
        );

        MultiplicadorActual = Mathf.Clamp(
            Mathf.Pow(velocidadNorm, exponenteVelocidad) * multiplicadorMaximo,
            0.01f,
            multiplicadorMaximo
        );

        if (!soloContarMovimientoHaciaAdelante) return distanciaRecorrida;

        Vector3 forwardPlano = Vector3.ProjectOnPlane(objetivo.forward, Vector3.up);
        if (forwardPlano.sqrMagnitude < 0.001f) return distanciaRecorrida;

        return Mathf.Max(0f, Vector3.Dot(desplazamientoPlano, forwardPlano.normalized));
    }

    private void ActualizarUI()
    {
        if (textoPuntaje == null) return;

        string formatoNumero = digitosMinimos > 0
            ? PuntajeActual.ToString("D" + digitosMinimos)
            : PuntajeActual.ToString();

        textoPuntaje.text = prefijo + formatoNumero;
    }
}