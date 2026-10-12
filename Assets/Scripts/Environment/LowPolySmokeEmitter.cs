using System.Collections.Generic;
using UnityEngine;

// Humo de tubería estilizado: la boca está en la posición del objeto y el humo sale en la dirección de
// su eje Y (gíralo para que apunte por donde sale la tubería).
// Cada bocanada es un racimo de bultos redondos (forma de nube) que sale disparada por una boca estrecha, a golpes
// rítmicos; se frena enseguida, sube flotando, se ondula con turbulencia, se hincha y al final se
// disuelve (el shader se la va comiendo con agujeros).
// Se usa con el material Smoke_LowPoly_MAT (shader Michi/StylizedSmoke).
// Respeta el ajuste gráfico "Smoke and dust": Off = nada, Low = la mitad de bocanadas.
public class LowPolySmokeEmitter : MonoBehaviour
{
    private class Puff
    {
        public Transform transform;
        public MeshRenderer renderer;
        public Vector3 velocity;
        public Quaternion spin;
        public Vector3 noiseSeed;
        public float startSize;
        public Color color;
        public float age;
        public float lifetime;
        public bool alive;
    }

    [SerializeField] private Material material;

    [Header("Cantidad")]
    [Tooltip("Bocanadas por segundo (de media)")]
    [SerializeField] private float rate = 7f;
    [SerializeField] private Vector2 lifetime = new Vector2(3.5f, 5.5f);
    [SerializeField, Range(4, 100)] private int maxPuffs = 45;

    [Header("Golpes de vapor")]
    [Tooltip("Segundos entre golpe y golpe")]
    [SerializeField] private float pulseInterval = 0.7f;
    [Tooltip("0 = salida continua, 1 = casi todo sale en golpes")]
    [SerializeField, Range(0f, 1f)] private float pulseStrength = 0.6f;

    [Header("Tamaño")]
    [Tooltip("Tamaño al nacer (diámetro en unidades); más o menos el ancho de la boca")]
    [SerializeField] private Vector2 startSize = new Vector2(0.4f, 0.65f);
    [Tooltip("Cuántas veces más grande es al final de su vida")]
    [SerializeField] private float endSizeMultiplier = 4.5f;

    [Header("Disolución")]
    [Tooltip("Cuánto de la bocanada existe al nacer (1 = entera)")]
    [SerializeField, Range(0f, 1f)] private float startOpacity = 1f;
    [Tooltip("Parte de la vida (0-1) en la que sigue entera antes de empezar a disolverse")]
    [SerializeField, Range(0f, 0.9f)] private float holdOpacity = 0.3f;
    [Tooltip("Forma de la disolución: 1 = constante, más alto = se agujerea rápido y los últimos jirones duran más")]
    [SerializeField, Range(0.5f, 4f)] private float fadeCurve = 1f;

    [Header("Tonos")]
    [Tooltip("Tonos posibles de cada bocanada; se elige uno al azar al nacer")]
    [SerializeField] private Color[] tones =
    {
        new Color(0.97f, 0.97f, 0.97f), // blanca
        new Color(0.9f, 0.9f, 0.91f),   // blanca grisácea
        new Color(0.76f, 0.76f, 0.78f), // gris clarito
        new Color(0.56f, 0.56f, 0.59f)  // gris oscuro
    };
    [Tooltip("Variación aleatoria de brillo sobre el tono elegido")]
    [SerializeField, Range(0f, 0.3f)] private float toneJitter = 0.05f;
    [Tooltip("Cuánto se acercan a un gris medio al dispersarse (0 = nada)")]
    [SerializeField, Range(0f, 1f)] private float blendToGrey = 0.3f;

