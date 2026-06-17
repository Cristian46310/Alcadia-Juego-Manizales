using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Configura el módulo UI del Input System para toques en Android/iOS.
/// </summary>
public static class InputSistemaUIUtil
{
#if ENABLE_INPUT_SYSTEM
    public static void HabilitarToqueMejorado()
    {
        if (!EnhancedTouchSupport.enabled)
        {
            EnhancedTouchSupport.Enable();
        }
    }

    public static void ConfigurarModuloUI(InputSystemUIInputModule modulo, InputActionAsset asset)
    {
        if (modulo == null || asset == null)
        {
            return;
        }

        HabilitarToqueMejorado();

        modulo.actionsAsset = asset;
        var mapaUi = asset.FindActionMap("UI", false);
        if (mapaUi == null)
        {
            Debug.LogWarning("[InputSistemaUI] No se encontró el mapa 'UI' en el asset de acciones.");
            return;
        }

        if (!mapaUi.enabled)
        {
            mapaUi.Enable();
        }

        modulo.point = Referencia(mapaUi, "Point");
        modulo.move = Referencia(mapaUi, "Navigate");
        modulo.submit = Referencia(mapaUi, "Submit");
        modulo.cancel = Referencia(mapaUi, "Cancel");
        modulo.leftClick = Referencia(mapaUi, "Click");
        modulo.middleClick = Referencia(mapaUi, "MiddleClick");
        modulo.rightClick = Referencia(mapaUi, "RightClick");
        modulo.scrollWheel = Referencia(mapaUi, "ScrollWheel");

        modulo.pointerBehavior = UIPointerBehavior.AllPointersAsIs;
    }

    private static InputActionReference Referencia(InputActionMap mapa, string nombre)
    {
        var accion = mapa.FindAction(nombre, false);
        return accion != null ? InputActionReference.Create(accion) : null;
    }

    public static void ConfigurarEventSystemEnEscena(InputActionAsset assetPreferido = null)
    {
        var sistema = EventSystem.current;
        if (sistema == null)
        {
            var go = new GameObject("EventSystem_Auto");
            sistema = go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        var modulo = sistema.GetComponent<InputSystemUIInputModule>();
        if (modulo == null)
        {
            modulo = sistema.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        var asset = assetPreferido ?? BuscarAssetInput();
        if (asset != null)
        {
            ConfigurarModuloUI(modulo, asset);
        }
        else
        {
            Debug.LogWarning("[InputSistemaUI] No se encontró InputSystem_Actions.");
        }
    }

    private static InputActionAsset BuscarAssetInput()
    {
        var todos = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        foreach (var candidato in todos)
        {
            if (candidato != null && candidato.name == "InputSystem_Actions")
            {
                return candidato;
            }
        }

        return null;
    }

    public static bool HuboToqueEstaFrame()
    {
        if (Touchscreen.current != null)
        {
            foreach (var toque in Touchscreen.current.touches)
            {
                if (toque.press.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return false;
    }

    public static IEnumerable<Vector2> PosicionesToqueEstaFrame()
    {
        if (Touchscreen.current != null)
        {
            foreach (var toque in Touchscreen.current.touches)
            {
                if (toque.press.wasPressedThisFrame)
                {
                    yield return toque.position.ReadValue();
                }
            }
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            yield return Mouse.current.position.ReadValue();
        }
    }
#endif
}
