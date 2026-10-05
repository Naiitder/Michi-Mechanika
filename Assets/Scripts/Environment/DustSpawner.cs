using System.Collections.Generic;
using UnityEngine;

public class DustSpawner : MonoBehaviour
{
    private class Cloud
    {
        public Transform transform;
        public MeshRenderer renderer;
        public Vector3 velocity;
        public Vector3 baseScale;
        public float age;
        public float lifetime;
        public float opacity;
        public float growth;
        public bool alive;
    }

    [SerializeField] private Material material;
    [SerializeField] private Transform target;

    [Header("Quantity")]
    [SerializeField, Range(1, 16)] private int maxClouds = 6;
    [SerializeField] private Vector2 spawnInterval = new Vector2(3f, 7f);
    [SerializeField] private Vector2 lifetime = new Vector2(18f, 38f);

    [Header("Spawn location")]
    [SerializeField] private Vector2 distance = new Vector2(6f, 22f);
    [SerializeField] private Vector2 height = new Vector2(-5f, 2.5f);

    [Header("Tamaño y opacidad")]
    [SerializeField] private Vector3 smallestSize = new Vector3(4f, 2f, 3.5f);
    [SerializeField] private Vector3 largestSize = new Vector3(17f, 6f, 12f);
    [SerializeField] private Vector2 opacity = new Vector2(0.4f, 1f);

    [Header("Wind")]
    [SerializeField] private Vector2 windDirection = new Vector2(1f, -0.25f);
    [SerializeField] private Vector2 windSpeed = new Vector2(0.5f, 1.6f);
    [SerializeField, Range(0f, 90f)] private float spread = 25f;

    private static readonly int FadeId = Shader.PropertyToID("_Fade");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    private readonly List<Cloud> clouds = new List<Cloud>();
    private MaterialPropertyBlock block;
    private Mesh cubeMesh;
    private float nextSpawn;
    private int cloudLimit;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        cloudLimit = maxClouds;
        block = new MaterialPropertyBlock();
        
        if (target != null && material != null)
        {
            int initial = Mathf.Max(1, maxClouds / 2);
            for (int k = 0; k < initial; k++)
                Spawn(Random.Range(0.1f, 0.6f));
        }

        nextSpawn = Random.Range(spawnInterval.x, spawnInterval.y);
    }

    private void Update()
    {
        if (target == null || material == null) return;

        // Ajuste gráfico "Smoke and dust": apagado no hay nubes; en Low, la mitad.
        int quality = GraphicsOptions.SmokeLevel;
        if (quality == GraphicsOptions.Off)
        {
            foreach (Cloud cloud in clouds)
            {
                if (!cloud.alive) continue;
                cloud.alive = false;
                cloud.renderer.enabled = false;
            }
            return;
        }
        cloudLimit = quality == GraphicsOptions.Low ? Mathf.Max(1, maxClouds / 2) : maxClouds;

        float dt = Time.deltaTime;

        nextSpawn -= dt;
        if (nextSpawn <= 0f)
        {
            Spawn(0f);
            nextSpawn = Random.Range(spawnInterval.x, spawnInterval.y);
        }

        foreach (Cloud cloud in clouds)
        {
            if (!cloud.alive) continue;

            cloud.age += dt;
            float u = cloud.age / cloud.lifetime;
            if (u >= 1f)
            {
                cloud.alive = false;
                cloud.renderer.enabled = false;
                continue;
            }

            cloud.transform.position += cloud.velocity * dt;
            cloud.transform.localScale = cloud.baseScale * Mathf.Lerp(1f, cloud.growth, u);
            
            float fade = Mathf.SmoothStep(0f, 1f, u / 0.25f) * Mathf.SmoothStep(0f, 1f, (1f - u) / 0.35f);
            cloud.renderer.GetPropertyBlock(block);
            block.SetFloat(FadeId, cloud.opacity * fade);
            cloud.renderer.SetPropertyBlock(block);
        }
    }

    private void Spawn(float startAt)
    {
        Cloud cloud = GetFreeCloud();
        if (cloud == null) return;
        
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(distance.x, distance.y);
        Vector3 position = target.position + new Vector3(
            Mathf.Cos(angle) * radius,
            Random.Range(height.x, height.y),
            Mathf.Sin(angle) * radius);
        
        Vector2 wind = windDirection.sqrMagnitude > 0.0001f ? windDirection.normalized : Vector2.right;
        float turn = Random.Range(-spread, spread) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(turn), sin = Mathf.Sin(turn);
        var direction = new Vector3(wind.x * cos - wind.y * sin, 0f, wind.x * sin + wind.y * cos);

        float size = Random.value;
        cloud.baseScale = Vector3.Lerp(smallestSize, largestSize, size * size);
        cloud.velocity = direction * Random.Range(windSpeed.x, windSpeed.y) + Vector3.up * Random.Range(-0.05f, 0.12f);
        cloud.lifetime = Random.Range(lifetime.x, lifetime.y);
        cloud.age = cloud.lifetime * startAt;
        cloud.opacity = Random.Range(opacity.x, opacity.y);
        cloud.growth = Random.Range(1.1f, 1.5f);
        cloud.alive = true;

        cloud.transform.position = position;
        cloud.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        cloud.transform.localScale = cloud.baseScale;
        
        cloud.renderer.GetPropertyBlock(block);
        block.SetFloat(FadeId, 0f);
        block.SetVector(SeedId, new Vector4(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f), 0f));
        cloud.renderer.SetPropertyBlock(block);
        cloud.renderer.enabled = true;
    }

    private Cloud GetFreeCloud()
    {
        int alive = 0;
        foreach (Cloud existing in clouds)
            if (existing.alive) alive++;
        if (alive >= cloudLimit) return null;

        foreach (Cloud existing in clouds)
            if (!existing.alive) return existing;

        var go = new GameObject("Dust Cloud");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = cubeMesh;

        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        var cloud = new Cloud { transform = go.transform, renderer = meshRenderer };
        clouds.Add(cloud);
        return cloud;
    }
}
