using UnityEngine;

public class SidewalkPedestrianSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject pedestrianPrefab;

    [Header("Rutas")]
    public LanePath ruta;
    [Tooltip("Lista opcional: si tiene elementos validos, el spawner repartira peatones entre estas rutas")]
    public LanePath[] rutas;

    [Header("Cantidad")]
    public int cantidad = 8;

    [Header("Distribución")]
    public int waypointInicial = 0;
    public float separacionEntrePeatones = 1.5f;
    public float alturaSobreSuelo = 0f;
    public Transform parent;

    [Header("Movimiento")]
    public bool loop = false;
    public bool irYVolver = true;
    public float speed = 1.4f;
    public float rotateSpeed = 8f;
    public float stopDistance = 0.15f;

    private void Start()
    {
        if (pedestrianPrefab == null || cantidad <= 0)
        {
            Debug.LogWarning("[SidewalkPedestrianSpawner] pedestrianPrefab es null o cantidad <= 0");
            return;
        }

        LanePath[] rutasValidas = ObtenerRutasValidas();
        if (rutasValidas.Length == 0)
        {
            Debug.LogError("[SidewalkPedestrianSpawner] No hay rutas válidas. Verifica que 'ruta' o 'rutas[]' tengan waypoints configurados.");
            return;
        }
        
        Debug.Log($"[SidewalkPedestrianSpawner] Spawn iniciado: {cantidad} peatones en {rutasValidas.Length} rutas");

        if (parent == null)
        {
            parent = transform;
        }

        int[] cantidadPorRuta = new int[rutasValidas.Length];

        for (int i = 0; i < cantidad; i++)
        {
            int indiceRuta = i % rutasValidas.Length;
            LanePath rutaElegida = rutasValidas[indiceRuta];
            int indiceInicio = waypointInicial;

            Vector3 spawnPosition;
            Quaternion spawnRotation;
            int indiceReal;

            float distanciaEnRuta = cantidadPorRuta[indiceRuta] * separacionEntrePeatones;
            cantidadPorRuta[indiceRuta]++;

            bool ok = rutaElegida.TrySampleHaciaAdelante(
                indiceInicio,
                distanciaEnRuta,
                out spawnPosition,
                out spawnRotation,
                out indiceReal
            );

            if (!ok)
            {
                spawnPosition = rutaElegida.GetPoint(indiceInicio);
                spawnRotation = rutaElegida.GetRotacionEnPosicion(spawnPosition);
            }

            spawnPosition.y += alturaSobreSuelo;

            GameObject instancia = Instantiate(pedestrianPrefab, spawnPosition, spawnRotation, parent);

            if (!instancia.CompareTag("Peaton"))
            {
                instancia.tag = "Peaton";
            }

            SidewalkPedestrianWalker walker = instancia.GetComponent<SidewalkPedestrianWalker>();
            if (walker == null)
            {
                walker = instancia.AddComponent<SidewalkPedestrianWalker>();
            }

            walker.ruta = rutaElegida;
            walker.waypointInicial = indiceReal;
            walker.loop = loop;
            walker.irYVolver = irYVolver;
            walker.speed = speed;
            walker.rotateSpeed = rotateSpeed;
            walker.stopDistance = stopDistance;
            walker.alturaSobreSuelo = alturaSobreSuelo;
        }
        
        Debug.Log($"[SidewalkPedestrianSpawner] ✓ {cantidad} peatones creados exitosamente.");
    }

    private LanePath[] ObtenerRutasValidas()
    {
        int extra = ruta != null && ruta.PointCount > 0 ? 1 : 0;
        int capacidad = (rutas != null ? rutas.Length : 0) + extra;
        if (capacidad == 0)
        {
            Debug.LogError("[SidewalkPedestrianSpawner] 'ruta' y 'rutas[]' están vacías.");
            return new LanePath[0];
        }

        LanePath[] temporales = new LanePath[capacidad];
        int count = 0;

        if (rutas != null)
        {
            for (int i = 0; i < rutas.Length; i++)
            {
                LanePath candidata = rutas[i];
                if (candidata == null)
                {
                    Debug.LogWarning($"[SidewalkPedestrianSpawner] rutas[{i}] es null.");
                    continue;
                }

                if (candidata.PointCount == 0)
                {
                    candidata.SincronizarPuntosDesdeHijos();
                    Debug.Log($"[SidewalkPedestrianSpawner] rutas[{i}] sincronizada. PointCount: {candidata.PointCount}");
                }

                if (candidata.PointCount == 0)
                {
                    Debug.LogWarning($"[SidewalkPedestrianSpawner] rutas[{i}] ({candidata.name}) no tiene waypoints después de sincronizar.");
                    continue;
                }

                bool repetida = false;
                for (int j = 0; j < count; j++)
                {
                    if (temporales[j] == candidata)
                    {
                        repetida = true;
                        break;
                    }
                }

                if (!repetida)
                {
                    temporales[count++] = candidata;
                    Debug.Log($"[SidewalkPedestrianSpawner] Ruta válida agregada: {candidata.name} ({candidata.PointCount} waypoints)");
                }
            }
        }

        if (ruta != null && ruta.PointCount == 0)
        {
            ruta.SincronizarPuntosDesdeHijos();
            Debug.Log($"[SidewalkPedestrianSpawner] ruta principal sincronizada. PointCount: {ruta.PointCount}");
        }

        if (count == 0 && ruta != null && ruta.PointCount > 0)
        {
            temporales[count++] = ruta;
            Debug.Log($"[SidewalkPedestrianSpawner] Ruta principal agregada: {ruta.name} ({ruta.PointCount} waypoints)");
        }

        LanePath[] resultado = new LanePath[count];
        for (int i = 0; i < count; i++)
        {
            resultado[i] = temporales[i];
        }

        return resultado;
    }
}