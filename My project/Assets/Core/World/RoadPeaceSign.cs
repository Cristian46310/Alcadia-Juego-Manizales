using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Símbolo de paz pintado en carretera — decorativo, sin colisiones.
/// Círculo exterior + línea vertical + dos líneas diagonales inferiores.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadPeaceSign : MonoBehaviour
{
    [Header("Tamaño")]
    public float radius        = 0.55f;   // radio del círculo exterior
    public float ringThickness = 0.07f;   // grosor del anillo
    public float lineWidth     = 0.06f;   // grosor de las líneas internas
    public int   segments      = 48;

    [Header("Apariencia")]
    public Color color = Color.white;

    const float Y = 0.02f;

    void OnEnable()  => BuildMesh();
    void Start()     => BuildMesh();

    readonly List<Vector3> _v = new List<Vector3>();
    readonly List<int>     _t = new List<int>();

    Vector3 P(float x, float z) => new Vector3(x, Y, z);

    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = _v.Count;
        _v.Add(a); _v.Add(b); _v.Add(c); _v.Add(d);
        _t.Add(i); _t.Add(i+2); _t.Add(i+1);
        _t.Add(i); _t.Add(i+3); _t.Add(i+2);
    }

    void AddRing(float cx, float cz, float rOuter, float rInner, int segs)
    {
        float step = Mathf.PI * 2f / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * step, a1 = (i + 1) * step;
            Vector3 o0 = P(cx + Mathf.Cos(a0)*rOuter, cz + Mathf.Sin(a0)*rOuter);
            Vector3 o1 = P(cx + Mathf.Cos(a1)*rOuter, cz + Mathf.Sin(a1)*rOuter);
            Vector3 i0 = P(cx + Mathf.Cos(a0)*rInner, cz + Mathf.Sin(a0)*rInner);
            Vector3 i1 = P(cx + Mathf.Cos(a1)*rInner, cz + Mathf.Sin(a1)*rInner);
            AddQuad(i0, o0, o1, i1);
        }
    }

    void AddBar(float x0, float z0, float x1, float z1, float w)
    {
        Vector3 dir  = new Vector3(x1-x0, 0, z1-z0).normalized;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x);
        float hw = w * 0.5f;
        AddQuad(
            P(x0,z0) + perp*hw,
            P(x0,z0) - perp*hw,
            P(x1,z1) - perp*hw,
            P(x1,z1) + perp*hw);
    }

    void BuildMesh()
    {
        _v.Clear(); _t.Clear();

        float r  = radius;
        float ri = r - ringThickness;
        float lw = lineWidth;

        // ── Anillo exterior ──
        AddRing(0, 0, r, ri, segments);

        // ── Línea vertical (de la cima al fondo) ──
        AddBar(0, r, 0, -r, lw);

        // ── Diagonal inferior izquierda (de centro a esquina inf-izq) ──
        // El símbolo de paz: desde el centro bajan dos líneas a ~210° y ~330°
        float ang1 = 210f * Mathf.Deg2Rad;
        float ang2 = 330f * Mathf.Deg2Rad;
        AddBar(0, 0, Mathf.Cos(ang1)*r, Mathf.Sin(ang1)*r, lw);
        AddBar(0, 0, Mathf.Cos(ang2)*r, Mathf.Sin(ang2)*r, lw);

        ApplyMesh();
    }

    void ApplyMesh()
    {
        Mesh mesh = new Mesh { name = "RoadPeaceSign" };
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
