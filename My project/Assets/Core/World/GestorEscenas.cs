using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Garantiza que solo una escena de juego esté activa (evita superposición en editor o carga additive).
/// </summary>
public static class GestorEscenas
{
    public static void CargarSolo(string nombreEscena)
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogError("[GestorEscenas] Nombre de escena vacío.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena, LoadSceneMode.Single);
    }

    /// <summary>
    /// Al entrar a Taller/Mensaje, descarga cualquier otra escena que siga abierta (p. ej. Test en el editor).
    /// </summary>
    public static void DescargarEscenasExcepto(Scene escenaPermitida)
    {
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene escena = SceneManager.GetSceneAt(i);
            if (!escena.isLoaded || escena == escenaPermitida)
            {
                continue;
            }

            Debug.Log($"[GestorEscenas] Descargando escena extra: '{escena.name}'");
            SceneManager.UnloadSceneAsync(escena);
        }
    }

    public static IEnumerator DescargarEscenasExceptoCoroutine(Scene escenaPermitida)
    {
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene escena = SceneManager.GetSceneAt(i);
            if (!escena.isLoaded || escena == escenaPermitida)
            {
                continue;
            }

            Debug.Log($"[GestorEscenas] Descargando escena extra: '{escena.name}'");
            yield return SceneManager.UnloadSceneAsync(escena);
        }
    }
}
