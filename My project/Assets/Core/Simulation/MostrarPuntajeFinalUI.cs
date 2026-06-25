using TMPro;
using UnityEngine;

public class MostrarPuntajeFinalUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textoPuntaje;
    [SerializeField] private string prefijoPuntaje = "PUNTAJE FINAL ";
    [SerializeField] private int digitosMinimos = 6;
    [SerializeField] private bool mostrarKilometros = true;
    [SerializeField] private string prefijoKm = "Distancia: ";
    [SerializeField] private TextMeshProUGUI textoKilometros;

    private void Awake()
    {
        AsegurarElementosUI();
    }

    private void Start()
    {
        PuntajePartida.Cargar();
        ActualizarUI();
    }

    private void AsegurarElementosUI()
    {
        if (textoPuntaje == null)
        {
            textoPuntaje = CrearTexto("TextoPuntajeFinal", new Vector2(0f, 520f), 40);
        }

        if (mostrarKilometros && textoKilometros == null)
        {
            textoKilometros = CrearTexto("TextoKilometrosFinal", new Vector2(0f, 460f), 28);
        }
    }

    private TextMeshProUGUI CrearTexto(string nombre, Vector2 anchoredPosition, int fontSize)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900f, 80f);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.color = new Color(0.1f, 0.45f, 0.2f, 1f);
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private void ActualizarUI()
    {
        if (textoPuntaje != null)
        {
            string numero = digitosMinimos > 0
                ? PuntajePartida.Puntaje.ToString("D" + digitosMinimos)
                : PuntajePartida.Puntaje.ToString();
            textoPuntaje.text = prefijoPuntaje + numero;
        }

        if (mostrarKilometros && textoKilometros != null)
        {
            textoKilometros.text = prefijoKm + PuntajePartida.KilometrosRecorridos.ToString("F1") + " km";
        }
    }

    // Permite cambiar el prefijo del puntaje desde otras clases (por ejemplo, bootstrap de escena)
    public void SetPrefijoPuntaje(string nuevoPrefijo)
    {
        prefijoPuntaje = nuevoPrefijo ?? string.Empty;
        ActualizarUI();
    }
}
