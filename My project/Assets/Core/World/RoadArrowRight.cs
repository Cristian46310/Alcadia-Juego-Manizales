using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flecha de carretera: recto + giro derecha (↑→) — decorativa, sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadArrowRight : MonoBehaviour
{
    [Header("Tallo común (base)")]
    public float stemWidth   = 0.50f;
    public float stemLength  = 0.80f;

    [Header("Brazo recto (↑)")]
    public float straightWidth  = 0.22f;
    public float straightLength = 1.10f;
    public float straightHead   = 0.45f;

    [Header("Brazo derecha (→)")]
    public float rightWidth  = 0.22f;
    public float rightLength = 0.90f;
    public float rightHead   = 0.45f;

    [Header("Apariencia")]
    public Color color = Color.white;

    const float Y = 0.02f;

    void OnEnable()  => BuildMesh();
    void Start()     => BuildMesh();

    void BuildMesh()
    {
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        float hw = stemWidth     * 0.5f;
        float sw = straightWidth * 0.5f;
        float rw = rightWidth    * 0.5f;

        float stemTop     = stemLength;
        float straightTop = stemLength + straightLength;
        float straightTip = straightTop + straightHead;

        // Brazo derecho: sale desde la esquina superior-derecha del tallo en +X
        float armCenterZ = stemTop + rw;
        float armRight   = hw + rightLength;   // extremo derecho
        float armTip     = armRight + rightHead; // punta de la cabeza

        // ── 1. TALLO ──────────────────────────────────────────────────────────────
        AddQuad(verts, tris,
            V(-hw, 0),
            V( hw, 0),
            V( hw, armCenterZ + rw),
            V(-hw, armCenterZ + rw));

        // ── 2. BRAZO RECTO (↑) ────────────────────────────────────────────────────
        AddQuad(verts, tris,
            V(-hw, stemTop),
            V( sw, stemTop),
            V( sw, straightTop),
            V(-hw, straightTop));

        // Cabeza recta
        AddTri(verts, tris,
            V(-(hw + straightHead * 0.3f), straightTop),
            V(  straightHead,              straightTop),
            V( (sw - hw) * 0.5f,           straightTip));

        // ── 3. BRAZO DERECHO (→) ──────────────────────────────────────────────────
        AddQuad(verts, tris,
            V( hw,       armCenterZ - rw),
            V( armRight, armCenterZ - rw),
            V( armRight, armCenterZ + rw),
            V( hw,       armCenterZ + rw));

        // Cabeza derecha (triángulo apunta a +X)
        AddTri(verts, tris,
            V(armRight, armCenterZ - rightHead),
            V(armRight, armCenterZ + rightHead),
            V(armTip,   armCenterZ));

        ApplyMesh(verts.ToArray(), tris.ToArray());
    }

    Vector3 V(float x, float z) => new Vector3(x, Y, z);

    static void AddQuad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(i); t.Add(i+2); t.Add(i+1);
        t.Add(i); t.Add(i+3); t.Add(i+2);
    }

    static void AddTri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        if (Vector3.Dot(Vector3.Cross(b - a, c - a), Vector3.up) > 0f)
        { t.Add(i); t.Add(i+1); t.Add(i+2); }
        else
        { t.Add(i); t.Add(i+2); t.Add(i+1); }
    }

    void ApplyMesh(Vector3[] verts, int[] tris)
    {
        Mesh mesh = new Mesh { name = "ArrowStraightRight" };
        mesh.vertices  = verts;
        mesh.triangles = tris;
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
