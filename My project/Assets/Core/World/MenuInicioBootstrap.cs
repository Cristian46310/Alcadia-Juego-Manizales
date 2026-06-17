using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Asegura Canvas y EventSystem en la escena de menú inicial (toques en móvil).
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
#if ENABLE_INPUT_SYSTEM
        InputSistemaUIUtil.ConfigurarEventSystemEnEscena(accionesInput);
#endif
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
}
