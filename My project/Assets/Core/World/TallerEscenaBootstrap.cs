using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Asegura Canvas, textos y EventSystem al entrar a Taller o MensajeMotivacional.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TallerEscenaBootstrap : MonoBehaviour
{
    private static readonly string[] NombresTextoMensaje = { "TextoReflexio", "TextoMensaje" };

#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionAsset accionesInput;
#endif

    private void Awake()
    {
        Debug.Log($"[EscenaFinal] Iniciada: {gameObject.scene.name}");
        ConfigurarEventSystem();
        StartCoroutine(InicializarEscenaUnica());
    }

    private IEnumerator InicializarEscenaUnica()
    {
        yield return GestorEscenas.DescargarEscenasExceptoCoroutine(gameObject.scene);
        SceneManager.SetActiveScene(gameObject.scene);

        CorregirCanvas();
        EliminarPanelFondoGenerado();
        CorregirTextosMensaje();
        ConfigurarEventSystem();
        AsegurarPuntajeFinalUI();
        OcultarElementosEscenasAjenas();
        AsegurarBotonesReintentar();
    }

    private void AsegurarBotonesReintentar()
    {
        var botones = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < botones.Length; i++)
        {
            Button boton = botones[i];
            if (boton == null || boton.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            if (!boton.gameObject.name.Contains("retry", System.StringComparison.OrdinalIgnoreCase)
                && !boton.GetComponent<VolverATest>())
            {
                continue;
            }

            if (boton.GetComponent<VolverATest>() == null)
            {
                boton.gameObject.AddComponent<VolverATest>();
            }

            boton.interactable = true;

            foreach (var tmp in boton.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp != null)
                {
                    tmp.raycastTarget = false;
                }
            }
        }
    }

    private void OcultarElementosEscenasAjenas()
    {
        Scene escenaActual = gameObject.scene;

        var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.scene == escenaActual)
            {
                continue;
            }

            canvas.gameObject.SetActive(false);
            Debug.Log($"[EscenaFinal] Canvas oculto de escena ajena: '{canvas.gameObject.scene.name}'");
        }

        var camaras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < camaras.Length; i++)
        {
            Camera camara = camaras[i];
            if (camara == null || camara.gameObject.scene == escenaActual)
            {
                continue;
            }

            camara.enabled = false;
        }
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
            Debug.Log("[EscenaFinal] Canvas scale corregido a (1,1,1).");
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localPosition = Vector3.zero;

        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
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

    private void CorregirTextosMensaje()
    {
        var fuentePorDefecto = TMP_Settings.defaultFontAsset;

        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp == null)
            {
                continue;
            }

            string nombre = tmp.gameObject.name;
            if (nombre == "TextoPuntajeFinal" || nombre == "TextoKilometrosFinal")
            {
                continue;
            }

            if (fuentePorDefecto != null && tmp.font == null)
            {
                tmp.font = fuentePorDefecto;
            }

            if (!EsTextoMensajePrincipal(nombre))
            {
                continue;
            }

            var rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(920f, 420f);
            rt.anchoredPosition = new Vector2(0f, 80f);
            rt.localScale = Vector3.one;

            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = Mathf.Max(tmp.fontSize, 36f);
            tmp.raycastTarget = false;

            if (string.IsNullOrWhiteSpace(tmp.text))
            {
                Debug.LogWarning($"[EscenaFinal] El texto '{nombre}' está vacío en la escena.");
            }
            else
            {
                tmp.ForceMeshUpdate();
            }
        }
    }

    private void EliminarPanelFondoGenerado()
    {
        Transform panel = transform.Find("PanelFondoMensaje");
        if (panel != null)
        {
            Destroy(panel.gameObject);
        }
    }

    private static bool EsTextoMensajePrincipal(string nombre)
    {
        for (int i = 0; i < NombresTextoMensaje.Length; i++)
        {
            if (nombre == NombresTextoMensaje[i])
            {
                return true;
            }
        }

        return false;
    }

    private void AsegurarPuntajeFinalUI()
    {
        var ui = GetComponent<MostrarPuntajeFinalUI>();
        if (ui == null)
        {
            ui = gameObject.AddComponent<MostrarPuntajeFinalUI>();
        }

        // Para las escenas finales solicitadas, cambiar el prefijo a "DINERO"
        string nombreEscena = gameObject.scene.name ?? string.Empty;
        string nombreMinus = nombreEscena.ToLowerInvariant();
        if (nombreMinus.Contains("taller") || nombreMinus.Contains("mensaje"))
        {
            ui.SetPrefijoPuntaje("DINERO ");
            Debug.Log($"[EscenaFinal] Prefijo de puntaje cambiado a 'DINERO' para escena: {nombreEscena}");
        }
    }

    private void ConfigurarEventSystem()
    {
#if ENABLE_INPUT_SYSTEM
        InputSistemaUIUtil.ConfigurarEventSystemEnEscena(accionesInput);
#else
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem_Auto");
            go.AddComponent<EventSystem>();
        }
#endif
    }
}
