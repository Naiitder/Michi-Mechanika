using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CloudDrift : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float opacity = 1f;
    [SerializeField, Range(0f, 1f)] private float phase;

    [Header("Loop path")]
    [SerializeField] private Vector3 travel;
    [SerializeField] private float loopSeconds = 16f;
    [SerializeField] private float growth = 1f;

    [Header("Sway")]
    [SerializeField] private Vector3 sway;
    [SerializeField] private float swaySeconds = 30f;

    [Header("Appear and Disappear")]
    [SerializeField, Range(0f, 1f)] private float pulseAmount;
    [SerializeField] private float pulseSeconds = 25f;

    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    private Renderer cloudRenderer;
    private MaterialPropertyBlock block;
    private Vector3 origin;
    private Vector3 baseScale;
    private bool captured;
    private bool hiddenBySettings;

    private void OnEnable()
    {
        cloudRenderer = GetComponent<Renderer>();
        if (Application.isPlaying)
        {
            origin = transform.localPosition;
            baseScale = transform.localScale;
            captured = true;
        }
    }

    private void OnDisable()
    {
        if (!captured) return;
        transform.localPosition = origin;
        transform.localScale = baseScale;
        captured = false;
    }

    private void Update()
    {
        // Ajuste gráfico "Smoke and dust: Off". En el editor (sin Play) se dibuja siempre.
        bool hide = Application.isPlaying && GraphicsOptions.SmokeLevel == GraphicsOptions.Off;
        if (hide != hiddenBySettings)
        {
            hiddenBySettings = hide;
            cloudRenderer.enabled = !hide;
        }
        if (hide) return;

        float fade = 1f;

        if (captured)
        {
            float time = Time.time;
            Vector3 position = origin;
            Vector3 scale = baseScale;

            if (travel != Vector3.zero && loopSeconds > 0f)
            {
                float u = Mathf.Repeat(time / loopSeconds + phase, 1f);
                position += travel * u;
                scale *= Mathf.Lerp(1f, growth, u);
                fade = Mathf.SmoothStep(0f, 1f, u / 0.2f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.4f);
            }

            if (sway != Vector3.zero && swaySeconds > 0f)
            {
                float a = (time / swaySeconds + phase) * Mathf.PI * 2f;
                position += new Vector3(
                    Mathf.Sin(a) * sway.x,
                    Mathf.Sin(a * 1.7f + 1.3f) * sway.y,
                    Mathf.Cos(a * 0.8f + 0.4f) * sway.z);
                scale *= 1f + 0.07f * Mathf.Sin(a * 1.3f + 2f);
            }

            if (pulseAmount > 0f && pulseSeconds > 0f)
            {
                float n = Mathf.PerlinNoise(time / pulseSeconds + phase * 53.7f, phase * 91.3f + 0.5f);
                float visible = Mathf.SmoothStep(0f, 1f, (n - 0.3f) / 0.35f);
                fade *= Mathf.Lerp(1f, visible, pulseAmount);
            }

            transform.localPosition = position;
            transform.localScale = scale;
        }
        
        Vector3 start = captured ? origin : transform.localPosition;
        var seed = new Vector4(start.x * 0.73f + phase * 37.1f, start.y * 1.31f + phase * 11.7f, start.z * 0.57f, 0f);

        block ??= new MaterialPropertyBlock();
        cloudRenderer.GetPropertyBlock(block);
        block.SetFloat(FadeId, opacity * fade);
        block.SetVector(SeedId, seed);
        cloudRenderer.SetPropertyBlock(block);
    }
}
