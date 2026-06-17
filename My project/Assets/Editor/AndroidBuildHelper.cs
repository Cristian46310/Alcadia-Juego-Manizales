using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menú y compilación por línea de comandos para generar APK de Android.
/// </summary>
public static class AndroidBuildHelper
{
    private const string RutaApkPorDefecto = "Builds/Android/TraficoManizales.apk";

    [MenuItem("Build/Android/Generar APK (Debug)")]
    public static void GenerarApkDebug()
    {
        GenerarApk(false);
    }

    [MenuItem("Build/Android/Generar APK (Release)")]
    public static void GenerarApkRelease()
    {
        GenerarApk(true);
    }

    /// <summary>
    /// Unity -batchmode -quit -projectPath "My project" -executeMethod AndroidBuildHelper.BuildApkRelease
    /// </summary>
    public static void BuildApkRelease()
    {
        if (!GenerarApk(true))
        {
            EditorApplication.Exit(1);
        }
    }

    public static void BuildApkDebug()
    {
        if (!GenerarApk(false))
        {
            EditorApplication.Exit(1);
        }
    }

    private static bool GenerarApk(bool release)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        if (!ValidarEscenasBuild())
        {
            return false;
        }

        string rutaProyecto = Directory.GetParent(Application.dataPath).FullName;
        string rutaApk = Path.Combine(rutaProyecto, RutaApkPorDefecto);
        Directory.CreateDirectory(Path.GetDirectoryName(rutaApk));

        var opciones = new BuildPlayerOptions
        {
            scenes = ObtenerEscenasActivas(),
            locationPathName = rutaApk,
            target = BuildTarget.Android,
            options = BuildOptions.CompressWithLz4
        };

        if (!release)
        {
            opciones.options |= BuildOptions.Development | BuildOptions.AllowDebugging;
        }

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        Debug.Log($"[AndroidBuild] Compilando {(release ? "Release" : "Debug")} → {rutaApk}");

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        BuildSummary resumen = reporte.summary;

        if (resumen.result == BuildResult.Succeeded)
        {
            Debug.Log($"[AndroidBuild] APK listo: {rutaApk} ({resumen.totalSize / (1024f * 1024f):F1} MB)");
            EditorUtility.RevealInFinder(rutaApk);
            return true;
        }

        Debug.LogError($"[AndroidBuild] Falló: {resumen.result} — {resumen.totalErrors} errores.");
        return false;
    }

    private static bool ValidarEscenasBuild()
    {
        string[] requeridas = { "Inicio", "Test", "Taller", "MensajeMotivacional 1" };
        var escenas = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => Path.GetFileNameWithoutExtension(s.path)).ToList();

        bool ok = true;
        foreach (string nombre in requeridas)
        {
            if (!escenas.Contains(nombre))
            {
                Debug.LogError($"[AndroidBuild] Falta escena en Build Settings: {nombre}");
                ok = false;
            }
        }

        if (escenas.FirstOrDefault() != "Inicio")
        {
            Debug.LogWarning("[AndroidBuild] La primera escena debería ser 'Inicio'.");
        }

        return ok;
    }

    private static string[] ObtenerEscenasActivas()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
    }
}
