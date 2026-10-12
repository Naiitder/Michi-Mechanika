using UnityEngine;

// Partículas de ambiente que cruzan la pantalla empujadas por el viento: motas de polvo, copos de
// ceniza que caen girando y unas pocas chispas (brasas) que vuelan rápido, alargadas en la dirección
// en que se mueven, y se enfrían (amarillo -> naranja -> rojo oscuro) hasta apagarse.
// Viven en un volumen delante de la cámara (entre nearDepth y farDepth): las que salen por un lado
// de la pantalla vuelven a entrar por el otro, así siempre hay partículas y se conserva el paralaje
// (las cercanas cruzan más rápido que las lejanas).
// Todo se dibuja en una sola llamada con el material AmbientParticles_MAT (shader Michi/AmbientParticles).
// Respeta el ajuste gráfico "Smoke and dust": Off = nada, Low = la mitad.
[DefaultExecutionOrder(1000)] // después de que Cinemachine mueva la cámara
public class AmbientParticles : MonoBehaviour
{
    private const int Dust = 0, Ash = 1, Ember = 2;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
        public Vector3 phase;
        public float size;
        public float rotation;
        public float spin;
        public float tumble;
        public float tumbleSpeed;
        public float speed;
        public float seed;
        public float tone;
        public float age;
        public int kind;
    }

    [SerializeField] private Material material;

    [Header("Cantidad")]
    [SerializeField, Range(0, 600)] private int dustCount = 28;
    [SerializeField, Range(0, 300)] private int ashCount = 10;
    [SerializeField, Range(0, 100)] private int emberCount = 3;

    [Header("Zona (distancia a la cámara)")]
    [SerializeField] private float nearDepth = 2.5f;
    [SerializeField] private float farDepth = 28f;

    [Header("Viento")]
    [Tooltip("Dirección en pantalla: x = hacia la derecha, y = hacia arriba")]
    [SerializeField] private Vector2 windDirection = new Vector2(1f, -0.1f);
    [Tooltip("Unidades por segundo")]
    [SerializeField] private float windSpeed = 2.2f;
    [Tooltip("Rachas: cuánto varía la velocidad del viento (0 = constante)")]
    [SerializeField, Range(0f, 1f)] private float gustStrength = 0.5f;
    [SerializeField] private float gustPeriod = 7f;
    [Tooltip("Vaivén de cada partícula")]
    [SerializeField] private float turbulence = 0.45f;

    [Header("Polvo")]
    [SerializeField] private Vector2 dustSize = new Vector2(0.03f, 0.075f);
    [SerializeField] private Color dustColor = new Color(1f, 0.86f, 0.66f, 0.55f);

    [Header("Ceniza")]
    [SerializeField] private Vector2 ashSize = new Vector2(0.06f, 0.15f);
    [SerializeField] private Color ashLight = new Color(0.6f, 0.57f, 0.55f, 0.95f);
    [SerializeField] private Color ashDark = new Color(0.2f, 0.18f, 0.18f, 0.95f);
    [Tooltip("Velocidad a la que cae la ceniza")]
    [SerializeField] private float ashFall = 0.45f;

    [Header("Brasas (chispas)")]
    [Tooltip("Grosor de la chispa; el largo depende de su velocidad")]
    [SerializeField] private Vector2 emberSize = new Vector2(0.02f, 0.04f);
    [ColorUsage(false, true)]
    [Tooltip("Color recién salida (caliente)")]
    [SerializeField] private Color emberColor = new Color(5f, 2.2f, 0.5f, 1f);
    [ColorUsage(false, true)]
    [Tooltip("Color justo antes de apagarse (fría)")]
    [SerializeField] private Color emberCoolColor = new Color(0.9f, 0.12f, 0.03f, 1f);
    [Tooltip("Segundos que tarda en enfriarse y apagarse")]
    [SerializeField] private Vector2 emberLifetime = new Vector2(1.2f, 2.8f);
    [Tooltip("Cuánto más rápido que el viento vuelan las chispas")]
    [SerializeField] private float emberSpeed = 1.8f;
    [Tooltip("Velocidad de subida al salir (luego caen un poco al perder fuerza)")]
    [SerializeField] private float emberRise = 1.2f;

    private Particle[] particles;
    private Camera cam;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector4[] uv0;
    private Vector4[] uv1;
    private Vector3[] uv2;
    private Color[] colors;
    private float clock;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();

        int total = dustCount + ashCount + emberCount;
        particles = new Particle[total];
        int k = 0;
        for (int i = 0; i < dustCount; i++) particles[k++] = NewParticle(Dust);
        for (int i = 0; i < ashCount; i++) particles[k++] = NewParticle(Ash);
        for (int i = 0; i < emberCount; i++) particles[k++] = NewParticle(Ember);

        vertices = new Vector3[total * 4];
        uv0 = new Vector4[total * 4];
        uv1 = new Vector4[total * 4];
        uv2 = new Vector3[total * 4];
        colors = new Color[total * 4];
        var indices = new int[total * 6];
        for (int i = 0; i < total; i++)
        {
            int v = i * 4, t = i * 6;
            uv0[v] = new Vector4(-1, -1); uv0[v + 1] = new Vector4(1, -1);
            uv0[v + 2] = new Vector4(1, 1); uv0[v + 3] = new Vector4(-1, 1);
            indices[t] = v; indices[t + 1] = v + 2; indices[t + 2] = v + 1;
            indices[t + 3] = v; indices[t + 4] = v + 3; indices[t + 5] = v + 2;
        }

        mesh = new Mesh { name = "AmbientParticles", hideFlags = HideFlags.HideAndDontSave };
        mesh.MarkDynamic();
        if (total * 4 > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetUVs(2, uv2);
        mesh.colors = colors;
        mesh.SetTriangles(indices, 0);

        // Repartidas por todo el volumen desde el principio.
        if (cam != null)
            for (int i = 0; i < particles.Length; i++)
                particles[i].position = RandomInView(Random.value, Random.value, Random.Range(nearDepth, farDepth));
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
    }

    private Particle NewParticle(int kind)
    {
        var p = new Particle
        {
            kind = kind,
            seed = Random.value,
            tone = Random.value,
            phase = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f)),
            rotation = Random.Range(0f, Mathf.PI * 2f),
            tumble = Random.Range(0f, Mathf.PI * 2f),
            age = 10f
        };
        switch (kind)
        {
            case Dust:
                p.size = Random.Range(dustSize.x, dustSize.y);
                p.speed = Random.Range(0.6f, 1.1f);
                p.spin = 0f;
                p.tumbleSpeed = 0f;
                break;
            case Ash:
                p.size = Random.Range(ashSize.x, ashSize.y);
                p.speed = Random.Range(0.75f, 1.2f);
                p.spin = Random.Range(-2.5f, 2.5f);
                p.tumbleSpeed = Random.Range(1.5f, 4.5f);
                break;
            default:
                p.size = Random.Range(emberSize.x, emberSize.y);
                p.speed = Random.Range(0.8f, 1.2f) * emberSpeed;
                p.spin = 0f;
                p.tumbleSpeed = 0f;
                p.life = Random.Range(emberLifetime.x, emberLifetime.y);
                // Empiezan a mitad de vida para que no salgan todas a la vez al cargar.
                p.age = Random.Range(0f, p.life);
                break;
        }
        return p;
    }

    // Punto en el volumen visible: u, v en 0..1 sobre la pantalla, a la distancia dada.
    private Vector3 RandomInView(float u, float v, float depth)
    {
        float halfH = depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
        float halfW = halfH * cam.aspect;
        Transform t = cam.transform;
        return t.position + t.forward * depth + t.right * ((u * 2f - 1f) * halfW) + t.up * ((v * 2f - 1f) * halfH);
    }

    private void LateUpdate()
    {
        if (cam == null || material == null || particles == null) return;

        int quality = GraphicsOptions.SmokeLevel;
        if (quality == GraphicsOptions.Off) return;
        bool low = quality == GraphicsOptions.Low;

        float dt = Time.deltaTime;
        clock += dt;

        Transform ct = cam.transform;
        Vector3 right = ct.right; right.y = 0f;
        if (right.sqrMagnitude < 1e-4f) right = Vector3.right;
        right.Normalize();
        Vector2 wd = windDirection.sqrMagnitude > 1e-4f ? windDirection.normalized : Vector2.right;
        float gust = 1f + gustStrength * (Mathf.PerlinNoise(clock / Mathf.Max(0.1f, gustPeriod), 0.37f) - 0.5f) * 2f;
        Vector3 wind = (right * wd.x + Vector3.up * wd.y) * (windSpeed * gust);

        float tanHalf = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.15f;
        Vector3 camPos = ct.position;
        Vector3 fwd = ct.forward, up = ct.up, camRight = ct.right;

        int dustShown = low ? dustCount / 2 : dustCount;
        int ashShown = low ? ashCount / 2 : ashCount;
        int emberShown = low ? emberCount / 2 : emberCount;

        for (int i = 0; i < particles.Length; i++)
        {
            ref Particle p = ref particles[i];

            Vector3 velocity = wind * p.speed;
            float t = clock;
            velocity += new Vector3(
                Mathf.Sin(t * 0.9f + p.phase.x),
                Mathf.Sin(t * 1.3f + p.phase.y) * 0.6f,
                Mathf.Sin(t * 0.7f + p.phase.z)) * turbulence;
            if (p.kind == Ash) velocity.y -= ashFall;
            else if (p.kind == Ember)
            {
                // Sale hacia arriba con fuerza y la va perdiendo; nada de vaivén (eso las hacía luciérnagas).
                float u = Mathf.Clamp01(p.age / p.life);
                velocity = wind * p.speed + Vector3.up * (emberRise * (1f - 1.6f * u));
            }
            p.velocity = velocity;

            p.position += velocity * dt;
            p.rotation += p.spin * dt;
            p.tumble += p.tumbleSpeed * dt;
            p.age += dt;

            // Chispa apagada: nace otra en otro punto de la vista.
            if (p.kind == Ember && p.age >= p.life)
            {
                p.position = RandomInView(Random.Range(0f, 0.8f), Random.Range(0f, 0.6f), Random.Range(nearDepth + 2f, farDepth * 0.7f));
                p.life = Random.Range(emberLifetime.x, emberLifetime.y);
                p.age = 0f;
            }

            // Volver a meterla en el volumen visible por el lado contrario.
            Vector3 rel = p.position - camPos;
            float depth = Vector3.Dot(rel, fwd);
            if (depth < nearDepth || depth > farDepth)
            {
                // La cámara ha avanzado o retrocedido: nueva posición en el volumen.
                p.position = RandomInView(Random.value, Random.value, Random.Range(nearDepth, farDepth));
                p.age = 0f;
            }
            else
            {
                float halfH = depth * tanHalf;
                float halfW = halfH * cam.aspect;
                float x = Vector3.Dot(rel, camRight);
                float y = Vector3.Dot(rel, up);
                bool moved = false;
                if (x > halfW) { x -= 2f * halfW; moved = true; }
                else if (x < -halfW) { x += 2f * halfW; moved = true; }
                if (y > halfH) { y -= 2f * halfH; moved = true; }
                else if (y < -halfH) { y += 2f * halfH; moved = true; }
                if (moved)
                {
                    p.position = camPos + fwd * depth + camRight * x + up * y;
                    p.age = 0f;
                }
            }

            bool shown = p.kind == Dust ? i < dustShown
                : p.kind == Ash ? i - dustCount < ashShown
                : i - dustCount - ashCount < emberShown;

            Color c;
            float appear;
            if (p.kind == Ember)
            {
                // Se enfría y se apaga a lo largo de su vida.
                float u = Mathf.Clamp01(p.age / p.life);
                c = Color.Lerp(emberColor, emberCoolColor, Mathf.Pow(u, 0.7f));
                appear = Mathf.Clamp01(p.age / 0.08f) * (1f - Mathf.SmoothStep(0.55f, 1f, u));
            }
            else
            {
                c = p.kind == Dust ? dustColor : Color.Lerp(ashDark, ashLight, p.tone);
                // Aparece suave al reentrar (sin "pops").
                appear = Mathf.Clamp01(p.age / 0.6f);
            }

            int v0 = i * 4;
            Vector4 data = new Vector4(shown ? p.size : 0f, p.rotation, p.kind == Ash ? Mathf.Cos(p.tumble) : 1f, appear);
            for (int k = 0; k < 4; k++)
            {
                vertices[v0 + k] = p.position;
                uv0[v0 + k].z = p.kind;
                uv0[v0 + k].w = p.seed;
                uv1[v0 + k] = data;
                uv2[v0 + k] = p.velocity;
                colors[v0 + k] = c;
            }
        }

        mesh.vertices = vertices;
        mesh.SetUVs(0, uv0);
        mesh.SetUVs(1, uv1);
        mesh.SetUVs(2, uv2);
        mesh.colors = colors;
        mesh.bounds = new Bounds(camPos + fwd * (farDepth * 0.5f), Vector3.one * (farDepth * 2.5f));

        var rp = new RenderParams(material)
        {
            layer = gameObject.layer,
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
            receiveShadows = false,
            worldBounds = mesh.bounds
        };
        Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.identity);
    }
}
