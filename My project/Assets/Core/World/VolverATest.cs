using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
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
#if ENABLE_INPUT_SYSTEM
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
#endif

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
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (!InputSistemaUIUtil.HuboToqueEstaFrame())
        {
            if (permitirTeclaR && TeclaRPresionada())
            {
                IntentarDeNuevo();
            }

            return;
        }

        if (ToqueDentroDelBoton())
        {
            IntentarDeNuevo();
        }
#else
        if (Input.GetMouseButtonDown(0) && ToqueDentroDelBoton(Input.mousePosition))
        {
            IntentarDeNuevo();
        }
        else if (permitirTeclaR && Input.GetKeyDown(KeyCode.R))
        {
            IntentarDeNuevo();
        }
#endif
    }

    public void IntentarDeNuevo()
    {
        if (cargando)
        {
            return;
        }

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

        cargando = true;
        Time.timeScale = 1f;
        PuntajePartida.Limpiar();
        Debug.Log($"[VolverATest] Cargando '{nombreEscena}'...");
        GestorEscenas.CargarSolo(nombreEscena);
    }

    private bool ToqueDentroDelBoton()
    {
        if (rectBoton == null)
        {
            return false;
        }

        Camera camaraUi = null;
        if (canvasPadre != null && canvasPadre.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camaraUi = canvasPadre.worldCamera;
        }

#if ENABLE_INPUT_SYSTEM
        foreach (var posicion in InputSistemaUIUtil.PosicionesToqueEstaFrame())
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rectBoton, posicion, camaraUi))
            {
                return true;
            }
        }

        return false;
#else
        return ToqueDentroDelBoton(Input.mousePosition);
#endif
    }

    private bool ToqueDentroDelBoton(Vector2 posicionPantalla)
    {
        if (rectBoton == null)
        {
            return false;
        }

        Camera camaraUi = null;
        if (canvasPadre != null && canvasPadre.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camaraUi = canvasPadre.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rectBoton, posicionPantalla, camaraUi);
    }

    private static bool TeclaRPresionada()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.rKey.wasPressedThisFrame;
        }

        return false;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}
