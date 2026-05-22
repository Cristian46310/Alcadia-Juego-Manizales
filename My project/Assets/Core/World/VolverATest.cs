using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class VolverATest : MonoBehaviour
{
    [SerializeField] private string nombreEscena = "Test";
    [SerializeField] private bool permitirTeclaR = true;

    private Button boton;
    private RectTransform rectBoton;
    private Canvas canvasPadre;
    private bool cargando;

    private void Awake()
    {
        Debug.Log($"[VolverATest] Activo en '{gameObject.name}' (escena: {SceneManager.GetActiveScene().name})");

        boton = GetComponent<Button>();
        rectBoton = GetComponent<RectTransform>();
        canvasPadre = GetComponentInParent<Canvas>();

        if (boton != null)
        {
            boton.onClick.RemoveListener(IntentarDeNuevo);
            boton.onClick.AddListener(IntentarDeNuevo);
        }
        else
        {
            Debug.LogWarning("[VolverATest] Falta componente Button en este objeto.");
        }

        if (EventSystem.current == null)
        {
            Debug.LogWarning("[VolverATest] No hay EventSystem. Se usa detección manual de clic.");
        }
    }

    private void Update()
    {
        if (!EstaPantallaPresionada())
        {
            return;
        }

        if (ClicDentroDelBoton())
        {
            Debug.Log("[VolverATest] Clic detectado en el botón.");
            IntentarDeNuevo();
        }
        else if (permitirTeclaR && TeclaRPresionada())
        {
            Debug.Log("[VolverATest] Tecla R — cargando escena.");
            IntentarDeNuevo();
        }
    }

    public void IntentarDeNuevo()
    {
        if (cargando)
        {
            return;
        }

        cargando = true;
        Time.timeScale = 1f;
        PuntajePartida.Limpiar();

        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError("[VolverATest] Nombre de escena vacío.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError(
                $"[VolverATest] No se puede cargar '{nombreEscena}'. " +
                "Revisa File > Build Settings.");
            return;
        }

        Debug.Log($"[VolverATest] Cargando '{nombreEscena}'...");
        GestorEscenas.CargarSolo(nombreEscena);
    }

    private bool ClicDentroDelBoton()
    {
        if (rectBoton == null)
        {
            return false;
        }

        Vector2 posicionPantalla = ObtenerPosicionPantalla();

        Camera camaraUi = null;
        if (canvasPadre != null && canvasPadre.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camaraUi = canvasPadre.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectBoton, posicionPantalla, camaraUi);
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

    private static bool TeclaRPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.rKey.wasPressedThisFrame;
        }
#endif
        return Input.GetKeyDown(KeyCode.R);
    }
}
