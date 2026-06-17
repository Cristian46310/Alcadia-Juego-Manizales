#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(ActivadorTraficoTorre))]
public class ActivadorTraficoTorreEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        var activador = (ActivadorTraficoTorre)target;

        EditorGUILayout.HelpBox(
            "Coches: arrancan en el radio exterior (metrosAntesParaIniciarCoches). " +
            "Moto automática: radio interior (metrosCercaParaActivar, punto 650.5 / 1238.05).\n\n" +
            "Coches torre: siguen CarrilFInal/finalpath. Añade puntos como hijos del carril, pulsa " +
            "Sincronizar carril torre y luego Repartir coches.",
            MessageType.Info);

        if (GUILayout.Button("Recrear marcadores (InicoTorre / FIntoree)"))
        {
            activador.RecrearMarcadoresRecta();
        }

        if (GUILayout.Button("Alinear marcadores antes de la cebra"))
        {
            activador.AlinearMarcadoresAntesDeCebraMenu();
        }

        if (GUILayout.Button("Recrear coches del cruce (torre)"))
        {
            ActivadorTraficoTorreSetup.RecrearCochesDelCruce(activador);
        }

        if (GUILayout.Button("Sincronizar carril torre (hijos → waypoints)"))
        {
            activador.SincronizarCarrilTorreEnEditor();
            EditorSceneManager.MarkSceneDirty(activador.gameObject.scene);
        }

        if (GUILayout.Button("Repartir coches en posiciones X/Z"))
        {
            activador.RepartirCochesTorreEnEditor();
            EditorSceneManager.MarkSceneDirty(activador.gameObject.scene);
        }
    }

    [MenuItem("CONTEXT/ActivadorTraficoTorre/Recrear coches del cruce")]
    private static void RecrearCochesContextMenu(MenuCommand command)
    {
        if (command.context is ActivadorTraficoTorre activador)
        {
            ActivadorTraficoTorreSetup.RecrearCochesDelCruce(activador);
        }
    }
}

public static class ActivadorTraficoTorreSetup
{
    private struct DefinicionCoche
    {
        public string nombre;
        public string rutaPrefab;
        public Vector3 posicion;
        public Vector3 rotacionEuler;
    }

    private static readonly DefinicionCoche[] Coches =
    {
        new()
        {
            nombre = "ARCADE - FREE Racing Car (1)",
            rutaPrefab = "Assets/ARCADE - FREE Racing Car/Prefabs (With Colliders)/Free Racing Car.prefab",
            posicion = new Vector3(663.90314f, -0.09884554f, 1262.8563f),
            rotacionEuler = new Vector3(0f, -116.18f, 0f)
        },
        new()
        {
            nombre = "ARCADE - FREE Racing Car (2)",
            rutaPrefab = "Assets/ARCADE - FREE Racing Car/Prefabs (With Colliders)/Free Racing Car.prefab",
            posicion = new Vector3(661.035f, -0.09884554f, 1268.689f),
            rotacionEuler = new Vector3(0f, -116.18f, 0f)
        },
        new()
        {
            nombre = "ARCADE - FREE Racing Car (3)",
            rutaPrefab = "Assets/ARCADE - FREE Racing Car/Prefabs (With Colliders)/Free Racing Car.prefab",
            posicion = new Vector3(654.73f, -0.09884554f, 1265.59f),
            rotacionEuler = new Vector3(0f, -116.18f, 0f)
        },
        new()
        {
            nombre = "ARCADE - FREE Racing Car (4)",
            rutaPrefab = "Assets/ARCADE - FREE Racing Car/Prefabs (With Colliders)/Free Racing Car.prefab",
            posicion = new Vector3(657.93f, -0.09884554f, 1259.92f),
            rotacionEuler = new Vector3(0f, -116.18f, 0f)
        },
        new()
        {
            nombre = "Car02 (1)",
            rutaPrefab = "Assets/car/Car02.fbx",
            posicion = new Vector3(649.26f, 0.15f, 1262.88f),
            rotacionEuler = new Vector3(0f, -117.669f, 0f)
        },
        new()
        {
            nombre = "_Pickup_Concept2015 (1)",
            rutaPrefab = "Assets/car/_Pickup_Concept2015.FBX",
            posicion = new Vector3(652.47f, -0.085168004f, 1256.84f),
            rotacionEuler = new Vector3(0f, -121.968f, 0f)
        }
    };

