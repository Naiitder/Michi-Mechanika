using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Ajustes gráficos del jugador: seis perfiles (Very Low … Ultra) y los ajustes avanzados que cada
/// perfil rellena. Se guardan en PlayerPrefs y se aplican al arrancar y al cargar cada escena.
/// "High" en sombras y escala significa "como está configurado el Render Pipeline Asset": los
/// niveles inferiores recortan a partir de ahí, así que el aspecto se sigue ajustando en el asset.
/// </summary>
public static class GraphicsOptions
{
    public enum Setting { Shadows, RenderScale, AntiAliasing, AmbientOcclusion, PostProcessing, Fog, Smoke }

    public const int SettingCount = 7;
    public const int Custom = -1;

    // Niveles de los efectos volumétricos (Fog y Smoke).
    public const int Off = 0, Low = 1, Medium = 2, High = 3, Ultra = 4;

    public static readonly string[] PresetNames = { "Very Low", "Low", "Medium", "High", "Very High", "Ultra" };

    private static readonly string[] OffOn = { "Off", "On" };
    private static readonly string[] Volumetric = { "Off", "Low", "Medium", "High", "Ultra" };

    /// <summary>Texto de cada valor posible, por ajuste (mismo orden que <see cref="Setting"/>).</summary>
    public static readonly string[][] ValueNames =
    {
        new[] { "Off", "Low", "Medium", "High" },
        new[] { "50%", "60%", "70%", "80%", "90%", "100%" },
        new[] { "Off", "MSAA 2x", "MSAA 4x" },
        OffOn,
        OffOn,
        Volumetric,
        Volumetric,
    };

    // Valor de cada ajuste en cada perfil:  Shadows, Scale, AA, AO, Post, Fog, Smoke
    private static readonly int[][] Presets =
    {
        new[] { 0, 1, 0, 0, 0, Off,    Off    }, // Very Low
        new[] { 1, 3, 0, 0, 1, Low,    Low    }, // Low
        new[] { 2, 4, 0, 0, 1, Medium, Medium }, // Medium
        new[] { 3, 5, 0, 1, 1, High,   High   }, // High
        new[] { 3, 5, 0, 1, 1, Ultra,  Ultra  }, // Very High: el juego tal como estaba antes de existir los perfiles
        new[] { 3, 5, 2, 1, 1, Ultra,  Ultra  }, // Ultra
    };

    private static readonly float[] RenderScales = { 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1f };
    private static readonly int[] MsaaSamples = { 1, 2, 4 };

    // Pasos de raymarching por nivel (índice = Off … Ultra). Ultra son los máximos de los shaders.
    private static readonly int[] FogSteps = { 0, 6, 8, 12, 16 };
    private static readonly int[] CloudSteps = { 0, 6, 8, 10, 14 };

    private static readonly int FogStepsId = Shader.PropertyToID("_MichiFogSteps");
    private static readonly int CloudStepsId = Shader.PropertyToID("_MichiCloudSteps");
    private static readonly int CloudLowDetailId = Shader.PropertyToID("_MichiCloudLowDetail");

    private const string KeyPrefix = "settings.graphics.";

    private static int[] values;

    // Valores originales del Render Pipeline Asset: son la referencia de "High" y se restauran al
    // salir, porque en el Editor los cambios hechos al asset en Play no se deshacen solos.
    private static UniversalRenderPipelineAsset capturedAsset;
    private static float baseRenderScale, baseShadowDistance, baseCascade2Split;
    private static int baseCascades, baseShadowResolution, baseMsaa;
    private static readonly List<ScriptableRendererFeature> occlusionFeatures = new List<ScriptableRendererFeature>();
    private static readonly List<bool> occlusionWasActive = new List<bool>();

    // Lo que cada cámara traía activado en la escena: un ajuste solo puede apagarlo, nunca encenderlo.
    private static readonly Dictionary<int, (bool shadows, bool post)> authoredCameras =
        new Dictionary<int, (bool shadows, bool post)>();

    private static int DefaultPreset => Application.isMobilePlatform ? 1 : 3;

    private static int[] Values
    {
        get
        {
            if (values != null) return values;

            values = new int[SettingCount];
            int[] defaults = Presets[DefaultPreset];
            for (int k = 0; k < SettingCount; k++)
            {
                int stored = PlayerPrefs.GetInt(KeyPrefix + (Setting)k, defaults[k]);
                values[k] = Mathf.Clamp(stored, 0, ValueNames[k].Length - 1);
            }
            return values;
        }
    }

    public static int Get(Setting setting) => Values[(int)setting];

    public static void Set(Setting setting, int value)
    {
        int index = (int)setting;
        value = Mathf.Clamp(value, 0, ValueNames[index].Length - 1);
        if (Values[index] == value) return;

        Values[index] = value;
        Save();
        Apply();
    }

    /// <summary>Índice del perfil cuyos valores coinciden con los actuales, o <see cref="Custom"/>.</summary>
    public static int Preset
    {
        get
        {
            for (int p = 0; p < Presets.Length; p++)
            {
                bool match = true;
                for (int k = 0; k < SettingCount && match; k++)
                    match = Presets[p][k] == Values[k];
                if (match) return p;
            }
            return Custom;
        }
        set
        {
            value = Mathf.Clamp(value, 0, Presets.Length - 1);
            System.Array.Copy(Presets[value], Values, SettingCount);
            Save();
            Apply();
        }
    }

