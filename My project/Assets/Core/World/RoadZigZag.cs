using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marca vial de zigzag — línea en W pintada en el suelo.
/// Decorativa, sin colisiones. Típica de zonas de parada de bus / carril bici.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadZigZag : MonoBehaviour
{
    [Header("Forma")]
    [Tooltip("Número de dientes del zigzag")]
    public int   teeth       = 5;
    [Tooltip("Ancho de cada diente (distancia horizontal entre picos)")]
    public float toothWidth  = 0.45f;
    [Tooltip("Altura de cada diente (distancia vertical entre pico y valle)")]
    public float toothHeight = 0.35f;
    [Tooltip("Grosor de la línea")]
    public float lineWidth   = 0.10f;

    [Header("Apariencia")]
    public Color color = Color.white;

    const float Y = 0.02f;

    readonly List<Vector3> _v = new List<Vector3>();
    readonly List<int>     _t = new List<int>();

    void OnEnable() => Build();
    void Start()    => Build();

    static void Quad(List<Vector3> v, List<int> t,
                     Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(i); t.Add(i+2); t.Add(i+1);
        t.Add(i); t.Add(i+3); t.Add(i+2);
    }

    /// Segmento de línea gruesa entre dos puntos
    void Seg(float x0, float z0, float x1, float z1)
    {
        Vector3 dir  = new Vector3(x1-x0, 0, z1-z0).normalized;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x);
        float hw = lineWidth * 0.5f;
        Quad(_v, _t,
            new Vector3(x0,Y,z0) + perp*hw,
            new Vector3(x0,Y,z0) - perp*hw,
            new Vector3(x1,Y,z1) - perp*hw,
            new Vector3(x1,Y,z1) + perp*hw);
    }

    void Build()
    {
        _v.Clear(); _t.Clear();

        // Los puntos del zigzag van de izquierda a derecha en X.
        // Alternamos Z entre 0 (valle) y toothHeight (pico).
        // El total en X = teeth * toothWidth, centrado en origen.

        float totalX = teeth * toothWidth;
        float startX = -totalX * 0.5f;

        // Generamos los vértices del zigzag (puntos centrales de la línea)
        var pts = new List<Vector2>(); // (x, z)
        for (int i = 0; i <= teeth; i++)
        {
            float x = startX + i * toothWidth;
            float z = (i % 2 == 0) ? 0f : toothHeight;
            pts.Add(new Vector2(x, z));
        }

        // Desplazamos verticalmente para centrar en Z
        float midZ = toothHeight * 0.5f;
        for (int i = 0; i < pts.Count; i++)
            pts[i] = new Vector2(pts[i].x, pts[i].y - midZ);

        // Dibujamos segmentos entre puntos consecutivos
        for (int i = 0; i < pts.Count - 1; i++)
            Seg(pts[i].x, pts[i].y, pts[i+1].x, pts[i+1].y);

        // Tapamos las uniones con discos pequeños para evitar huecos
        float jr = lineWidth * 0.55f;
        int segs = 10;
        foreach (var p in pts)
        {
            float cx = p.x, cz = p.y;
            var center = new Vector3(cx, Y, cz);
            float step = Mathf.PI * 2f / segs;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i*step, a1 = (i+1)*step;
                int idx = _v.Count;
                _v.Add(center);
                _v.Add(new Vector3(cx+Mathf.Cos(a0)*jr, Y, cz+Mathf.Sin(a0)*jr));
                _v.Add(new Vector3(cx+Mathf.Cos(a1)*jr, Y, cz+Mathf.Sin(a1)*jr));
                _t.Add(idx); _t.Add(idx+2); _t.Add(idx+1);
            }
        }

        Apply();
    }

    void Apply()
    {
        var mesh = new Mesh { name = "RoadZigZag" };
        mesh.vertices  = _v.ToArray();
        mesh.triangles = _t.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().mesh = mesh;

        var mr = GetComponent<MeshRenderer>();
        var sh  = Shader.Find("Universal Render Pipeline/Unlit")
               ?? Shader.Find("Unlit/Color");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

#if UNITY_EDITOR
    void OnValidate() => Build();
#endif
}