    public static void RecrearCochesDelCruce(ActivadorTraficoTorre activador)
    {
        if (activador == null)
        {
            return;
        }

        Transform contenedor = activador.transform.Find("CochesCruceTorre");
        if (contenedor == null)
        {
            var contenedorGo = new GameObject("CochesCruceTorre");
            Undo.RegisterCreatedObjectUndo(contenedorGo, "Crear CochesCruceTorre");
            contenedor = contenedorGo.transform;
            contenedor.SetParent(activador.transform, false);
            contenedor.localPosition = Vector3.zero;
        }

        var lista = new System.Collections.Generic.List<TrafficVehicleAI>(Coches.Length);
        LanePath carril = null;
        GameObject carrilGo = GameObject.Find("CarrilFInal");
        if (carrilGo != null)
        {
            carril = carrilGo.GetComponent<LanePath>();
        }

        for (int i = 0; i < Coches.Length; i++)
        {
            DefinicionCoche def = Coches[i];
            TrafficVehicleAI ai = ObtenerOCrearCoche(def, contenedor, carril);
            if (ai != null)
            {
                lista.Add(ai);
            }
        }

        SerializedObject so = new SerializedObject(activador);
        SerializedProperty prop = so.FindProperty("cochesDelCruce");
        prop.arraySize = lista.Count;
        for (int i = 0; i < lista.Count; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = lista[i];
        }

        so.ApplyModifiedProperties();
        activador.RepartirCochesTorreEnEditor();
        EditorUtility.SetDirty(activador);
        EditorSceneManager.MarkSceneDirty(activador.gameObject.scene);

        Debug.Log($"[ActivadorTraficoTorre] {lista.Count} coches del cruce listos bajo CochesCruceTorre.");
    }

    private static TrafficVehicleAI ObtenerOCrearCoche(DefinicionCoche def, Transform contenedor, LanePath carrilFallback)
    {
        Transform existente = contenedor.Find(def.nombre);
        GameObject instancia;

        if (existente != null)
        {
            instancia = existente.gameObject;
        }
        else
        {
            GameObject enEscena = GameObject.Find(def.nombre);
            if (enEscena != null)
            {
                instancia = enEscena;
                Undo.SetTransformParent(instancia.transform, contenedor, "Agrupar coche torre");
            }
            else
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.rutaPrefab);
                if (prefab == null)
                {
                    Debug.LogError($"[ActivadorTraficoTorre] No se encontró prefab: {def.rutaPrefab}");
                    return null;
                }

                instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contenedor);
                instancia.name = def.nombre;
                Undo.RegisterCreatedObjectUndo(instancia, "Crear " + def.nombre);
            }
        }

        if (instancia.GetComponent<Rigidbody>() == null)
        {
            var rb = Undo.AddComponent<Rigidbody>(instancia);
            rb.useGravity = true;
            rb.isKinematic = false;
        }

        TrafficVehicleAI ai = instancia.GetComponent<TrafficVehicleAI>();
        if (ai == null)
        {
            ai = Undo.AddComponent<TrafficVehicleAI>(instancia);
        }

        if (instancia.GetComponent<VehiculoCruceTorre>() == null)
        {
            Undo.AddComponent<VehiculoCruceTorre>(instancia);
        }

        AsegurarCollider(instancia);

        ai.destruirAlFinalDelCarril = true;
        ai.enabled = false;
        ai.velocidadObjetivo = 0f;
        ai.lanePath = carrilFallback;

        EditorUtility.SetDirty(instancia);
        return ai;
    }

    private static void AsegurarCollider(GameObject coche)
    {
        if (coche.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        var box = Undo.AddComponent<BoxCollider>(coche);
        if (coche.TryGetComponent<Renderer>(out Renderer renderer))
        {
            Bounds b = renderer.bounds;
            box.center = coche.transform.InverseTransformPoint(b.center);
            box.size = Vector3.Scale(b.size, new Vector3(
                1f / Mathf.Max(coche.transform.lossyScale.x, 0.001f),
                1f / Mathf.Max(coche.transform.lossyScale.y, 0.001f),
                1f / Mathf.Max(coche.transform.lossyScale.z, 0.001f)));
        }
        else
        {
            box.center = new Vector3(0f, 0.6f, 0f);
            box.size = new Vector3(2f, 1.2f, 4.5f);
        }
    }
}
#endif
