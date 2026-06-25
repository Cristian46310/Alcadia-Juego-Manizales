using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Señal de No Retorno — círculo rojo con U invertida tachada.
/// Decorativa, sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class SignNoUTurn : MonoBehaviour
{
    [Header("Tamaño")]
    public float radius        = 0.38f;
    public float ringThickness = 0.07f;
    public float lineWidth     = 0.06f;
    public int   segments      = 48;

    [Header("Colores")]
    public Color ringColor     = new Color(0.85f,0.1f,0.1f);
    public Color symbolColor   = new Color(0.15f,0.15f,0.15f);

    const float YR = 0f, YS = 0.005f;

    readonly List<Vector3> _vR=new List<Vector3>();
    readonly List<int>     _tR=new List<int>();
    readonly List<Vector3> _vS=new List<Vector3>();
    readonly List<int>     _tS=new List<int>();

    void OnEnable() => Build();
    void Start()    => Build();

    static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){
        int i=v.Count; v.Add(a);v.Add(b);v.Add(c);v.Add(d);
        t.Add(i);t.Add(i+2);t.Add(i+1); t.Add(i);t.Add(i+3);t.Add(i+2);
    }

    void Ring(List<Vector3> v,List<int> t,float cx,float cz,float ro,float ri,int segs,float y){
        float step=Mathf.PI*2f/segs;
        for(int i=0;i<segs;i++){
            float a0=i*step,a1=(i+1)*step;
            Quad(v,t,
                new Vector3(cx+Mathf.Cos(a0)*ri,y,cz+Mathf.Sin(a0)*ri),
                new Vector3(cx+Mathf.Cos(a0)*ro,y,cz+Mathf.Sin(a0)*ro),
                new Vector3(cx+Mathf.Cos(a1)*ro,y,cz+Mathf.Sin(a1)*ro),
                new Vector3(cx+Mathf.Cos(a1)*ri,y,cz+Mathf.Sin(a1)*ri));
        }
    }

    void Arc(List<Vector3> v,List<int> t,float cx,float cz,float ro,float ri,float fromDeg,float toDeg,int segs,float y){
        float from=fromDeg*Mathf.Deg2Rad, to=toDeg*Mathf.Deg2Rad;
        float step=(to-from)/segs;
        for(int i=0;i<segs;i++){
            float a0=from+i*step,a1=from+(i+1)*step;
            Quad(v,t,
                new Vector3(cx+Mathf.Cos(a0)*ri,y,cz+Mathf.Sin(a0)*ri),
                new Vector3(cx+Mathf.Cos(a0)*ro,y,cz+Mathf.Sin(a0)*ro),
                new Vector3(cx+Mathf.Cos(a1)*ro,y,cz+Mathf.Sin(a1)*ro),
                new Vector3(cx+Mathf.Cos(a1)*ri,y,cz+Mathf.Sin(a1)*ri));
        }
    }

    void Bar(List<Vector3> v,List<int> t,float x0,float z0,float x1,float z1,float w,float y){
        Vector3 dir=new Vector3(x1-x0,0,z1-z0).normalized;
        Vector3 perp=new Vector3(-dir.z,0,dir.x);
        float hw=w*0.5f;
        Quad(v,t,
            new Vector3(x0,y,z0)+perp*hw, new Vector3(x0,y,z0)-perp*hw,
            new Vector3(x1,y,z1)-perp*hw, new Vector3(x1,y,z1)+perp*hw);
    }

    void Build(){
        _vR.Clear();_tR.Clear();_vS.Clear();_tS.Clear();

        float r=radius, ri=r-ringThickness, lw=lineWidth;

        // Fondo blanco (disco)
        Ring(_vR,_tR,0,0,r,0,segments,YR);

        // Anillo rojo exterior
        Ring(_vR,_tR,0,0,r,ri,segments,YR+0.001f);

        // Símbolo U invertida (la U normal tiene el arco arriba)
        // Palo izquierdo (sube)
        float px=-0.10f, pz=-0.18f, pt=0.16f;
        Bar(_vS,_tS, px,pz, px,pt, lw, YS);
        // Palo derecho (baja con flecha)
        float qx=0.10f;
        Bar(_vS,_tS, qx,pt, qx,pz, lw, YS);
        // Arco superior conectando ambos palos
        Arc(_vS,_tS,0,pt, 0.10f+lw*0.5f, 0.10f-lw*0.5f, 0f,180f,16,YS);
        // Flecha abajo en el palo derecho
        float ax=qx, az=pz;
        // cabeza de flecha hacia abajo
        int idx=_vS.Count;
        _vS.Add(new Vector3(ax-lw,YS,az+lw*1.5f));
        _vS.Add(new Vector3(ax+lw,YS,az+lw*1.5f));
        _vS.Add(new Vector3(ax,   YS,az-lw*1.8f));
        if(Vector3.Dot(Vector3.Cross(
            _vS[idx+1]-_vS[idx], _vS[idx+2]-_vS[idx]), Vector3.up)>0f)
        { _tS.Add(idx);_tS.Add(idx+1);_tS.Add(idx+2); }
        else
        { _tS.Add(idx);_tS.Add(idx+2);_tS.Add(idx+1); }

        // Diagonal tachadora
        float d=r*0.80f;
        Bar(_vS,_tS,-d*0.62f,d*0.62f, d*0.62f,-d*0.62f, lw*1.1f, YS+0.001f);

        Apply();
    }

    void Apply(){
        var ci=new CombineInstance[2];
        ci[0].mesh=Make(_vR,_tR,"Ring"); ci[0].transform=Matrix4x4.identity;
        ci[1].mesh=Make(_vS,_tS,"Sym");  ci[1].transform=Matrix4x4.identity;
        Mesh m=new Mesh{name="SignNoUTurn"};
        m.CombineMeshes(ci,false,false); m.RecalculateBounds();
        GetComponent<MeshFilter>().mesh=m;
        var mr=GetComponent<MeshRenderer>();
        mr.sharedMaterials=new[]{MakeMat(ringColor),MakeMat(symbolColor)};
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
