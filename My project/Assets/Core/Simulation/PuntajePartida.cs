using UnityEngine;

/// <summary>
/// Puntaje de la partida actual, disponible entre escenas (Test → Taller / Mensaje).
/// </summary>
public static class PuntajePartida
{
    private const string ClavePuntaje = "PuntajeFinal";
    private const string ClaveMetros = "MetrosFinal";

    public static int Puntaje { get; private set; }
    public static float MetrosRecorridos { get; private set; }

    public static float KilometrosRecorridos => MetrosRecorridos / 1000f;

    public static void GuardarDesde(DistanceScoreController controlador)
    {
        if (controlador == null)
        {
            return;
        }

        Puntaje = controlador.PuntajeActual;
        MetrosRecorridos = controlador.MetrosRecorridos;

        PlayerPrefs.SetInt(ClavePuntaje, Puntaje);
        PlayerPrefs.SetFloat(ClaveMetros, MetrosRecorridos);
        PlayerPrefs.Save();
    }

    public static void Cargar()
    {
        Puntaje = PlayerPrefs.GetInt(ClavePuntaje, 0);
        MetrosRecorridos = PlayerPrefs.GetFloat(ClaveMetros, 0f);
    }

    public static void Limpiar()
    {
        Puntaje = 0;
        MetrosRecorridos = 0f;
        PlayerPrefs.DeleteKey(ClavePuntaje);
        PlayerPrefs.DeleteKey(ClaveMetros);
        PlayerPrefs.Save();
    }
}
