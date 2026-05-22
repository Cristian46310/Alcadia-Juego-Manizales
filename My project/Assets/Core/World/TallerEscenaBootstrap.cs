using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Asegura EventSystem y Canvas listos al entrar al taller.
/// </summary>
public class TallerEscenaBootstrap : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionAsset accionesInput;
#endif

    private void Awake()
    {
        Debug.Log("[Taller] Escena Taller iniciada.");

        CorregirCanvas();
        AsegurarEventSystem();
    }

    private void CorregirCanvas()
    {
        var rect = GetComponent<RectTransform>();
        if (rect != null && rect.localScale == Vector3.zero)
        {
            rect.localScale = Vector3.one;
            Debug.Log("[Taller] Canvas scale corregido a (1,1,1).");
        }
    }

    private void AsegurarEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        var go = new GameObject("EventSystem_Auto");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        var modulo = go.AddComponent<InputSystemUIInputModule>();
        if (accionesInput != null)
        {
            modulo.actionsAsset = accionesInput;
        }
#endif
        Debug.Log("[Taller] EventSystem creado en runtime.");
    }
}
