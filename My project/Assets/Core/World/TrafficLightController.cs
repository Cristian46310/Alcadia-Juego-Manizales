using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public enum TrafficLightState
    {
        Red,
        Yellow,
        Green
    }

    [Header("Estado")]
    public TrafficLightState estadoInicial = TrafficLightState.Red;
    public bool iniciarAutomaticamente = true;
    public float duracionRojo = 8f;
    public float duracionAmarillo = 2f;
    public float duracionVerde = 8f;

    [Header("Luces de Unity")]
    public Light luzRoja;
    public Light luzAmarilla;
    public Light luzVerde;
    public float intensidadActiva = 8f;
    public float intensidadInactiva = 0f;

    [Header("Lentes del Modelo")]
    public Renderer lenteRojo;
    public Renderer lenteAmarillo;
    public Renderer lenteVerde;
    public Color colorRojo = new(1f, 0.15f, 0.15f);
    public Color colorAmarillo = new(1f, 0.75f, 0.1f);
    public Color colorVerde = new(0.2f, 1f, 0.25f);
    public Color colorApagado = new(0.08f, 0.08f, 0.08f);
    public float emisionActiva = 3f;
    public float emisionApagada = 0f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private readonly MaterialPropertyBlock propertyBlock = new();
    private TrafficLightState estadoActual;
    private float temporizadorEstado;

    public TrafficLightState EstadoActual => estadoActual;
    public bool DebeDetenerVehiculos => estadoActual != TrafficLightState.Green;

    private void Start()
    {
        AplicarEstadoVisual(estadoInicial);

        if (!iniciarAutomaticamente)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        temporizadorEstado += Time.deltaTime;

        if (temporizadorEstado < ObtenerDuracionActual())
        {
            return;
        }

        AvanzarEstadoAutomatico();
    }

    public void SetState(TrafficLightState nuevoEstado)
    {
        AplicarEstadoVisual(nuevoEstado);
    }

    public void SetAutomaticoActivo(bool activo)
    {
        iniciarAutomaticamente = activo;
        enabled = activo;
    }

    private void AvanzarEstadoAutomatico()
    {
        switch (estadoActual)
        {
            case TrafficLightState.Red:
                AplicarEstadoVisual(TrafficLightState.Green);
                break;
            case TrafficLightState.Yellow:
                AplicarEstadoVisual(TrafficLightState.Red);
                break;
            default:
                AplicarEstadoVisual(TrafficLightState.Yellow);
                break;
        }
    }

    private float ObtenerDuracionActual()
    {
        return estadoActual switch
        {
            TrafficLightState.Red => duracionRojo,
            TrafficLightState.Yellow => duracionAmarillo,
            _ => duracionVerde
        };
    }

    private void AplicarEstadoVisual(TrafficLightState nuevoEstado)
    {
        estadoActual = nuevoEstado;
        temporizadorEstado = 0f;

        bool rojoActivo = nuevoEstado == TrafficLightState.Red;
        bool amarilloActivo = nuevoEstado == TrafficLightState.Yellow;
        bool verdeActivo = nuevoEstado == TrafficLightState.Green;

        AplicarLuz(luzRoja, rojoActivo, colorRojo);
        AplicarLuz(luzAmarilla, amarilloActivo, colorAmarillo);
        AplicarLuz(luzVerde, verdeActivo, colorVerde);

        AplicarLente(lenteRojo, rojoActivo, colorRojo);
        AplicarLente(lenteAmarillo, amarilloActivo, colorAmarillo);
        AplicarLente(lenteVerde, verdeActivo, colorVerde);
    }

    private void AplicarLuz(Light luz, bool activa, Color color)
    {
        if (luz == null)
        {
            return;
        }

        luz.color = color;
        luz.intensity = activa ? intensidadActiva : intensidadInactiva;
        luz.enabled = activa || intensidadInactiva > 0f;
    }

    private void AplicarLente(Renderer rendererObjetivo, bool activo, Color colorActivo)
    {
        if (rendererObjetivo == null)
        {
            return;
        }

        rendererObjetivo.GetPropertyBlock(propertyBlock);

        Color colorFinal = activo ? colorActivo : colorApagado;
        Color emisionFinal = colorActivo * (activo ? emisionActiva : emisionApagada);

        propertyBlock.SetColor(BaseColorId, colorFinal);
        propertyBlock.SetColor(ColorId, colorFinal);
        propertyBlock.SetColor(EmissionColorId, emisionFinal);

        rendererObjetivo.SetPropertyBlock(propertyBlock);
    }
}
