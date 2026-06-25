using UnityEngine;

/// <summary>
/// Flecha de carretera recta (↑) — solo decorativa, sin colisiones.
/// Arrastra este script a un GameObject vacío en la escena.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class RoadArrowStraight : MonoBehaviour
{
    [Header("Tamaño de la flecha")]
    public float bodyWidth  = 0.35f;   // ancho del cuerpo
    public float totalLength = 2.0f;   // largo total
    public float headLength = 0.70f;   // largo de la cabeza triangular
    public float headWidth  = 0.75f;   // semi-ancho de la cabeza (de punta a punta)

    [Header("Apariencia")]
    public Color color = Color.white;

    const float SurfaceOffset = 0.02f;

    void OnEnable()
    {
        BuildMesh();
    }

    void Start()
    {
        BuildMesh();
    }

    void BuildMesh()
    {
        // ── vértices ──────────────────────────────────────────
        //
        //          6  (punta)
        //         / \
        //        4   5          ← base de la cabeza
        //        |   |
        //        3   2          ← inicio del cuerpo (arriba)
        //        |   |
        //        0   1          ← cola (abajo)
        //
        // El eje Z apunta "hacia adelante" (la dirección de la flecha).

        float hw  = bodyWidth * 0.5f;           // semi-ancho cuerpo
        float bodyLen = totalLength - headLength;

        Vector3[] v = new Vector3[]
        {
            new Vector3(-hw,  SurfaceOffset, 0),
            new Vector3( hw,  SurfaceOffset, 0),
            new Vector3( hw,  SurfaceOffset, bodyLen),
            new Vector3(-hw,  SurfaceOffset, bodyLen),
            new Vector3(-headWidth, SurfaceOffset, bodyLen),
            new Vector3( headWidth, SurfaceOffset, bodyLen),
            new Vector3( 0,   SurfaceOffset, totalLength),
        };

        int[] t = new int[]
        {
            // Cuerpo (2 triángulos)
            0, 3, 1,
            1, 3, 2,
            // Cabeza (1 triángulo)
            4, 6, 5,
        };

        ApplyMesh(v, t);
    }

    void ApplyMesh(Vector3[] verts, int[] tris)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowStraight";
        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = CreateArrowMaterial(color);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows    = false;

        // Sin ningún Collider — puramente visual
    }

    static Material CreateArrowMaterial(Color arrowColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", arrowColor);
        }
        else
        {
            mat.color = arrowColor;
        }

        return mat;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        BuildMesh();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Vector3 center = transform.position + transform.forward * (totalLength * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(headWidth * 2, 0.05f, totalLength));
    }
#endif
}
