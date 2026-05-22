using TMPro;
using UnityEngine;

public class DistanceScoreController : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform objetivo;

    [Header("Puntaje")]
    public float puntosPorMetro = 10f;
    public float velocidadMinimaParaSumar = 0.5f;
    public bool soloContarMovimientoHaciaAdelante = true;

    [Header("UI")]
    public TextMeshProUGUI textoPuntaje;
    public string prefijo = "SCORE ";
    public int digitosMinimos = 6;

    private Vector3 posicionAnteriorPlano;
    private float puntajeAcumulado;

    public static DistanceScoreController Instancia { get; private set; }

    public int PuntajeActual => Mathf.FloorToInt(puntajeAcumulado);

    public float MetrosRecorridos =>
        puntosPorMetro > 0f ? puntajeAcumulado / puntosPorMetro : 0f;

    public float KilometrosRecorridos => MetrosRecorridos / 1000f;

    private void Awake()
    {
        Instancia = this;
    }

    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }

    private void Start()
    {
        if (objetivo == null)
        {
            objetivo = transform;
        }

        posicionAnteriorPlano = ObtenerPosicionPlano();
        ActualizarUI();
    }

    private void Update()
    {
        if (objetivo == null)
        {
            return;
        }

        Vector3 posicionActualPlano = ObtenerPosicionPlano();
        Vector3 desplazamientoPlano = posicionActualPlano - posicionAnteriorPlano;
        posicionAnteriorPlano = posicionActualPlano;

        float distanciaValida = CalcularDistanciaValida(desplazamientoPlano);

        if (distanciaValida <= 0f)
        {
            return;
        }

        puntajeAcumulado += distanciaValida * puntosPorMetro;
        ActualizarUI();
    }

    public void ReiniciarPuntaje()
    {
        puntajeAcumulado = 0f;
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

        if (distanciaRecorrida <= 0f)
        {
            return 0f;
        }

        float velocidadPlano = distanciaRecorrida / Mathf.Max(Time.deltaTime, 0.0001f);

        if (velocidadPlano < velocidadMinimaParaSumar)
        {
            return 0f;
        }

        if (!soloContarMovimientoHaciaAdelante)
        {
            return distanciaRecorrida;
        }

        Vector3 forwardPlano = Vector3.ProjectOnPlane(objetivo.forward, Vector3.up);

        if (forwardPlano.sqrMagnitude < 0.001f)
        {
            return distanciaRecorrida;
        }

        return Mathf.Max(0f, Vector3.Dot(desplazamientoPlano, forwardPlano.normalized));
    }

    private void ActualizarUI()
    {
        if (textoPuntaje == null)
        {
            return;
        }

        string formatoNumero = digitosMinimos > 0
            ? PuntajeActual.ToString("D" + digitosMinimos)
            : PuntajeActual.ToString();

        textoPuntaje.text = prefijo + formatoNumero;
    }
}
