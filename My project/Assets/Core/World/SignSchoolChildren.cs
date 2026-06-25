using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Señal de zona escolar — rombo amarillo con dos figuras de niños caminando.
/// Decorativa, sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class SignSchoolChildren : MonoBehaviour
{
    [Header("Rombo")]
    public float size = 0.70f;

    [Header("Colores")]
    public Color diamondColor = new Color(0.98f, 0.78f, 0.0f);
    public Color figureColor  = new Color(0.10f, 0.10f, 0.10f);

    const float YD=0f, YF=0.005f;

    readonly List<Vector3> _vD=new List<Vector3>(), _vF=new List<Vector3>();
    readonly List<int>     _tD=new List<int>(),     _tF=new List<int>();

    void OnEnable()=>Build(); void Start()=>Build();

    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){
        int i=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);
        t.Add(i);t.Add(i+2);t.Add(i+1);t.Add(i);t.Add(i+3);t.Add(i+2);
    }
    void Bar(float x0,float z0,float x1,float z1,float w){
        Vector3 dir=new Vector3(x1-x0,0,z1-z0).normalized;
        Vector3 perp=new Vector3(-dir.z,0,dir.x);float hw=w*0.5f;
        Quad(_vF,_tF,
            new Vector3(x0,YF,z0)+perp*hw,new Vector3(x0,YF,z0)-perp*hw,
            new Vector3(x1,YF,z1)-perp*hw,new Vector3(x1,YF,z1)+perp*hw);
    }
    void Disc(float cx,float cz,float r,int segs){
        var center=new Vector3(cx,YF,cz);
        float step=Mathf.PI*2f/segs;
        for(int i=0;i<segs;i++){
            float a0=i*step,a1=(i+1)*step;int idx=_vF.Count;
            _vF.Add(center);
            _vF.Add(new Vector3(cx+Mathf.Cos(a0)*r,YF,cz+Mathf.Sin(a0)*r));
            _vF.Add(new Vector3(cx+Mathf.Cos(a1)*r,YF,cz+Mathf.Sin(a1)*r));
            _tF.Add(idx);_tF.Add(idx+2);_tF.Add(idx+1);
        }
    }
    void Figure(float ox, float oz, float s){
        Disc(ox, oz+0.44f*s, 0.09f*s, 16);          // cabeza
        Bar(ox,oz+0.35f*s,ox,oz+0.10f*s, 0.07f*s);  // torso
        Bar(ox,oz+0.28f*s,ox-0.12f*s,oz+0.14f*s,0.055f*s); // brazo izq
        Bar(ox,oz+0.28f*s,ox+0.12f*s,oz+0.20f*s,0.055f*s); // brazo der
        Bar(ox,oz+0.10f*s,ox+0.14f*s,oz-0.18f*s,0.06f*s);  // pierna der
        Bar(ox,oz+0.10f*s,ox-0.10f*s,oz-0.20f*s,0.06f*s);  // pierna izq
    }

    void Build(){
        _vD.Clear();_tD.Clear();_vF.Clear();_tF.Clear();
        float h=size;
        // Rombo
        var top=new Vector3(0,YD,h);var bot=new Vector3(0,YD,-h);
        var left=new Vector3(-h,YD,0);var right=new Vector3(h,YD,0);
        var ctr=new Vector3(0,YD,0);
        void T(Vector3 a,Vector3 b,Vector3 c){
            int i=_vD.Count;_vD.Add(a);_vD.Add(b);_vD.Add(c);
            _tD.Add(i);_tD.Add(i+1);_tD.Add(i+2);
        }
        T(ctr,top,right);T(ctr,right,bot);T(ctr,bot,left);T(ctr,left,top);

        // Dos figuras: adulto (grande) a la derecha, niño (pequeño) a la izquierda
        Figure( 0.08f,  0f, 0.70f); // adulto
        Figure(-0.20f, -0.04f, 0.52f); // niño delante

        Apply();
    }

    void Apply(){
        var ci=new CombineInstance[2];
        ci[0].mesh=Make(_vD,_tD,"Diamond");ci[0].transform=Matrix4x4.identity;
        ci[1].mesh=Make(_vF,_tF,"Figures"); ci[1].transform=Matrix4x4.identity;
        var m=new Mesh{name="SignSchoolChildren"};
        m.CombineMeshes(ci,false,false);m.RecalculateBounds();
        GetComponent<MeshFilter>().mesh=m;
        var mr=GetComponent<MeshRenderer>();
        mr.sharedMaterials=new[]{MakeMat(diamondColor),MakeMat(figureColor)};
        mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows=false;
    }
    static Mesh Make(List<Vector3> v,List<int> t,string n){
        var m=new Mesh{name=n};m.vertices=v.ToArray();m.triangles=t.ToArray();
        m.RecalculateNormals();m.RecalculateBounds();return m;
    }
    static Material MakeMat(Color c){
        var sh=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Color");
        var mat=new Material(sh);
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",c);else mat.color=c;
        return mat;
    }
#if UNITY_EDITOR
    void OnValidate()=>Build();
#endif
}
