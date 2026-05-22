using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Asegura Canvas y EventSystem en la escena de menú inicial.
/// </summary>
[DefaultExecutionOrder(-100)]
public class MenuInicioBootstrap : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionAsset accionesInput;
#endif

    private void Awake()
    {
        CorregirCanvas();
        AsegurarEventSystem();
    }

    private void CorregirCanvas()
    {
        var rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        if (rect.localScale == Vector3.zero)
        {
            rect.localScale = Vector3.one;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
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
    }
}
