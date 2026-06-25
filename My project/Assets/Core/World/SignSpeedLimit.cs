using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Señal de límite de velocidad — círculo rojo con número editable en el centro.
/// Sin colisiones.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class SignSpeedLimit : MonoBehaviour
{
    [Header("Número de velocidad")]
    public int speedLimit = 30;

    [Header("Tamaño")]
    public float radius        = 0.40f;
    public float ringThickness = 0.07f;
    public int   segments      = 48;

    [Header("Colores")]
    public Color ringColor   = new Color(0.85f, 0.10f, 0.10f);
    public Color centerColor = Color.white;
    public Color textColor   = new Color(0.10f, 0.10f, 0.10f);

    [Header("Texto")]
    public float textSize = 0.22f;

    const float YD = 0f;
    const float YT = 0.006f;

    readonly List<Vector3> _vD = new List<Vector3>();
    readonly List<int>     _tD = new List<int>();
    readonly List<Vector3> _vR = new List<Vector3>();
    readonly List<int>     _tR = new List<int>();

    TextMesh _tm;

    void OnEnable() => Build();
    void Start()    => Build();

    void Build()
    {
        _vD.Clear(); _tD.Clear();
        _vR.Clear(); _tR.Clear();

        Disc(_vD, _tD, 0, 0, radius - ringThickness, segments, YD);
        Ring(_vR, _tR, 0, 0, radius, radius - ringThickness, segments, YD + 0.001f);

        ApplyCircle();
        SetupText();
    }

    void SetupText()
    {
        Transform child = transform.Find("SpeedText");
        if (child == null)
        {
            var go = new GameObject("SpeedText");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        // La señal está en el plano XZ (padre rotado X=90 en escena).
        // El TextMesh escribe en el plano XY local.
        // Para que el texto quede PARADO y mirando hacia arriba en el plano XZ
        // necesitamos rotarlo -90° en X localmente, así cancela la rotación del padre.
        child.localPosition = new Vector3(0f, YT, 0f);
        child.localRotation = Quaternion.Euler(-90f, 180f, 0f);
        child.localScale    = Vector3.one;

        _tm = child.GetComponent<TextMesh>();
        if (_tm == null) _tm = child.gameObject.AddComponent<TextMesh>();

        _tm.text          = speedLimit.ToString();
        _tm.fontSize      = 200;
        _tm.characterSize = textSize * 0.01f;
        _tm.anchor        = TextAnchor.MiddleCenter;
        _tm.alignment     = TextAlignment.Center;
        _tm.color         = textColor;
        _tm.fontStyle     = FontStyle.Bold;

        var mr = child.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }
    }

    void ApplyCircle()
    {
        var ci = new CombineInstance[2];
        ci[0].mesh = Make(_vD, _tD, "Center"); ci[0].transform = Matrix4x4.identity;
        ci[1].mesh = Make(_vR, _tR, "Ring");   ci[1].transform = Matrix4x4.identity;

        var m = new Mesh { name = "SignSpeedLimit" };
        m.CombineMeshes(ci, false, false);
        m.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = m;
        var mr = GetComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { MakeMat(centerColor), MakeMat(ringColor) };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    static void Disc(List<Vector3> v, List<int> t, float cx, float cz, float r, int segs, float y)
    {
        var center = new Vector3(cx, y, cz);
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

    static void Ring(List<Vector3> v, List<int> t, float cx, float cz,
                     float ro, float ri, int segs, float y)
    {
        float step = Mathf.PI * 2f / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i*step, a1 = (i+1)*step;
            int idx = v.Count;
            v.Add(new Vector3(cx + Mathf.Cos(a0)*ri, y, cz + Mathf.Sin(a0)*ri));
            v.Add(new Vector3(cx + Mathf.Cos(a0)*ro, y, cz + Mathf.Sin(a0)*ro));
            v.Add(new Vector3(cx + Mathf.Cos(a1)*ro, y, cz + Mathf.Sin(a1)*ro));
            v.Add(new Vector3(cx + Mathf.Cos(a1)*ri, y, cz + Mathf.Sin(a1)*ri));
            t.Add(idx); t.Add(idx+2); t.Add(idx+1);
            t.Add(idx); t.Add(idx+3); t.Add(idx+2);
        }
    }

    static Mesh Make(List<Vector3> v, List<int> t, string n)
    {
        var m = new Mesh { name = n };
        m.vertices = v.ToArray(); m.triangles = t.ToArray();
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Material MakeMat(Color c)
    {
        var sh  = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else mat.color = c;
        return mat;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Build();
        if (_tm != null) _tm.text = speedLimit.ToString();
    }
#endif
}