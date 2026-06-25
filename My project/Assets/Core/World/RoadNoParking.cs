using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Símbolo de No Parqueo — círculo + letra R + diagonal tachadora.
/// Decorativo, sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadNoParking : MonoBehaviour
{
    [Header("Tamaño")]
    public float radius        = 0.50f;
    public float ringThickness = 0.07f;
    public float lineWidth     = 0.07f;   // grosor de la diagonal y trazos de la R
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

    void AddRing(float cx, float cz, float rOuter, float rInner, int segs)
    {
        float step = Mathf.PI * 2f / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i * step, a1 = (i+1) * step;
            Vector3 o0 = P(cx + Mathf.Cos(a0)*rOuter, cz + Mathf.Sin(a0)*rOuter);
            Vector3 o1 = P(cx + Mathf.Cos(a1)*rOuter, cz + Mathf.Sin(a1)*rOuter);
            Vector3 i0 = P(cx + Mathf.Cos(a0)*rInner, cz + Mathf.Sin(a0)*rInner);
            Vector3 i1 = P(cx + Mathf.Cos(a1)*rInner, cz + Mathf.Sin(a1)*rInner);
            AddQuad(i0, o0, o1, i1);
        }
    }

    /// Arco parcial (para la curva de la R)
    void AddArc(float cx, float cz, float rOuter, float rInner,
                float fromDeg, float toDeg, int segs)
    {
        float from = fromDeg * Mathf.Deg2Rad;
        float to   = toDeg   * Mathf.Deg2Rad;
        float step = (to - from) / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = from + i * step;
            float a1 = from + (i+1) * step;
            Vector3 o0 = P(cx + Mathf.Cos(a0)*rOuter, cz + Mathf.Sin(a0)*rOuter);
            Vector3 o1 = P(cx + Mathf.Cos(a1)*rOuter, cz + Mathf.Sin(a1)*rOuter);
            Vector3 i0 = P(cx + Mathf.Cos(a0)*rInner, cz + Mathf.Sin(a0)*rInner);
            Vector3 i1 = P(cx + Mathf.Cos(a1)*rInner, cz + Mathf.Sin(a1)*rInner);
            AddQuad(i0, o0, o1, i1);
        }
    }

    void BuildMesh()
    {
        _v.Clear(); _t.Clear();

        float r  = radius;
        float ri = r - ringThickness;
        float lw = lineWidth;
        float lh = lw * 0.5f;

        // ── Anillo exterior ──────────────────────────────────────────────────
        AddRing(0, 0, r, ri, segments);

        // ── Letra R ──────────────────────────────────────────────────────────
        // La R está centrada ligeramente a la izquierda del círculo
        // Coordenadas en espacio local (X=der, Z=arriba en la carretera)
        //
        //   palo vertical: de z=-0.22 a z=+0.22, en x=-0.08
        //   joroba: semicírculo a la derecha del palo, de z=0.02 a z=0.22
        //   pata:   diagonal desde (x=-0.08,z=0.02) hacia (x=0.16,z=-0.22)

        float rPoleX  = -0.08f;          // x del palo vertical
        float rBottom = -0.22f;          // z inferior de la R
        float rTop    =  0.22f;          // z superior de la R
        float rMidZ   =  0.02f;          // z donde se divide joroba/pata
        float rArcCX  =  rPoleX + lh;   // centro X del arco de la joroba
        float rArcCZ  = (rTop + rMidZ) * 0.5f; // centro Z del arco
        float rArcR   = (rTop - rMidZ) * 0.5f; // radio del arco

        // Palo vertical
        AddBar(rPoleX, rBottom, rPoleX, rTop, lw);

        // Joroba (semicírculo derecho, de 270° a 90° = lado derecho)
        AddArc(rArcCX, rArcCZ, rArcR + lh, rArcR - lh, -90f, 90f, 16);

        // Pata diagonal (baja desde la unión con la joroba hacia la derecha)
        AddBar(rPoleX, rMidZ, rPoleX + 0.26f, rBottom, lw * 0.9f);

        // Barra horizontal superior de la joroba (cierra con el palo)
        AddBar(rPoleX, rTop,  rArcCX, rTop,  lw);
        AddBar(rPoleX, rMidZ, rArcCX, rMidZ, lw);

        // ── Diagonal tachadora (de esquina sup-izq a esquina inf-der) ──────
        float diag = r * 0.92f;
        AddBar(-diag * 0.60f,  diag * 0.60f,
                diag * 0.60f, -diag * 0.60f, lw * 1.1f);

        ApplyMesh();
    }

    void ApplyMesh()
    {
        Mesh mesh = new Mesh { name = "RoadNoParking" };
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
