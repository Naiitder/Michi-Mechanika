using UnityEngine;

/// <summary>
/// Telón de fondo pintado con parallax. Va en la Main Camera (la que lleva el CinemachineBrain).
/// Crea él solo un quad hijo que rellena la pantalla justo delante del plano lejano, así que
/// funciona con cualquier ángulo, FOV o proporción de pantalla sin ajustar nada a mano.
/// El aspecto (brillo, bruma, foso, parpadeo) se ajusta en el material; aquí van encuadre y parallax.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class BackdropParallax : MonoBehaviour
{
    [SerializeField] private Material material;

    [Header("Encuadre")]
    [Tooltip("Cuánto se amplía la pintura. Más zoom = se recorta el marco de primer plano y queda más margen para el parallax.")]
    [SerializeField, Range(1f, 3f)] private float zoom = 1.64f;
    [Tooltip("Qué parte de la pintura se ve: -1 = izquierda, 1 = derecha.")]
    [SerializeField, Range(-1f, 1f)] private float framingX = 0.1f;
    [Tooltip("Qué parte de la pintura se ve: -1 = abajo, 1 = arriba.")]
    [SerializeField, Range(-1f, 1f)] private float framingY = 0.38f;
    [Tooltip("Voltea la pintura en horizontal, para niveles con la cámara desde el otro lado.")]
    [SerializeField] private bool flipX;

    [Header("Parallax")]
    [Tooltip("Cuánto se desplaza lo más cercano de la pintura por cada unidad que se mueve la cámara.")]
    [SerializeField, Range(0f, 0.03f)] private float parallaxPerUnit = 0.005f;
    [Tooltip("Cuánto se mueve lo lejano respecto a lo cercano. 1 = todo se mueve igual (sin profundidad).")]
    [SerializeField, Range(0f, 1f)] private float farFactor = 0.5f;

    [Header("Colocación")]
    [Tooltip("Distancia del telón, como fracción del Far Clip de la cámara.")]
    [SerializeField, Range(0.5f, 0.99f)] private float distance = 0.95f;

    private static readonly int UVRectId = Shader.PropertyToID("_UVRect");
    private static readonly int ParallaxId = Shader.PropertyToID("_Parallax");

    private Camera cam;
    private GameObject quad;
    private Mesh mesh;
    private MeshRenderer quadRenderer;
    private MaterialPropertyBlock block;
    private Vector3 origin;
    private bool hasOrigin;

#if UNITY_EDITOR
    private void Reset()
    {
        material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/FactoryBackdrop_MAT.mat");
    }
#endif

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        hasOrigin = false;
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
        if (material == null || cam == null) return;

        EnsureQuad();
        FitToCamera();
        UpdateShader();
    }

    private void EnsureQuad()
    {
        if (quad != null)
        {
            if (quadRenderer.sharedMaterial != material) quadRenderer.sharedMaterial = material;
            return;
        }

        mesh = new Mesh { name = "Backdrop Quad", hideFlags = HideFlags.DontSave };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();

        quad = new GameObject("Backdrop (auto)") { hideFlags = HideFlags.DontSave };
        quad.transform.SetParent(transform, false);
        quad.AddComponent<MeshFilter>().sharedMesh = mesh;

        quadRenderer = quad.AddComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = material;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        quadRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        quadRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    // El quad rellena exactamente la pantalla a la distancia elegida.
    private void FitToCamera()
    {
        float d = cam.farClipPlane * distance;
        float height = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        Transform t = quad.transform;
        t.localPosition = new Vector3(0f, 0f, d);
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(width * 1.01f, height * 1.01f, 1f);
    }

    private void UpdateShader()
    {
        // Trozo de la pintura que se ve, ajustado a la proporción de la pantalla (sin deformarla).
        Texture tex = material.mainTexture;
        float texAspect = tex != null && tex.height > 0 ? (float)tex.width / tex.height : 16f / 9f;
        Vector2 scale = cam.aspect > texAspect
            ? new Vector2(1f, texAspect / cam.aspect)
            : new Vector2(cam.aspect / texAspect, 1f);
        scale /= zoom;

        // Margen que sobra a cada lado: se reparte entre el encuadre y el parallax.
        Vector2 margin = (Vector2.one - scale) * 0.5f;
        Vector2 center = new Vector2(framingX * margin.x, framingY * margin.y);
        Vector2 available = new Vector2(margin.x - Mathf.Abs(center.x), margin.y - Mathf.Abs(center.y));

        // Desplazamiento de la cámara desde donde empezó el nivel, en sus ejes derecha/arriba.
        Vector2 offset = Vector2.zero;
        if (Application.isPlaying)
        {
            if (!hasOrigin)
            {
                origin = transform.position;
                hasOrigin = true;
            }

            Vector3 delta = transform.position - origin;
            float x = Vector3.Dot(delta, transform.right) * parallaxPerUnit;
            float y = Vector3.Dot(delta, transform.up) * parallaxPerUnit;
            // Frenado suave al acercarse al borde de la pintura, para que nunca se vea el corte.
            offset = new Vector2(SoftLimit(x, available.x), SoftLimit(y, available.y));
        }

        if (flipX)
        {
            scale.x = -scale.x;
            center.x = -center.x;
            offset.x = -offset.x;
        }

        block ??= new MaterialPropertyBlock();
        quadRenderer.GetPropertyBlock(block);
        block.SetVector(UVRectId, new Vector4(scale.x, scale.y, center.x, center.y));
        block.SetVector(ParallaxId, new Vector4(offset.x, offset.y, farFactor, 0f));
        quadRenderer.SetPropertyBlock(block);
    }

    private static float SoftLimit(float value, float limit)
    {
        if (limit <= 0.0001f) return 0f;
        return limit * (float)System.Math.Tanh(value / limit);
    }

    private static void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
