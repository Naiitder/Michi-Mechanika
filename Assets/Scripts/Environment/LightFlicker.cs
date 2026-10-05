using UnityEngine;

/// <summary>
/// Parpadeo suave para luces de horno o farol: hace oscilar la intensidad alrededor de su valor
/// original con ruido, para que no se repita ni dé saltos bruscos.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Tooltip("Cuánto varía la intensidad (0.2 = ±20 %).")]
    [SerializeField, Range(0f, 1f)] private float amount = 0.15f;
    [Tooltip("Velocidad del parpadeo.")]
    [SerializeField, Range(0f, 20f)] private float speed = 2f;

    private Light target;
    private float baseIntensity;
    private float seed;

    private void Awake()
    {
        target = GetComponent<Light>();
        baseIntensity = target.intensity;
        seed = Random.value * 100f;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;
        target.intensity = baseIntensity * (1f + noise * amount);
    }
}