    /// <summary>Perfil desde el que se parte al pulsar una flecha estando en "Custom".</summary>
    public static int FallbackPreset => DefaultPreset;

    public static int FogLevel => Get(Setting.Fog);
    public static int SmokeLevel => Get(Setting.Smoke);

    private static void Save()
    {
        for (int k = 0; k < SettingCount; k++)
            PlayerPrefs.SetInt(KeyPrefix + (Setting)k, Values[k]);
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        values = null;
        capturedAsset = null;
        authoredCameras.Clear();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting -= RestoreAsset;
        Application.quitting += RestoreAsset;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif

        Apply();
    }

#if UNITY_EDITOR
    private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
    {
        if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode) RestoreAsset();
    }
#endif

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToCameras();

    public static void Apply()
    {
        ApplyToShaders();
        ApplyToPipeline();
        ApplyToCameras();
    }

    private static void ApplyToShaders()
    {
        int smoke = Get(Setting.Smoke);
        Shader.SetGlobalFloat(FogStepsId, FogSteps[Get(Setting.Fog)]);
        Shader.SetGlobalFloat(CloudStepsId, CloudSteps[smoke]);
        Shader.SetGlobalFloat(CloudLowDetailId, smoke == Low ? 1f : 0f);
    }

    private static void ApplyToPipeline()
    {
        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null) return;
        if (asset != capturedAsset) Capture(asset);

        asset.renderScale = baseRenderScale * RenderScales[Get(Setting.RenderScale)];
        asset.msaaSampleCount = Mathf.Max(baseMsaa, MsaaSamples[Get(Setting.AntiAliasing)]);

        // Sombras: High = lo configurado en el asset. Por debajo, menos cascadas y menos alcance
        // (con una o dos cascadas, repartirlas en la distancia original las dejaría borrosas).
        // "Off" se aplica en las cámaras.
        switch (Get(Setting.Shadows))
        {
            case 1:
                asset.shadowCascadeCount = 1;
                asset.shadowDistance = Mathf.Min(baseShadowDistance, 40f);
                asset.mainLightShadowmapResolution = Mathf.Min(baseShadowResolution, 1024);
                break;
            case 2:
                asset.shadowCascadeCount = Mathf.Min(baseCascades, 2);
                asset.shadowDistance = Mathf.Min(baseShadowDistance, 60f);
                asset.mainLightShadowmapResolution = baseShadowResolution;
                if (baseCascades > 2) asset.cascade2Split = 0.3f;
                break;
            default:
                asset.shadowCascadeCount = baseCascades;
                asset.shadowDistance = baseShadowDistance;
                asset.mainLightShadowmapResolution = baseShadowResolution;
                asset.cascade2Split = baseCascade2Split;
                break;
        }

        bool occlusion = Get(Setting.AmbientOcclusion) == 1;
        for (int k = 0; k < occlusionFeatures.Count; k++)
        {
            if (occlusionFeatures[k] != null)
                occlusionFeatures[k].SetActive(occlusionWasActive[k] && occlusion);
        }
    }

    private static void ApplyToCameras()
    {
        bool shadows = Get(Setting.Shadows) != 0;
        bool post = Get(Setting.PostProcessing) == 1;

        foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!camera.TryGetComponent(out UniversalAdditionalCameraData data)) continue;

            int id = camera.GetInstanceID();
            if (!authoredCameras.TryGetValue(id, out var authored))
            {
                authored = (data.renderShadows, data.renderPostProcessing);
                authoredCameras[id] = authored;
            }

            data.renderShadows = authored.shadows && shadows;
            data.renderPostProcessing = authored.post && post;
        }
    }

    private static void Capture(UniversalRenderPipelineAsset asset)
    {
        RestoreAsset();

        capturedAsset = asset;
        baseRenderScale = asset.renderScale;
        baseShadowDistance = asset.shadowDistance;
        baseCascades = asset.shadowCascadeCount;
        baseCascade2Split = asset.cascade2Split;
        baseShadowResolution = asset.mainLightShadowmapResolution;
        baseMsaa = asset.msaaSampleCount;

        foreach (ScriptableRendererData renderer in asset.rendererDataList)
        {
            if (renderer == null) continue;
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                if (!(feature is ScreenSpaceAmbientOcclusion)) continue;
                occlusionFeatures.Add(feature);
                occlusionWasActive.Add(feature.isActive);
            }
        }
    }

    private static void RestoreAsset()
    {
        if (capturedAsset != null)
        {
            capturedAsset.renderScale = baseRenderScale;
            capturedAsset.msaaSampleCount = baseMsaa;
            capturedAsset.shadowCascadeCount = baseCascades;
            capturedAsset.shadowDistance = baseShadowDistance;
            capturedAsset.cascade2Split = baseCascade2Split;
            capturedAsset.mainLightShadowmapResolution = baseShadowResolution;
        }

        for (int k = 0; k < occlusionFeatures.Count; k++)
        {
            if (occlusionFeatures[k] != null) occlusionFeatures[k].SetActive(occlusionWasActive[k]);
        }

        occlusionFeatures.Clear();
        occlusionWasActive.Clear();
        capturedAsset = null;
    }
}
