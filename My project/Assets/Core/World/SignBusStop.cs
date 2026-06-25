using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Señal de parada de bus — panel azul con símbolo de bus blanco.
/// Decorativa, sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class SignBusStop : MonoBehaviour
{
    [Header("Panel")]
    public float panelWidth  = 0.60f;
    public float panelHeight = 0.70f;
    public float cornerRadius = 0.06f;
    public int   cornerSegs   = 8;

    [Header("Colores")]
    public Color panelColor = new Color(0.10f, 0.35f, 0.75f);
    public Color busColor   = Color.white;

    const float Y_PANEL = 0f;
    const float Y_BUS   = 0.005f;

    readonly List<Vector3> _vP = new List<Vector3>();
    readonly List<int>     _tP = new List<int>();
    readonly List<Vector3> _vB = new List<Vector3>();
    readonly List<int>     _tB = new List<int>();

    void OnEnable() => Build();
    void Start()    => Build();

    Vector3 PP(float x, float z) => new Vector3(x, Y_PANEL, z);
    Vector3 PB(float x, float z) => new Vector3(x, Y_BUS,   z);

    static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c); v.Add(d);
        t.Add(i); t.Add(i+2); t.Add(i+1);
        t.Add(i); t.Add(i+3); t.Add(i+2);
    }

    static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
    {
        int i = v.Count;
        v.Add(a); v.Add(b); v.Add(c);
        t.Add(i); t.Add(i+1); t.Add(i+2);
    }

    void Bar(List<Vector3> v, List<int> t, float x0, float z0, float x1, float z1, float w, float y)
    {
        Vector3 dir  = new Vector3(x1-x0, 0, z1-z0).normalized;
        Vector3 perp = new Vector3(-dir.z, 0, dir.x);
        float hw = w * 0.5f;
        var A = new Vector3(x0,y,z0)+perp*hw;
        var B = new Vector3(x0,y,z0)-perp*hw;
        var C = new Vector3(x1,y,z1)-perp*hw;
        var D = new Vector3(x1,y,z1)+perp*hw;
        Quad(v,t,A,B,C,D);
    }

    void Disc(List<Vector3> v, List<int> t, float cx, float cz, float r, int segs, float y)
    {
        var center = new Vector3(cx, y, cz);
        float step = Mathf.PI*2f/segs;
        for(int i=0;i<segs;i++){
            float a0=i*step, a1=(i+1)*step;
            int idx=v.Count;
            v.Add(center);
            v.Add(new Vector3(cx+Mathf.Cos(a0)*r, y, cz+Mathf.Sin(a0)*r));
            v.Add(new Vector3(cx+Mathf.Cos(a1)*r, y, cz+Mathf.Sin(a1)*r));
            t.Add(idx); t.Add(idx+2); t.Add(idx+1);
        }
    }

    void Build()
    {
        _vP.Clear(); _tP.Clear();
        _vB.Clear(); _tB.Clear();

        float hw = panelWidth*0.5f, hh = panelHeight*0.5f;
        float cr = cornerRadius;

        // Panel rectangular simple
        Quad(_vP,_tP, PP(-hw,-hh), PP(hw,-hh), PP(hw,hh), PP(-hw,hh));

        // Símbolo de bus (centrado, escala ~0.35 del panel)
        float s = 0.12f; // unidad base del bus

        // Carrocería principal
        Quad(_vB,_tB, PB(-2.2f*s,-1.4f*s), PB(2.2f*s,-1.4f*s), PB(2.2f*s,1.0f*s), PB(-2.2f*s,1.0f*s));
        // Techo redondeado (rectángulo + triángulos en esquinas)
        Quad(_vB,_tB, PB(-1.8f*s,1.0f*s), PB(1.8f*s,1.0f*s), PB(1.8f*s,1.6f*s), PB(-1.8f*s,1.6f*s));
        Tri(_vB,_tB,  PB(-2.2f*s,1.0f*s), PB(-1.8f*s,1.0f*s), PB(-1.8f*s,1.6f*s));
        Tri(_vB,_tB,  PB( 2.2f*s,1.0f*s), PB( 1.8f*s,1.6f*s), PB( 1.8f*s,1.0f*s));

        // Ruedas (discos oscuros → aquí los omitimos y hacemos arcos blancos)
        // Ventanas (recorte = usamos el fondo azul, así que dejamos huecos imposibles en mesh plana)
        // Alternativa: ventanas como quads del color del panel superpuestos
        // Las añadimos como sub-mesh del panel
        float wy = Y_BUS+0.001f;
        // Ventana izquierda
        Quad(_vP,_tP,
            new Vector3(-1.8f*s,wy,-0.1f*s), new Vector3(-0.3f*s,wy,-0.1f*s),
            new Vector3(-0.3f*s,wy, 0.8f*s), new Vector3(-1.8f*s,wy, 0.8f*s));
        // Ventana derecha
        Quad(_vP,_tP,
            new Vector3(0.2f*s,wy,-0.1f*s), new Vector3(1.8f*s,wy,-0.1f*s),
            new Vector3(1.8f*s,wy, 0.8f*s), new Vector3(0.2f*s,wy, 0.8f*s));
        // Puerta
        Quad(_vP,_tP,
            new Vector3(-0.15f*s,wy,-1.35f*s), new Vector3(0.6f*s,wy,-1.35f*s),
            new Vector3(0.6f*s,wy,-0.1f*s),   new Vector3(-0.15f*s,wy,-0.1f*s));

        Apply();
    }

    void Apply()
    {
        Mesh mP = MakeMesh(_vP,_tP,"BusPanel");
        Mesh mB = MakeMesh(_vB,_tB,"BusSymbol");

        var ci = new CombineInstance[2];
        ci[0].mesh=mP; ci[0].transform=Matrix4x4.identity;
        ci[1].mesh=mB; ci[1].transform=Matrix4x4.identity;

        Mesh combined = new Mesh{name="SignBusStop"};
        combined.CombineMeshes(ci,false,false);
        combined.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = combined;
        var mr = GetComponent<MeshRenderer>();
        mr.sharedMaterials = new[]{MakeMat(panelColor), MakeMat(busColor)};
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static Mesh MakeMesh(List<Vector3> v, List<int> t, string n)
    {
        var m = new Mesh{name=n};
        m.vertices=v.ToArray(); m.triangles=t.ToArray();
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Material MakeMat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(sh);
        if(mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",c); else mat.color=c;
        return mat;
    }

#if UNITY_EDITOR
    void OnValidate() => Build();
#endif
}
