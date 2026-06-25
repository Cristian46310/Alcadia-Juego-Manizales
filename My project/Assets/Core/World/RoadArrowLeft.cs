using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flecha de carretera: recto + giro izquierda (↑←) — decorativa, sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadArrowLeft : MonoBehaviour
{
    [Header("Tallo común (base)")]
    public float stemWidth   = 0.50f;   // ancho total del tallo
    public float stemLength  = 0.80f;   // altura del tallo antes de bifurcarse

    [Header("Brazo recto (↑)")]
    public float straightWidth  = 0.22f;  // ancho del brazo recto
    public float straightLength = 1.10f;  // largo del brazo recto (sin cabeza)
    public float straightHead   = 0.45f;  // largo de la cabeza recta

    [Header("Brazo izquierda (←)")]
    public float leftWidth   = 0.22f;  // ancho del brazo izquierdo
    public float leftLength  = 0.90f;  // largo horizontal hacia la izquierda (sin cabeza)
    public float leftHead    = 0.45f;  // largo de la cabeza izquierda

    [Header("Apariencia")]
    public Color color = Color.white;

    const float Y = 0.02f; // offset sobre el suelo

    void OnEnable()  => BuildMesh();
    void Start()     => BuildMesh();

    void BuildMesh()
    {
        // Coordenadas: Z = adelante (arriba en pantalla), X = derecha
        //
        //   cabeza←   ←←←←←←←←←←←←←
        //                              |  brazo horizontal
        //              ↑↑ cabeza↑      |
        //              |               |  (el brazo horizontal sale
        //              | brazo recto   |   desde la esquina superior-izq
        //              |               |   del tallo)
        //         ─────┴───────────────┘
        //              tallo (base)
        //         ─────────────────────
        //

        var verts = new List<Vector3>();
        var tris  = new List<int>();

        float hw = stemWidth  * 0.5f;       // semi-ancho tallo
        float sw = straightWidth * 0.5f;    // semi-ancho brazo recto
        float lw = leftWidth * 0.5f;        // semi-ancho brazo izquierdo

        float stemTop     = stemLength;
        float straightTop = stemLength + straightLength;
        float straightTip = straightTop + straightHead;

        // El brazo izquierdo sale desde la esquina superior-izquierda del tallo
        // en dirección -X, centrado verticalmente a la altura del tallo superior
        float armCenterZ  = stemTop + lw;           // centra el brazo en Z
        float armLeft     = -hw - leftLength;       // extremo izquierdo del brazo
        float armTip      = armLeft - leftHead;     // punta de la cabeza

        // ── 1. TALLO COMPLETO (cubre toda la base y la zona de bifurcación) ──────
        // Rectángulo de -hw a +hw en X, de 0 a stemTop + lw*2 en Z
        // (lo hacemos un poco más alto para que tape la unión con el brazo izq)
        AddQuad(verts, tris,
            V(-hw, 0),
            V( hw, 0),
            V( hw, armCenterZ + lw),
            V(-hw, armCenterZ + lw));

        // ── 2. BRAZO RECTO ────────────────────────────────────────────────────────
        // Va desde Z=stemTop hasta Z=straightTop, centrado a la derecha del tallo
        // Lo colocamos en el lado derecho: de X=-sw a X=+hw (usa el ancho del tallo)
        AddQuad(verts, tris,
            V(-sw, stemTop),
            V( hw, stemTop),
            V( hw, straightTop),
            V(-sw, straightTop));

        // Cabeza recta (triángulo)
        AddTri(verts, tris,
            V(-straightHead, straightTop),
            V( hw + straightHead * 0.3f, straightTop),
            V((hw - sw) * 0.5f - sw * 0.1f, straightTip));

        // ── 3. BRAZO HORIZONTAL IZQUIERDO ─────────────────────────────────────────
        // Va desde X=-hw hasta X=armLeft, a altura armCenterZ ± lw
        AddQuad(verts, tris,
            V(armLeft, armCenterZ - lw),
            V(-hw,     armCenterZ - lw),
            V(-hw,     armCenterZ + lw),
            V(armLeft, armCenterZ + lw));

        // Cabeza izquierda (triángulo apunta a -X)
        AddTri(verts, tris,
            V(armLeft, armCenterZ + leftHead),
            V(armLeft, armCenterZ - leftHead),
            V(armTip,  armCenterZ));

        ApplyMesh(verts.ToArray(), tris.ToArray());
    }

    Vector3 V(float x, float z) => new Vector3(x, Y, z);

    static void AddQuad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        // dos triángulos, normal hacia +Y
        t.Add(i);   t.Add(i+2); t.Add(i+1);
        t.Add(i);   t.Add(i+3); t.Add(i+2);
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
        Mesh mesh = new Mesh { name = "ArrowStraightLeft" };
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