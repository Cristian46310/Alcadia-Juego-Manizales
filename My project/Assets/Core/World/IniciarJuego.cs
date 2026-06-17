using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
#endif

/// <summary>
/// Botón de menú inicial: limpia el puntaje y carga la escena de juego (Test).
/// </summary>
[DisallowMultipleComponent]
public class IniciarJuego : MonoBehaviour
{
    [SerializeField] private string escenaJuego = "Test";

    private Button boton;
    private RectTransform rectBoton;
    private Canvas canvasPadre;
    private bool cargando;

    private void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
#endif

        boton = GetComponent<Button>();
        rectBoton = GetComponent<RectTransform>();
        canvasPadre = GetComponentInParent<Canvas>();

        if (boton != null)
        {
            boton.onClick.RemoveListener(IniciarPartida);
            boton.onClick.AddListener(IniciarPartida);
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (!InputSistemaUIUtil.HuboToqueEstaFrame() || rectBoton == null)
        {
            return;
        }

        if (ToqueDentroDelBoton())
        {
            IniciarPartida();
        }
#endif
    }

    public void IniciarPartida()
    {
        if (cargando)
        {
            return;
        }

        if (string.IsNullOrEmpty(escenaJuego))
        {
            Debug.LogError("[IniciarJuego] Nombre de escena vacío.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(escenaJuego))
        {
            Debug.LogError(
                $"[IniciarJuego] No se puede cargar '{escenaJuego}'. Revisa Build Settings.");
            return;
        }

        cargando = true;
        Time.timeScale = 1f;
        PuntajePartida.Limpiar();
        Debug.Log($"[IniciarJuego] Iniciando partida → '{escenaJuego}'");
        GestorEscenas.CargarSolo(escenaJuego);
    }

    private bool ToqueDentroDelBoton()
    {
        Camera camaraUi = null;
        if (canvasPadre != null && canvasPadre.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camaraUi = canvasPadre.worldCamera;
        }

        foreach (var posicion in InputSistemaUIUtil.PosicionesToqueEstaFrame())
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rectBoton, posicion, camaraUi))
            {
                return true;
            }
        }

        return false;
    }
}
