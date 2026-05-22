using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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
        if (!EstaPantallaPresionada() || rectBoton == null)
        {
            return;
        }

        if (ClicDentroDelBoton())
        {
            IniciarPartida();
        }
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

    private bool ClicDentroDelBoton()
    {
        Camera camaraUi = null;
        if (canvasPadre != null && canvasPadre.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camaraUi = canvasPadre.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectBoton, ObtenerPosicionPantalla(), camaraUi);
    }

    private static Vector2 ObtenerPosicionPantalla()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    private static bool EstaPantallaPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.leftButton.wasPressedThisFrame;
        }
#endif
        return Input.GetMouseButtonDown(0);
    }
}
