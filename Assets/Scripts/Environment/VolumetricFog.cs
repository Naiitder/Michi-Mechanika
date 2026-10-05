using UnityEngine;

/// <summary>
/// Niebla volumétrica del foso. Crea un quad pegado a la cámara principal que dibuja la niebla
/// (shader Michi/VolumetricFog) y le pasa la altura de la superficie y las luces que la iluminan.
/// El aspecto (colores, densidad, movimiento) se ajusta en el material.
/// </summary>
[ExecuteAlways]
public class VolumetricFog : MonoBehaviour
{
    private const int MaxLights = 4;

    [SerializeField] private Material material;

    [Header("Altura")]
    [Tooltip("Altura (Y del mundo) de la superficie de la niebla. Por debajo se va espesando.")]
    [SerializeField] private float topHeight = -8.5f;
    [Tooltip("Opcional: si se asigna, la superficie sigue a este objeto (por ejemplo el jugador) en vez de usar la altura fija.")]
    [SerializeField] private Transform followTarget;
    [Tooltip("Con seguimiento: a qué distancia por debajo del objetivo queda la superficie.")]
    [SerializeField] private float followOffset = -6f;
    [SerializeField] private float followSpeed = 3f;

    [Header("Luces")]
    [Tooltip("Luces puntuales que iluminan la niebla desde dentro (máximo 4). Vacío = las busca en la escena.")]
    [SerializeField] private Light[] lights;
    [Tooltip("Cuánto de la intensidad de cada luz pasa a la niebla.")]
    [SerializeField, Range(0f, 0.2f)] private float lightScattering = 0.045f;

    private static readonly int FogTopId = Shader.PropertyToID("_FogTop");
    private static readonly int LightPosId = Shader.PropertyToID("_FogLightPos");
    private static readonly int LightColorId = Shader.PropertyToID("_FogLightColor");

    private readonly Vector4[] lightPositions = new Vector4[MaxLights];
    private readonly Vector4[] lightColors = new Vector4[MaxLights];

    private Camera cam;
    private GameObject quad;
    private Mesh mesh;
    private MeshRenderer quadRenderer;
    private MaterialPropertyBlock block;
    private float currentTop;

#if UNITY_EDITOR
    private void Reset()
    {
        material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/VolumetricFog_MAT.mat");
    }
#endif

    private void OnEnable()
    {
        currentTop = TargetTop();
        if (lights == null || lights.Length == 0) lights = FindPointLights();
    }

    private void OnDisable()
    {
        SafeDestroy(quad);
        SafeDestroy(mesh);
        quad = null;
        mesh = null;
        quadRenderer = null;
    }

    private void LateUpdate()
    {
        if (material == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Ajuste gráfico "Volumetric fog: Off". En el editor (sin Play) se dibuja siempre.
        if (Application.isPlaying && GraphicsOptions.FogLevel == GraphicsOptions.Off)
        {
            if (quadRenderer != null) quadRenderer.enabled = false;
            return;
        }

        EnsureQuad();
        quadRenderer.enabled = true;
        FitToCamera();
        UpdateShader();
    }

    private float TargetTop() => followTarget != null ? followTarget.position.y + followOffset : topHeight;

    private static Light[] FindPointLights()
    {
        Light[] all = FindObjectsByType<Light>(FindObjectsSortMode.None);
        System.Array.Sort(all, (a, b) => b.intensity.CompareTo(a.intensity));
        var found = new System.Collections.Generic.List<Light>();
        foreach (Light l in all)
        {
            if (l.type != LightType.Point) continue;
            found.Add(l);
            if (found.Count == MaxLights) break;
        }
        return found.ToArray();
    }

    private void EnsureQuad()
    {
        if (quad != null)
        {
            if (quad.transform.parent != cam.transform) quad.transform.SetParent(cam.transform, false);
            if (quadRenderer.sharedMaterial != material) quadRenderer.sharedMaterial = material;
            return;
        }

        mesh = new Mesh { name = "Fog Quad", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();

        quad = new GameObject("Volumetric Fog (auto)") { hideFlags = HideFlags.DontSave };
        quad.transform.SetParent(cam.transform, false);
        quad.AddComponent<MeshFilter>().sharedMesh = mesh;

        quadRenderer = quad.AddComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = material;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        quadRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        quadRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    // El quad tapa toda la pantalla justo delante de la cámara; la niebla se calcula en el shader.
    private void FitToCamera()
    {
        float d = cam.nearClipPlane + 0.5f;
        float height = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        Transform t = quad.transform;
        t.localPosition = new Vector3(0f, 0f, d);
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(width * 1.05f, height * 1.05f, 1f);
    }

    private void UpdateShader()
    {
        float target = TargetTop();
        currentTop = Application.isPlaying && followTarget != null
            ? Mathf.Lerp(currentTop, target, 1f - Mathf.Exp(-followSpeed * Time.deltaTime))
            : target;

        for (int k = 0; k < MaxLights; k++)
        {
            Light l = lights != null && k < lights.Length ? lights[k] : null;
            if (l != null && l.isActiveAndEnabled)
            {
                Vector3 p = l.transform.position;
                Color c = l.color.linear * (l.intensity * lightScattering);
                lightPositions[k] = new Vector4(p.x, p.y, p.z, l.range);
                lightColors[k] = new Vector4(c.r, c.g, c.b, 0f);
            }
            else
            {
                lightPositions[k] = new Vector4(0f, 0f, 0f, 0.001f);
                lightColors[k] = Vector4.zero;
            }
        }

        block ??= new MaterialPropertyBlock();
        quadRenderer.GetPropertyBlock(block);
        block.SetFloat(FogTopId, currentTop);
        block.SetVectorArray(LightPosId, lightPositions);
        block.SetVectorArray(LightColorId, lightColors);
        quadRenderer.SetPropertyBlock(block);
    }

    private static void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
