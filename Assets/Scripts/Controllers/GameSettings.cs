using UnityEngine;

public static class GameSettings
{
    public const int Unlimited = -1;
    
    public static readonly int[] FrameRateOptions = { 24, 30, 60, 120, 144, 160, 200, 244, Unlimited };

    private const string MaxFpsKey = "settings.maxFps";
    private const int DefaultMaxFps = 60;
    private const string MasterVolumeKey = "settings.masterVolume";

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

    /// <summary>Volumen general, de 0 a 1. Escala todo lo que suena en el juego.</summary>
    public static float MasterVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        set
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            ApplyVolume();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplyVolume()
    {
        AudioListener.volume = MasterVolume;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = MaxFps;
    }
}
