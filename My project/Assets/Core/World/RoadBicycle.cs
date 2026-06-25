using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Símbolo de bicicleta pintado en carretera — decorativo, sin colisiones.
/// Genera la mesh proceduralmente: ruedas, cuadro, manubrio, sillín y figura humana.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadBicycle : MonoBehaviour
{
    [Header("Escala global")]
    public float scale = 1.0f;

    [Header("Ruedas")]
    public int   wheelSegments  = 32;
    public float wheelRadius    = 0.38f;
    public float wheelThickness = 0.06f;

    [Header("Apariencia")]
    public Color color = Color.white;

    const float Y = 0.02f;

    void OnEnable()  => BuildMesh();
    void Start()     => BuildMesh();

    // ── helpers ────────────────────────────────────────────────────────────────
    readonly List<Vector3> _v = new List<Vector3>();
    readonly List<int>     _t = new List<int>();

    Vector3 P(float x, float z) => new Vector3(x * scale, Y, z * scale);

    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = _v.Count;
        _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
        _t.Add(i); _t.Add(i+2); _t.Add(i+1);
        _t.Add(i); _t.Add(i+3); _t.Add(i+2);
    }

    /// Anillo plano (rueda) centrado en (cx,cz)
    void AddRing(float cx, float cz, float rOuter, float rInner, int segs)
    {
        float step = Mathf.PI * 2f / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * step, a1 = (i + 1) * step;
            Vector3 o0 = P(cx + Mathf.Cos(a0) * rOuter, cz + Mathf.Sin(a0) * rOuter);
            Vector3 o1 = P(cx + Mathf.Cos(a1) * rOuter, cz + Mathf.Sin(a1) * rOuter);
            Vector3 i0 = P(cx + Mathf.Cos(a0) * rInner, cz + Mathf.Sin(a0) * rInner);
            Vector3 i1 = P(cx + Mathf.Cos(a1) * rInner, cz + Mathf.Sin(a1) * rInner);
            AddQuad(i0, o0, o1, i1);
        }
    }

    /// Barra gruesa entre dos puntos (rectángulo orientado)
    void AddBar(float x0, float z0, float x1, float z1, float w)
    {
        Vector3 dir = new Vector3(x1 - x0, 0, z1 - z0).normalized;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x);
        float hw = w * 0.5f * scale;
        Vector3 A = P(x0, z0) + perp * hw;
        Vector3 B = P(x0, z0) - perp * hw;
        Vector3 C = P(x1, z1) - perp * hw;
        Vector3 D = P(x1, z1) + perp * hw;
        AddQuad(A, B, C, D);
    }

    /// Disco sólido
    void AddDisc(float cx, float cz, float r, int segs)
    {
        Vector3 center = P(cx, cz);
        float step = Mathf.PI * 2f / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * step, a1 = (i + 1) * step;
            int idx = _v.Count;
            _v.Add(center);
            _v.Add(P(cx + Mathf.Cos(a0) * r, cz + Mathf.Sin(a0) * r));
            _v.Add(P(cx + Mathf.Cos(a1) * r, cz + Mathf.Sin(a1) * r));
            _t.Add(idx); _t.Add(idx+2); _t.Add(idx+1);
        }
    }

    // ── construcción ──────────────────────────────────────────────────────────
    void BuildMesh()
    {
        _v.Clear(); _t.Clear();

        float wr = wheelRadius;
        float wt = wheelThickness;
        float rInner = wr - wt;

        // Centros de ruedas
        float leftX  = -0.42f;
        float rightX =  0.42f;
        float wheelZ =  0f;

        // ── Ruedas ──
        AddRing(leftX,  wheelZ, wr, rInner, wheelSegments);
        AddRing(rightX, wheelZ, wr, rInner, wheelSegments);

        // ── Cuadro de la bici (triángulo principal) ──
        // Eje trasero → pedalier → horquilla delantera
        float bbX =  0.0f,  bbZ =  0.0f;   // pedalier (bottom bracket)
        float stX = -0.42f, stZ =  0.20f;  // unión trasera (tope rueda izq)
        float htX =  0.42f, htZ =  0.22f;  // cabeza horquilla (tope rueda der)
        float sdX = -0.10f, sdZ =  0.48f;  // sillín

        float barW = 0.055f;

        // Cadena inferior: eje trasero → pedalier
        AddBar(leftX, wheelZ, bbX, bbZ, barW);
        // Vaina: pedalier → unión trasera
        AddBar(bbX, bbZ, stX, stZ, barW);
        // Tubo superior: sillín → cabeza horquilla
        AddBar(sdX, sdZ, htX, htZ, barW);
        // Tubo del asiento: pedalier → sillín
        AddBar(bbX, bbZ, sdX, sdZ, barW);
        // Tubo diagonal: pedalier → cabeza horquilla
        AddBar(bbX, bbZ, htX, htZ, barW);
        // Horquilla delantera: cabeza → eje delantero
        AddBar(htX, htZ, rightX, wheelZ, barW * 0.85f);

        // ── Sillín ──
        AddBar(sdX - 0.12f, sdZ + 0.04f, sdX + 0.12f, sdZ + 0.04f, 0.05f);

        // ── Manubrio ──
        float mhX = htX + 0.04f, mhZ = htZ + 0.28f;
        AddBar(htX, htZ, mhX, mhZ, 0.05f);               // tallo
        AddBar(mhX - 0.13f, mhZ, mhX + 0.06f, mhZ + 0.08f, 0.045f); // barra

        // ── Figura humana (simplificada) ──
        // Cabeza
        AddDisc(sdX + 0.06f, sdZ + 0.42f, 0.12f, 20);

        // Torso: hombros → caderas
        AddBar(sdX + 0.06f, sdZ + 0.30f, mhX - 0.04f, mhZ + 0.05f, 0.07f);

        // Piernas (simplificadas como barras)
        AddBar(sdX + 0.04f, sdZ + 0.18f, bbX - 0.06f, bbZ + 0.12f, 0.055f);
        AddBar(sdX + 0.04f, sdZ + 0.18f, bbX + 0.08f, bbZ + 0.05f, 0.055f);

        // Brazo al manubrio
        AddBar(sdX + 0.10f, sdZ + 0.28f, mhX - 0.05f, mhZ, 0.05f);

        // ── Pedalier (disco pequeño) ──
        AddDisc(bbX, bbZ, 0.08f, 16);

        ApplyMesh();
    }

    void ApplyMesh()
    {
        Mesh mesh = new Mesh { name = "RoadBicycle" };
        mesh.vertices  = _v.ToArray();
        mesh.triangles = _t.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color");
        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

#if UNITY_EDITOR
    void OnValidate() => BuildMesh();
#endif
}
