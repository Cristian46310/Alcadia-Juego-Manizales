using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ajustes ligeros de rendimiento al iniciar la escena de juego (Test).
/// </summary>
[DefaultExecutionOrder(-250)]
public class RendimientoJuegoBootstrap : MonoBehaviour
{
    [SerializeField] private int fpsObjetivo = 60;
    [SerializeField] private float distanciaSombras = 45f;
    [SerializeField] private bool quitarSombrasEdificios = true;
    [SerializeField] private bool optimizarParaMovil = true;

    private void Awake()
    {
        Application.targetFrameRate = fpsObjetivo;
        QualitySettings.vSyncCount = 0;

        if (optimizarParaMovil || EsPlataformaMovil())
        {
            QualitySettings.shadowDistance = distanciaSombras;
            QualitySettings.pixelLightCount = 1;
        }

        if (quitarSombrasEdificios)
        {
            OptimizarRenderEdificios();
        }
    }

    private static bool EsPlataformaMovil()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return Application.isMobilePlatform;
#endif
    }

    private static void OptimizarRenderEdificios()
    {
        var renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            string nombre = renderer.gameObject.name;
            if (!nombre.StartsWith("Building") && !nombre.StartsWith("Cube"))
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }
}