    [Header("Chorro")]
    [Tooltip("Velocidad con la que sale por la boca")]
    [SerializeField] private float jetSpeed = 7f;
    [Tooltip("Cuánto se frena al salir (más alto = el chorro es más corto)")]
    [SerializeField, Range(0f, 4f)] private float drag = 1.6f;
    [Tooltip("Velocidad final hacia la que tiende cada bocanada (viento + flotación)")]
    [SerializeField] private Vector3 wind = new Vector3(0.5f, 1.1f, 0.15f);
    [Tooltip("Radio de la boca de la tubería")]
    [SerializeField] private float spawnRadius = 0.12f;
    [Tooltip("Velocidad lateral aleatoria al salir (abre el cono del chorro)")]
    [SerializeField] private float spread = 0.25f;
    [Tooltip("Ondulación del humo; crece con la edad de la bocanada")]
    [SerializeField] private float turbulence = 1.2f;
    [SerializeField] private float turbulenceFrequency = 0.8f;
    [Tooltip("Giro máximo de cada bocanada, grados por segundo")]
    [SerializeField] private float spinSpeed = 40f;

    [Header("Forma")]
    [Tooltip("Bultos de cada bocanada: 1 = una esfera, más = más forma de nube")]
    [SerializeField, Range(1, 8)] private int lobes = 5;
    [Tooltip("Subdivisiones de cada bulto: 2 = 320 triángulos, 3 = 1280")]
    [SerializeField, Range(1, 3)] private int detail = 2;
    [Tooltip("Cuánto se deforma cada bulto (bultos suaves)")]
    [SerializeField, Range(0f, 0.35f)] private float roughness = 0.08f;

    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int GlowId = Shader.PropertyToID("_GlowFade");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int ColorId = Shader.PropertyToID("_PuffColor");
    private static readonly Color DispersedGrey = new Color(0.6f, 0.59f, 0.6f);

    private const int MeshVariants = 6;
    private static readonly Dictionary<long, Mesh[]> MeshCache = new Dictionary<long, Mesh[]>();

    private readonly List<Puff> puffs = new List<Puff>();
    private MaterialPropertyBlock block;
    private Mesh[] meshes;
    private float spawnDebt;
    private float clock;

    private void Start()
    {
        block = new MaterialPropertyBlock();
        meshes = GetMeshes(lobes, detail, roughness);

        // Que el chorro ya esté formado al cargar el nivel.
        float warm = lifetime.y;
        const float step = 1f / 20f;
        for (float t = 0f; t < warm; t += step) Simulate(step);
    }

    private void Update()
    {
        Simulate(Time.deltaTime);
    }

    private void Simulate(float dt)
    {
        if (material == null) return;

        int quality = GraphicsOptions.SmokeLevel;
        if (quality == GraphicsOptions.Off)
        {
            foreach (Puff p in puffs)
            {
                if (!p.alive) continue;
                p.alive = false;
                p.renderer.enabled = false;
            }
            return;
        }

        clock += dt;

        // Golpes: justo al empezar cada golpe sale mucho humo y más rápido, luego casi nada.
        float pulse = 0f;
        if (pulseInterval > 0.01f)
        {
            float phase = Mathf.Repeat(clock / pulseInterval, 1f);
            pulse = Mathf.Pow(1f - phase, 3f);
        }
        float emission = 1f - pulseStrength + pulseStrength * 4f * pulse; // media = 1
        float currentRate = rate * emission * (quality == GraphicsOptions.Low ? 0.5f : 1f);

        spawnDebt += currentRate * dt;
        while (spawnDebt >= 1f)
        {
            spawnDebt -= 1f;
            Spawn(pulse);
        }

        float damping = Mathf.Exp(-drag * dt);
        foreach (Puff p in puffs)
        {
            if (!p.alive) continue;

            p.age += dt;
            float u = p.age / p.lifetime;
            if (u >= 1f)
            {
                p.alive = false;
                p.renderer.enabled = false;
                continue;
            }

            // Turbulencia suave que va a más con la edad: el humo se ondula al alejarse de la boca.
            float nt = clock * turbulenceFrequency;
            Vector3 swirl = new Vector3(
                Mathf.PerlinNoise(p.noiseSeed.x + nt, 0.31f) - 0.5f,
                (Mathf.PerlinNoise(p.noiseSeed.y + nt, 1.73f) - 0.5f) * 0.5f,
                Mathf.PerlinNoise(p.noiseSeed.z + nt, 2.97f) - 0.5f) * (2f * turbulence * u);

            p.velocity = p.velocity * damping + wind * (1f - damping) + swirl * dt;
            p.transform.position += p.velocity * dt;
            p.transform.rotation = Quaternion.SlerpUnclamped(Quaternion.identity, p.spin, dt * (1f - 0.6f * u)) * p.transform.rotation;

            // Tamaño: brota en la boca y se abre deprisa al principio, cada vez más despacio después.
            float pop = Mathf.Lerp(0.5f, 1f, Mathf.SmoothStep(0f, 1f, u / 0.06f));
            float swell = 1f - Mathf.Pow(1f - u, 2.5f);
            float size = p.startSize * pop * Mathf.Lerp(1f, endSizeMultiplier, swell);
            p.transform.localScale = Vector3.one * size;

            // Opacidad: opaca al salir y se va volviendo invisible poco a poco.
            float fadeU = Mathf.Clamp01((u - holdOpacity) / Mathf.Max(0.001f, 1f - holdOpacity));
            float alpha = startOpacity * Mathf.Pow(1f - fadeU, fadeCurve);

            p.renderer.GetPropertyBlock(block);
            block.SetFloat(AlphaId, alpha);
            block.SetFloat(GlowId, 1f - Mathf.Clamp01(u * 3f));
            block.SetColor(ColorId, Color.Lerp(p.color, DispersedGrey, blendToGrey * u));
            p.renderer.SetPropertyBlock(block);
        }
    }

