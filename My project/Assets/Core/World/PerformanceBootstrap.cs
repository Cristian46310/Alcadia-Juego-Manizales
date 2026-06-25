using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class PerformanceBootstrap : MonoBehaviour
{
    [Header("Performance")]
    [Tooltip("Target frame rate for the game. Set 0 to leave default.")]
    public int targetFrameRate = 60;

    [Tooltip("Disable vSync when setting targetFrameRate > 0 to use targetFrameRate.")]
    public bool disableVSync = true;

    void Awake()
    {
        if (targetFrameRate > 0)
        {
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
