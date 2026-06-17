using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Referencias")]
    public LanePath[] carriles;
    public TrafficVehicleAI[] prefabsVehiculo;
    public Transform jugador;

    [Header("Spawn")]
    public float intervaloSpawn = 2.5f;
    public int maxVehiculosActivos = 12;
    [Tooltip("Distancia mínima (XZ) a otro coche para poder spawnear.")]
    public float distanciaMinimaEntreSpawns = 22f;
    [Tooltip("Intentos al buscar un waypoint libre en el carril.")]
    public int intentosMaximosPorSpawn = 16;
    [Tooltip("No spawnea si el jugador está a esta distancia (XZ) del punto de spawn o del carril.")]
    public float distanciaMinimaAlJugador = 30f;
    public float velocidadMinimaKMH = 30f;
    public float velocidadMaximaKMH = 60f;

    [Header("Limpieza")]
    public float distanciaDesaparicionRespectoJugador = 80f;

    [Header("Zona torre / meta (sin spawn dinámico)")]
    [Tooltip("No genera coches cerca de la meta ni si el jugador ya llegó.")]
    public Transform centroZonaExcluida;
    public float radioZonaExcluida = 95f;

    [HideInInspector]
    public bool SpawnPausado;

    private readonly List<TrafficVehicleAI> vehiculosActivos = new();
    private float temporizadorSpawn;

    private void Awake()
    {
        ResolverJugador();
    }

    private void Update()
    {
        LimpiarReferenciasNulas();
        DestruirVehiculosLejanos();

        if (SpawnPausado)
        {
            return;
        }

        temporizadorSpawn += Time.deltaTime;

        if (temporizadorSpawn < intervaloSpawn)
        {
            return;
        }

        temporizadorSpawn = 0f;
        IntentarSpawn();
    }

    private void IntentarSpawn()
    {
        ResolverJugador();

        if (carriles == null || carriles.Length == 0 ||
            prefabsVehiculo == null || prefabsVehiculo.Length == 0)
        {
            return;
        }

        if (vehiculosActivos.Count >= maxVehiculosActivos)
        {
            return;
        }

        if (jugador != null && EstaEnZonaExcluida(jugador.position))
        {
            return;
        }

        int inicio = Random.Range(0, carriles.Length);
        for (int i = 0; i < carriles.Length; i++)
        {
            LanePath carril = carriles[(inicio + i) % carriles.Length];
            if (IntentarSpawnEnCarril(carril))
            {
                return;
            }
        }
    }

    private bool IntentarSpawnEnCarril(LanePath carrilElegido)
    {
        if (carrilElegido == null || carrilElegido.PointCount == 0)
        {
            return false;
        }

        if (!ObtenerPuntoSpawnAleatorio(carrilElegido, out Vector3 puntoSpawn, out int indiceSpawn))
        {
            return false;
        }

        if (EstaEnZonaExcluida(puntoSpawn) ||
            JugadorCercaDelPuntoSpawn(puntoSpawn) ||
            !PosicionLibreParaSpawn(puntoSpawn))
        {
            return false;
        }

        TrafficVehicleAI prefab = ObtenerPrefabDisponible();
        if (prefab == null)
        {
            return false;
        }

        TrafficVehicleAI vehiculo = Instantiate(prefab, puntoSpawn, Quaternion.identity);
        vehiculo.ConfigurarCarril(carrilElegido, indiceSpawn);
        vehiculo.ConfigurarVelocidadObjetivo(Random.Range(velocidadMinimaKMH, velocidadMaximaKMH));
        vehiculosActivos.Add(vehiculo);
        return true;
    }

    /// <summary>
    /// Reúne todos los puntos del carril que estén fuera del rango del jugador
    /// y elige uno al azar entre ellos.
    /// Devuelve false si no hay ningún punto válido disponible.
    /// </summary>
    private bool ObtenerPuntoSpawnAleatorio(LanePath carril, out Vector3 puntoElegido, out int indiceElegido)
    {
        List<int> candidatos = new List<int>(carril.PointCount);

        for (int i = 0; i < carril.PointCount; i++)
        {
            Vector3 punto = carril.GetPoint(i);
            if (JugadorCercaDelPuntoSpawn(punto))
            {
                continue;
            }

            if (!PosicionLibreParaSpawn(punto))
            {
                continue;
            }

            candidatos.Add(i);
        }

        if (candidatos.Count == 0)
        {
            puntoElegido = Vector3.zero;
            indiceElegido = 0;
            return false;
        }

        int intentos = Mathf.Min(intentosMaximosPorSpawn, candidatos.Count);
        for (int intento = 0; intento < intentos; intento++)
        {
            int seleccion = candidatos[Random.Range(0, candidatos.Count)];
            Vector3 punto = carril.GetPoint(seleccion);

            if (!PosicionLibreParaSpawn(punto))
            {
                continue;
            }

            puntoElegido = punto;
            indiceElegido = seleccion;
            return true;
        }

        puntoElegido = Vector3.zero;
        indiceElegido = 0;
        return false;
    }

    private TrafficVehicleAI ObtenerPrefabDisponible()
    {
        int cantidadPrefabs = prefabsVehiculo.Length;
        int indiceInicial = Random.Range(0, cantidadPrefabs);

        for (int i = 0; i < cantidadPrefabs; i++)
        {
            TrafficVehicleAI prefab = prefabsVehiculo[(indiceInicial + i) % cantidadPrefabs];

            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private bool PosicionLibreParaSpawn(Vector3 puntoSpawn)
    {
        if (distanciaMinimaEntreSpawns <= 0f)
        {
            return true;
        }

        Vector3 spawnPlano = Plano(puntoSpawn);
        float radio = distanciaMinimaEntreSpawns;

        for (int i = 0; i < vehiculosActivos.Count; i++)
        {
            if (EstaDemasiadoCerca(vehiculosActivos[i], spawnPlano, radio))
            {
                return false;
            }
        }

        TrafficVehicleAI[] todos = FindObjectsByType<TrafficVehicleAI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < todos.Length; i++)
        {
            if (EstaDemasiadoCerca(todos[i], spawnPlano, radio))
            {
                return false;
            }
        }

        Vector3 centroFisico = puntoSpawn + Vector3.up * 1f;
        Collider[] solapamientos = Physics.OverlapSphere(
            centroFisico,
            radio * 0.45f,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < solapamientos.Length; i++)
        {
            Collider col = solapamientos[i];
            if (col == null)
            {
                continue;
            }

            TrafficVehicleAI otro = col.GetComponentInParent<TrafficVehicleAI>();
            if (otro == null || EsCocheDeLaTorre(otro))
            {
                continue;
            }

            if (EstaDemasiadoCerca(otro, spawnPlano, radio))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EsCocheDeLaTorre(TrafficVehicleAI vehiculo)
    {
        return vehiculo != null && vehiculo.GetComponent<VehiculoCruceTorre>() != null;
    }

    private static bool EstaDemasiadoCerca(TrafficVehicleAI vehiculo, Vector3 spawnPlano, float radio)
    {
        if (vehiculo == null || !vehiculo.gameObject.activeInHierarchy || EsCocheDeLaTorre(vehiculo))
        {
            return false;
        }

        return Vector3.Distance(Plano(vehiculo.transform.position), spawnPlano) < radio;
    }

    private bool EstaEnZonaExcluida(Vector3 posicion)
    {
        if (centroZonaExcluida == null || radioZonaExcluida <= 0f)
        {
            return false;
        }

        return Vector3.Distance(Plano(posicion), Plano(centroZonaExcluida.position)) <= radioZonaExcluida;
    }

    private static Vector3 Plano(Vector3 v) => Vector3.ProjectOnPlane(v, Vector3.up);

    private void ResolverJugador()
    {
        if (jugador != null)
        {
            return;
        }

        MotoController moto = FindAnyObjectByType<MotoController>();
        if (moto != null)
        {
            jugador = moto.transform;
        }
    }

    private bool JugadorCercaDelPuntoSpawn(Vector3 puntoSpawn)
    {
        if (jugador == null || distanciaMinimaAlJugador <= 0f)
        {
            return false;
        }

        return Vector3.Distance(Plano(jugador.position), Plano(puntoSpawn)) < distanciaMinimaAlJugador;
    }

    private void DestruirVehiculosLejanos()
    {
        if (jugador == null)
        {
            return;
        }

        for (int i = vehiculosActivos.Count - 1; i >= 0; i--)
        {
            TrafficVehicleAI vehiculo = vehiculosActivos[i];

            if (vehiculo == null)
            {
                vehiculosActivos.RemoveAt(i);
                continue;
            }

            Vector3 jugadorPlano = Vector3.ProjectOnPlane(jugador.position, Vector3.up);
            Vector3 vehiculoPlano = Vector3.ProjectOnPlane(vehiculo.transform.position, Vector3.up);

            if (Vector3.Distance(jugadorPlano, vehiculoPlano) <= distanciaDesaparicionRespectoJugador)
            {
                continue;
            }

            Destroy(vehiculo.gameObject);
            vehiculosActivos.RemoveAt(i);
        }
    }

    /// <summary>
    /// Vehículo estático en la vía para forzar el choque del evento final.
    /// </summary>
    public GameObject SpawnObstaculoBloqueo(Vector3 posicion, Quaternion rotacion)
    {
        TrafficVehicleAI prefab = ObtenerPrefabDisponible();
        if (prefab == null)
        {
            return null;
        }

        TrafficVehicleAI vehiculo = Instantiate(prefab, posicion, rotacion);
        vehiculo.enabled = false;

        Rigidbody rb = vehiculo.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (vehiculo.GetComponent<EntidadChoqueFin>() == null)
        {
            vehiculo.gameObject.AddComponent<EntidadChoqueFin>();
        }

        vehiculosActivos.Add(vehiculo);
        return vehiculo.gameObject;
    }

    private void LimpiarReferenciasNulas()
    {
        for (int i = vehiculosActivos.Count - 1; i >= 0; i--)
        {
            if (vehiculosActivos[i] == null)
            {
                vehiculosActivos.RemoveAt(i);
            }
        }
    }
}