using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
#endif

/// <summary>
/// Pedal táctil (acelerador o freno). Solo cuenta el dedo que empezó sobre el pedal.
/// </summary>
public class PedalInput : MonoBehaviour
{
    public bool isPressed;

    private RectTransform rect;
    private Canvas canvasPadre;
    private int? dedoActivo;

    private void Awake()
    {
#if ENABLE_INPUT_SYSTEM
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
#endif
        rect = GetComponent<RectTransform>();
        canvasPadre = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        isPressed = rect != null && ActualizarToquePedal();
    }

    private bool ActualizarToquePedal()
    {
        Camera camaraUi = ObtenerCamaraUi();

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null)
        {
            if (dedoActivo.HasValue && BuscarToquePorId(Touchscreen.current, dedoActivo.Value, out var toqueSeguimiento))
            {
                if (!toqueSeguimiento.press.isPressed)
                {
                    dedoActivo = null;
                    return false;
                }

                return ContienePunto(toqueSeguimiento.position.ReadValue(), camaraUi);
            }

            if (dedoActivo.HasValue)
            {
                dedoActivo = null;
            }

            foreach (var toque in Touchscreen.current.touches)
            {
                if (!toque.press.wasPressedThisFrame)
                {
                    continue;
                }

                if (ContienePunto(toque.position.ReadValue(), camaraUi))
                {
                    dedoActivo = toque.touchId.ReadValue();
                    return true;
                }
            }
        }

        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame
                && ContienePunto(Mouse.current.position.ReadValue(), camaraUi))
            {
                dedoActivo = -1;
                return true;
            }

            if (dedoActivo == -1)
            {
                if (!Mouse.current.leftButton.isPressed)
                {
                    dedoActivo = null;
                    return false;
                }

                return ContienePunto(Mouse.current.position.ReadValue(), camaraUi);
            }
        }

        return false;
#else
        if (dedoActivo.HasValue && dedoActivo.Value >= 0)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId != dedoActivo.Value)
                {
                    continue;
                }

                if (t.phase == UnityEngine.TouchPhase.Ended || t.phase == UnityEngine.TouchPhase.Canceled)
                {
                    dedoActivo = null;
                    return false;
                }

                return ContienePunto(t.position, camaraUi);
            }

            dedoActivo = null;
            return false;
        }

        for (var j = 0; j < Input.touchCount; j++)
        {
            var nuevo = Input.GetTouch(j);
            if (nuevo.phase != UnityEngine.TouchPhase.Began)
            {
                continue;
            }

            if (ContienePunto(nuevo.position, camaraUi))
            {
                dedoActivo = nuevo.fingerId;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0) && ContienePunto(Input.mousePosition, camaraUi))
        {
            dedoActivo = -1;
            return true;
        }

        if (dedoActivo == -1 && Input.GetMouseButton(0))
        {
            return ContienePunto(Input.mousePosition, camaraUi);
        }

        if (!Input.GetMouseButton(0))
        {
            dedoActivo = null;
        }

        return false;
#endif
    }

    private Camera ObtenerCamaraUi()
    {
        if (canvasPadre != null && canvasPadre.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvasPadre.worldCamera;
        }

        return null;
    }

    private bool ContienePunto(Vector2 posicionPantalla, Camera camaraUi)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rect, posicionPantalla, camaraUi);
    }

#if ENABLE_INPUT_SYSTEM
    private static bool BuscarToquePorId(
        Touchscreen pantalla,
        int id,
        out UnityEngine.InputSystem.Controls.TouchControl toque)
    {
        foreach (var candidato in pantalla.touches)
        {
            if (candidato.touchId.ReadValue() == id)
            {
                toque = candidato;
                return true;
            }
        }

        toque = null;
        return false;
    }
#endif
}
