using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ajusta controles táctiles en la escena de juego (Test): EventSystem multi-toque y pedales visibles en móvil.
/// </summary>
[DefaultExecutionOrder(-100)]
public class ControlesJuegoBootstrap : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionAsset accionesInput;
#endif

    [SerializeField] private string nombrePedalAcelerador = "Btn_Acelerador";
    [SerializeField] private string nombrePedalFreno = "Btn_Freno";

    private void Awake()
    {
        CorregirCanvas();
        AjustarPedalesParaMovil();
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

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void AjustarPedalesParaMovil()
    {
        ColocarEnEsquinaInferiorDerecha(
            GameObject.Find(nombrePedalAcelerador)?.GetComponent<RectTransform>(),
            new Vector2(-140f, 140f),
            new Vector2(2f, 3f));

        ColocarEnEsquinaInferiorDerecha(
            GameObject.Find(nombrePedalFreno)?.GetComponent<RectTransform>(),
            new Vector2(-320f, 140f),
            new Vector2(2f, 2.5f));
    }

    private static void ColocarEnEsquinaInferiorDerecha(
        RectTransform rect,
        Vector2 offset,
        Vector2 escala)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = offset;
        rect.localScale = new Vector3(escala.x, escala.y, 1f);
    }
}
