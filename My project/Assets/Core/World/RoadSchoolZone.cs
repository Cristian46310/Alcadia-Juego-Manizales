using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Símbolo de zona escolar — rombo amarillo con figura humana caminando.
/// Decorativo, sin colisiones. La figura se genera en color oscuro sobre el rombo.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadSchoolZone : MonoBehaviour
{
    [Header("Rombo")]
    public float diamondWidth  = 1.60f;
    public float diamondHeight = 1.10f;

    [Header("Escala figura")]
    public float figureScale = 1.0f;

    [Header("Colores")]
    public Color diamondColor = new Color(1f, 0.78f, 0f);   // amarillo
    public Color figureColor  = new Color(0.15f, 0.15f, 0.15f); // gris oscuro

    const float Y        = 0.02f;
    const float Y_FIGURE = 0.03f; // ligeramente sobre el rombo

    // ── listas separadas para rombo y figura ────────────────────────────────
    readonly List<Vector3> _vD = new List<Vector3>();
    readonly List<int>     _tD = new List<int>();
    readonly List<Vector3> _vF = new List<Vector3>();
    readonly List<int>     _tF = new List<int>();

    void OnEnable()  => BuildMesh();
    void Start()     => BuildMesh();

    // ── helpers ─────────────────────────────────────────────────────────────
    Vector3 PD(float x, float z) => new Vector3(x, Y, z);
    Vector3 PF(float x, float z) => new Vector3(x * figureScale, Y_FIGURE, z * figureScale);

    static void QuadTo(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(i); t.Add(i+2); t.Add(i+1);
        t.Add(i); t.Add(i+3); t.Add(i+2);
    }

    static void TriTo(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        if (Vector3.Dot(Vector3.Cross(b-a, c-a), Vector3.up) > 0f)
        { t.Add(i); t.Add(i+1); t.Add(i+2); }
        else
        { t.Add(i); t.Add(i+2); t.Add(i+1); }
    }

    static void DiscTo(List<Vector3> v, List<int> t, Vector3 center, float r, int segs, float y, float cx, float cz)
    {
        float step = Mathf.PI * 2f / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i*step, a1 = (i+1)*step;
            int idx = v.Count;
            v.Add(center);
            v.Add(new Vector3(cx + Mathf.Cos(a0)*r, y, cz + Mathf.Sin(a0)*r));
            v.Add(new Vector3(cx + Mathf.Cos(a1)*r, y, cz + Mathf.Sin(a1)*r));
            t.Add(idx); t.Add(idx+2); t.Add(idx+1);
        }
    }

    void BarF(float x0, float z0, float x1, float z1, float w)
    {
        Vector3 dir  = new Vector3((x1-x0)*figureScale, 0, (z1-z0)*figureScale).normalized;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x);
        float hw = w * figureScale * 0.5f;
        QuadTo(_vF, _tF,
            PF(x0,z0) + perp*hw,
            PF(x0,z0) - perp*hw,
            PF(x1,z1) - perp*hw,
            PF(x1,z1) + perp*hw);
    }

    // ── construcción ────────────────────────────────────────────────────────
    void BuildMesh()
    {
        _vD.Clear(); _tD.Clear();
        _vF.Clear(); _tF.Clear();

        // ── ROMBO ────────────────────────────────────────────────────────────
        float hw = diamondWidth  * 0.5f;
        float hh = diamondHeight * 0.5f;

        // Rombo = 4 triángulos desde el centro
        Vector3 top   = PD( 0,  hh);
        Vector3 bot   = PD( 0, -hh);
        Vector3 left  = PD(-hw,  0);
        Vector3 right = PD( hw,  0);
        Vector3 ctr   = PD( 0,   0);

        TriTo(_vD, _tD, ctr, top,  right);
        TriTo(_vD, _tD, ctr, right, bot);
        TriTo(_vD, _tD, ctr, bot,  left);
        TriTo(_vD, _tD, ctr, left,  top);

        // ── FIGURA HUMANA (vista lateral, caminando hacia la derecha) ────────
        // Coordenadas en unidades locales; figureScale las escala.
        // Origin = centro del rombo.
        // La figura está desplazada ligeramente a la izquierda.
        float ox = -0.10f, oz = 0.05f; // offset base de la figura

        float lw = 0.08f; // grosor líneas del cuerpo

        // Cabeza (disco)
        float hx = ox + 0.22f, hz = oz + 0.52f, hr = 0.10f;
        DiscTo(_vF, _tF, PF(hx, hz), hr * figureScale, 20, Y_FIGURE, hx*figureScale, hz*figureScale);

        // Bolso / maletín (pequeño rectángulo a la izquierda)
        float bx = ox - 0.28f, bz = oz + 0.18f;
        QuadTo(_vF, _tF,
            PF(bx - 0.12f, bz - 0.09f),
            PF(bx + 0.04f, bz - 0.09f),
            PF(bx + 0.04f, bz + 0.09f),
            PF(bx - 0.12f, bz + 0.09f));
        // Asa del bolso
        BarF(bx - 0.04f, bz + 0.09f, ox + 0.08f, oz + 0.30f, 0.05f);

        // Torso
        BarF(ox + 0.08f, oz + 0.14f,  ox + 0.18f, oz + 0.42f, lw);

        // Brazo delantero (hacia adelante-abajo)
        BarF(ox + 0.14f, oz + 0.36f,  ox + 0.38f, oz + 0.18f, lw * 0.85f);

        // Brazo trasero (hacia atrás-abajo, hacia el bolso)
        BarF(ox + 0.10f, oz + 0.34f,  ox - 0.14f, oz + 0.22f, lw * 0.85f);

        // Pierna delantera (estirada hacia adelante)
        BarF(ox + 0.08f, oz + 0.14f,  ox + 0.36f, oz - 0.18f, lw);
        // Pie delantero
        BarF(ox + 0.36f, oz - 0.18f,  ox + 0.52f, oz - 0.22f, lw * 0.8f);

        // Pierna trasera (doblada hacia atrás-arriba)
        BarF(ox + 0.08f, oz + 0.14f,  ox - 0.16f, oz - 0.04f, lw);
        BarF(ox - 0.16f, oz - 0.04f,  ox - 0.10f, oz - 0.22f, lw * 0.85f);
        // Pie trasero
        BarF(ox - 0.10f, oz - 0.22f,  ox + 0.04f, oz - 0.26f, lw * 0.75f);

        // Líneas verticales a la izquierda (bordillo / poste)
        float px = -0.55f;
        BarF(px,        oz + 0.40f,  px,        oz - 0.30f, 0.06f);
        BarF(px + 0.16f, oz + 0.28f, px + 0.16f, oz - 0.20f, 0.05f);

        ApplyMesh();
    }

    void ApplyMesh()
    {
        // ── Mesh del rombo ──
        Mesh meshD = new Mesh { name = "SchoolDiamond" };
        meshD.vertices  = _vD.ToArray();
        meshD.triangles = _tD.ToArray();
        meshD.RecalculateNormals();
        meshD.RecalculateBounds();

        // ── Mesh de la figura ──
        Mesh meshF = new Mesh { name = "SchoolFigure" };
        meshF.vertices  = _vF.ToArray();
        meshF.triangles = _tF.ToArray();
        meshF.RecalculateNormals();
        meshF.RecalculateBounds();

        // Combinar en un solo mesh con sub-meshes
        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh      = meshD;
        combine[0].transform = Matrix4x4.identity;
        combine[1].mesh      = meshF;
        combine[1].transform = Matrix4x4.identity;

        Mesh combined = new Mesh { name = "RoadSchoolZone" };
        combined.CombineMeshes(combine, false, false); // false = mantener sub-meshes
        combined.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = combined;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Dos materiales: [0] rombo, [1] figura
        Material matD = MakeMat(diamondColor);
        Material matF = MakeMat(figureColor);
        mr.sharedMaterials = new Material[] { matD, matF };
    }

    static Material MakeMat(Color c)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color");
        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else mat.color = c;
        return mat;
    }

#if UNITY_EDITOR
    void OnValidate() => BuildMesh();
#endif
}
