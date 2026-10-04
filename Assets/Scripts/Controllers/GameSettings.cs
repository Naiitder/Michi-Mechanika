using UnityEngine;

public static class GameSettings
{
    public const int Unlimited = -1;
    
    public static readonly int[] FrameRateOptions = { 24, 30, 60, 120, 144, 160, 200, 244, Unlimited };

    private const string MaxFpsKey = "settings.maxFps";
    private const int DefaultMaxFps = 60;

    public static int MaxFps
    {
        get => PlayerPrefs.GetInt(MaxFpsKey, DefaultMaxFps);
        set
        {
            PlayerPrefs.SetInt(MaxFpsKey, value);
            PlayerPrefs.Save();
            ApplyFrameRate();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = MaxFps;
    }
}
