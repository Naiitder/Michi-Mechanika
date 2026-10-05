using System.Collections.Generic;
using UnityEngine;


[DefaultExecutionOrder(200)] 
public class WeaponTrail : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator anim;
    [SerializeField] private Transform bladeBase;
    [SerializeField] private Transform bladeTip;
    [SerializeField] private float tipExtension = 0.26f;

    [Header("When its landed")]
    [SerializeField] private string[] attackStates = { "Jump_L_Attack", "Jump_R_Attack", "Attack" };
    [SerializeField] [Range(0f, 1f)] private float swingStart = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float swingEnd = 0.8f;

    [Header("Form")]
    [SerializeField] [Range(0.05f, 1.5f)] private float arcWidth = 0.7f;
    [SerializeField] [Range(0f, 1f)] private float flatten = 1f;
    [SerializeField] [Range(0, 8)] private int smoothing = 3;
    [SerializeField] private float trailDuration = 0.35f;

    [Header("Aspect")]
    [SerializeField] private Gradient glowColor = DefaultGradient();
    [SerializeField] private Material trailMaterial;

    private struct Sample
    {
        public Vector3 tip;     
        public Vector3 hand;     
        public float time;
    }

    private const int Subdivisions = 4;
    private const float EdgePos = 0.75f;   

    private readonly List<Sample> samples = new List<Sample>();
    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<Vector3> scratch = new List<Vector3>();
    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<Color> colors = new List<Color>();
    private readonly List<int> triangles = new List<int>();

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private int[] attackHashes;

    private static Gradient DefaultGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.25f, 0.65f, 1f), 0f),
                new GradientColorKey(new Color(0.08f, 0.35f, 1f), 0.5f),
                new GradientColorKey(new Color(0.05f, 0.15f, 0.9f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    private void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();

        attackHashes = new int[attackStates.Length];
        for (int i = 0; i < attackStates.Length; i++)
            attackHashes[i] = Animator.StringToHash(attackStates[i]);

        var go = new GameObject("WeaponTrail (runtime)");
        go.transform.SetParent(transform, false);

        mesh = new Mesh { name = "WeaponTrail" };
        mesh.MarkDynamic();
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.sharedMaterial = trailMaterial != null ? trailMaterial : CreateDefaultMaterial();
        meshRenderer.enabled = false;
    }
    
    private static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Michi/WeaponTrail");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return new Material(shader) { name = "WeaponTrail (runtime)" };
    }

    private void LateUpdate()
    {
        if (mesh == null || bladeBase == null || bladeTip == null) return;

        float now = Time.time;
        float life = Mathf.Max(trailDuration, 0.01f);
        
        int expired = 0;
        while (expired < samples.Count && now - samples[expired].time > life) expired++;
        if (expired > 0) samples.RemoveRange(0, expired);

        if (InSwing())
        {
            samples.Add(new Sample
            {
                tip = transform.InverseTransformPoint(bladeTip.TransformPoint(0f, tipExtension, 0f)),
                hand = transform.InverseTransformPoint(bladeBase.position),
                time = now
            });
        }

        RebuildMesh(now, life);
    }

    private void RebuildMesh(float now, float life)
    {
        int count = samples.Count;
        if (count < 3)
        {
            if (meshRenderer.enabled)
            {
                mesh.Clear();
                meshRenderer.enabled = false;
            }
            return;
        }
        
        path.Clear();
        Vector3 centroid = Vector3.zero;
        Vector3 pivot = Vector3.zero;
        float bladeLength = 0f;
        for (int i = 0; i < count; i++)
        {
            path.Add(samples[i].tip);
            centroid += samples[i].tip;
            pivot += samples[i].hand;
            bladeLength += Vector3.Distance(samples[i].tip, samples[i].hand);
        }
        centroid /= count;
        pivot /= count;
        bladeLength /= count;
        
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < count - 1; i++)
            normal += Vector3.Cross(path[i] - pivot, path[i + 1] - pivot);
        bool hasPlane = normal.sqrMagnitude > 1e-12f;
        if (hasPlane)
        {
            normal.Normalize();
            for (int i = 0; i < count; i++)
            {
                Vector3 flat = path[i] - normal * Vector3.Dot(path[i] - centroid, normal);
                path[i] = Vector3.Lerp(path[i], flat, flatten);
            }
            pivot -= normal * Vector3.Dot(pivot - centroid, normal) * flatten;
        }

      
        for (int pass = 0; pass < smoothing; pass++)
        {
            scratch.Clear();
            scratch.AddRange(path);
            for (int i = 1; i < count - 1; i++)
                path[i] = scratch[i] * 0.5f + (scratch[i - 1] + scratch[i + 1]) * 0.25f;
        }
        
        float width = bladeLength * arcWidth;
        float haloWidth = width * (1f - EdgePos) / EdgePos;

        vertices.Clear();
        uvs.Clear();
        colors.Clear();
        triangles.Clear();

        int last = count - 1;
        for (int i = 0; i < last; i++)
        {
            Vector3 p0 = path[Mathf.Max(i - 1, 0)];
            Vector3 p1 = path[i];
            Vector3 p2 = path[i + 1];
            Vector3 p3 = path[Mathf.Min(i + 2, last)];

       
            int steps = i == last - 1 ? Subdivisions + 1 : Subdivisions;
            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)Subdivisions;
                Vector3 edge = CatmullRom(p0, p1, p2, p3, t);

                Vector3 inward = pivot - edge;
                if (hasPlane) inward -= normal * Vector3.Dot(inward, normal) * flatten;
                inward = inward.sqrMagnitude > 1e-10f ? inward.normalized : Vector3.zero;

                float age = Mathf.Clamp01((now - Mathf.Lerp(samples[i].time, samples[i + 1].time, t)) / life);
                Color c = glowColor.Evaluate(age);

                vertices.Add(edge + inward * width);       
                vertices.Add(edge - inward * haloWidth);   
                uvs.Add(new Vector2(age, 0f));
                uvs.Add(new Vector2(age, 1f));
                colors.Add(c);
                colors.Add(c);
            }
        }

        for (int v = 0; v + 3 < vertices.Count; v += 2)
        {
            triangles.Add(v);
            triangles.Add(v + 1);
            triangles.Add(v + 2);
            triangles.Add(v + 2);
            triangles.Add(v + 1);
            triangles.Add(v + 3);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        meshRenderer.enabled = true;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                       (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    private bool InSwing()
    {
        if (anim == null) return false;
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        bool isAttack = false;
        for (int i = 0; i < attackHashes.Length; i++)
            if (attackHashes[i] == info.shortNameHash) { isAttack = true; break; }
        if (!isAttack) return false;

        float t = info.normalizedTime;
        return t >= swingStart && t <= swingEnd;
    }

    private void OnDisable()
    {
        samples.Clear();
        if (mesh != null) mesh.Clear();
        if (meshRenderer != null) meshRenderer.enabled = false;
    }
}