    private void Spawn(float pulse)
    {
        Puff p = GetFreePuff();
        if (p == null) return;

        Vector3 up = transform.up;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        Vector2 disc = Random.insideUnitCircle;
        Vector2 side = Random.insideUnitCircle * spread * jetSpeed * 0.15f;

        float speed = jetSpeed * Random.Range(0.85f, 1.1f) * (1f + 0.5f * pulseStrength * pulse);

        p.transform.position = transform.position + (right * disc.x + forward * disc.y) * spawnRadius;
        p.transform.rotation = Random.rotation;
        p.velocity = up * speed + right * side.x + forward * side.y;
        p.spin = Quaternion.Euler(Random.insideUnitSphere * spinSpeed);
        p.noiseSeed = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));
        p.startSize = Random.Range(startSize.x, startSize.y);
        Color tone = tones != null && tones.Length > 0 ? tones[Random.Range(0, tones.Length)] : Color.grey;
        float jitter = 1f + Random.Range(-toneJitter, toneJitter);
        p.color = new Color(tone.r * jitter, tone.g * jitter, tone.b * jitter, 1f);
        p.lifetime = Random.Range(lifetime.x, lifetime.y);
        p.age = 0f;
        p.alive = true;
        p.transform.localScale = Vector3.one * (p.startSize * 0.5f);

        p.renderer.GetComponent<MeshFilter>().sharedMesh = meshes[Random.Range(0, meshes.Length)];
        p.renderer.GetPropertyBlock(block);
        block.SetFloat(AlphaId, startOpacity);
        block.SetFloat(GlowId, 1f);
        block.SetColor(ColorId, p.color);
        block.SetVector(SeedId, new Vector4(Random.Range(-50f, 50f), Random.Range(-50f, 50f), Random.Range(-50f, 50f), 0f));
        p.renderer.SetPropertyBlock(block);
        p.renderer.enabled = true;
    }

    private Puff GetFreePuff()
    {
        Puff free = null;
        int alive = 0;
        foreach (Puff p in puffs)
        {
            if (p.alive) alive++;
            else if (free == null) free = p;
        }
        if (alive >= maxPuffs) return null;
        if (free != null) return free;

        var go = new GameObject("Smoke Puff");
        go.transform.SetParent(transform, true);
        go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        var puff = new Puff { transform = go.transform, renderer = meshRenderer };
        puffs.Add(puff);
        return puff;
    }

    // ------------------------------------------------------------------ Mallas

    private static Mesh[] GetMeshes(int lobeCount, int subdivisions, float rough)
    {
        long key = (lobeCount * 10L + subdivisions) * 1000L + Mathf.RoundToInt(rough * 100f);
        if (MeshCache.TryGetValue(key, out Mesh[] cached) && cached[0] != null) return cached;

        var result = new Mesh[MeshVariants];
        for (int i = 0; i < MeshVariants; i++) result[i] = BuildPuff(1000 + i * 7919, lobeCount, subdivisions, rough);
        MeshCache[key] = result;
        return result;
    }

    // Nube de ~1 de diámetro: un bulto central grande y varios más pequeños alrededor (más hacia
    // arriba y a los lados que hacia abajo), cada uno una esfera lisa con normales suaves.
    private static Mesh BuildPuff(int seed, int lobeCount, int subdivisions, float lump)
    {
        var random = new System.Random(seed);
        float Rand(float a, float b) => a + (float)random.NextDouble() * (b - a);

        Ico(subdivisions, out List<Vector3> unit, out List<int> unitTris);

        var positions = new List<Vector3>();
        var tris = new List<int>();

        for (int l = 0; l < lobeCount; l++)
        {
            Vector3 center;
            float radius;
            if (l == 0)
            {
                center = new Vector3(0f, -0.03f, 0f);
                radius = lobeCount == 1 ? 0.5f : 0.3f;
            }
            else
            {
                // Repartidos alrededor en ángulo, con algo de azar, y sobre todo en la mitad de arriba.
                float angle = (l - 1) / (float)(lobeCount - 1) * Mathf.PI * 2f + Rand(-0.5f, 0.5f);
                float elevation = Rand(-0.25f, 0.9f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), elevation, Mathf.Sin(angle)).normalized;
                radius = Rand(0.17f, 0.25f);
                center = dir * Rand(0.17f, 0.25f);
            }
            float limit = 0.5f - radius * (1f + lump);
            if (center.magnitude > limit) center = center.normalized * Mathf.Max(0f, limit);

            // Bultos suaves por esfera: varias ondas de baja frecuencia sumadas.
            var dirs = new Vector3[3];
            var phases = new float[3];
            for (int k = 0; k < 3; k++)
            {
                dirs[k] = new Vector3(Rand(-1f, 1f), Rand(-1f, 1f), Rand(-1f, 1f)).normalized;
                phases[k] = Rand(0f, 6.2831853f);
            }

            int baseIndex = positions.Count;
            foreach (Vector3 d in unit)
            {
                float bump = 0f;
                for (int k = 0; k < 3; k++) bump += Mathf.Sin(Vector3.Dot(d, dirs[k]) * 2.4f + phases[k]);
                positions.Add(center + d * radius * (1f + lump * bump / 3f * 1.5f));
            }
            for (int t = 0; t < unitTris.Count; t += 3)
            {
                int a = unitTris[t], b = unitTris[t + 1], c = unitTris[t + 2];
                Vector3 pa = positions[baseIndex + a], pb = positions[baseIndex + b], pc = positions[baseIndex + c];
                if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), (pa + pb + pc) / 3f - center) < 0f)
                    (b, c) = (c, b);
                tris.Add(baseIndex + a); tris.Add(baseIndex + b); tris.Add(baseIndex + c);
            }
        }

        var mesh = new Mesh { name = "StylizedSmokePuff", hideFlags = HideFlags.HideAndDontSave };
        if (positions.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(positions);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1.5f);
        return mesh;
    }

    private static void Ico(int subdivisions, out List<Vector3> verts, out List<int> tris)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        verts = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
        };
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;

        tris = new List<int>
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
        };

        for (int s = 0; s < subdivisions; s++)
        {
            var midCache = new Dictionary<long, int>();
            var next = new List<int>();
            List<Vector3> vv = verts;
            int Mid(int a, int b)
            {
                long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (midCache.TryGetValue(k, out int idx)) return idx;
                vv.Add(((vv[a] + vv[b]) * 0.5f).normalized);
                midCache[k] = vv.Count - 1;
                return vv.Count - 1;
            }
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris = next;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * (jetSpeed / Mathf.Max(0.1f, drag)));
    }
}
