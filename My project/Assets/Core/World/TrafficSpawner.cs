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
    public float distanciaMinimaEntreSpawns = 15f;
    public float distanciaMinimaAlJugador = 25f;
    public float velocidadMinimaKMH = 30f;
    public float velocidadMaximaKMH = 60f;

    [Header("Limpieza")]
    public float distanciaDesaparicionRespectoJugador = 80f;

    private readonly List<TrafficVehicleAI> vehiculosActivos = new();
    private float temporizadorSpawn;

    private void Update()
    {
        LimpiarReferenciasNulas();
        DestruirVehiculosLejanos();

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
        if (carriles == null || carriles.Length == 0 ||
            prefabsVehiculo == null || prefabsVehiculo.Length == 0)
        {
            return;
        }

        if (vehiculosActivos.Count >= maxVehiculosActivos)
        {
            return;
        }

        LanePath carrilElegido = ObtenerCarrilDisponible();

        if (carrilElegido == null || carrilElegido.PointCount == 0)
        {
            return;
        }

        // Busca un punto válido fuera del rango del jugador en todo el carril
        if (!ObtenerPuntoSpawnAleatorio(carrilElegido, out Vector3 puntoSpawn, out int indiceSpawn))
        {
            return;
        }

        if (!SpawnDisponible(carrilElegido, puntoSpawn))
        {
            return;
        }

        TrafficVehicleAI prefab = ObtenerPrefabDisponible();

        if (prefab == null)
        {
            return;
        }

        TrafficVehicleAI vehiculo = Instantiate(prefab, puntoSpawn, Quaternion.identity);

        // Pasa el índice correcto para que el vehículo no regrese al punto 0
        vehiculo.ConfigurarCarril(carrilElegido, indiceSpawn);
        vehiculo.ConfigurarVelocidadObjetivo(Random.Range(velocidadMinimaKMH, velocidadMaximaKMH));
        vehiculosActivos.Add(vehiculo);
    }

    /// <summary>
    /// Reúne todos los puntos del carril que estén fuera del rango del jugador
    /// y elige uno al azar entre ellos.
    /// Devuelve false si no hay ningún punto válido disponible.
    /// </summary>
    private bool ObtenerPuntoSpawnAleatorio(LanePath carril, out Vector3 puntoElegido, out int indiceElegido)
    {
        // Lista temporal de índices candidatos (evita allocations innecesarias con capacidad inicial)
        List<int> candidatos = new List<int>(carril.PointCount);

        for (int i = 0; i < carril.PointCount; i++)
        {
            Vector3 punto = carril.GetPoint(i);

            // Descarta puntos demasiado cerca del jugador
            if (jugador != null &&
                Vector3.Distance(jugador.position, punto) < distanciaMinimaAlJugador)
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

        // Elige un índice al azar entre los candidatos válidos
        int seleccion = candidatos[Random.Range(0, candidatos.Count)];
        puntoElegido = carril.GetPoint(seleccion);
        indiceElegido = seleccion;
        return true;
    }

    private LanePath ObtenerCarrilDisponible()
    {
        int cantidadCarriles = carriles.Length;
        int indiceInicial = Random.Range(0, cantidadCarriles);

        for (int i = 0; i < cantidadCarriles; i++)
        {
            LanePath carril = carriles[(indiceInicial + i) % cantidadCarriles];

            if (carril != null && carril.PointCount > 0)
            {
                return carril;
            }
        }

        return null;
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

    private bool SpawnDisponible(LanePath carril, Vector3 puntoSpawn)
    {
        for (int i = 0; i < vehiculosActivos.Count; i++)
        {
            TrafficVehicleAI vehiculo = vehiculosActivos[i];

            if (vehiculo == null || vehiculo.Lane != carril)
            {
                continue;
            }

            if (Vector3.Distance(vehiculo.transform.position, puntoSpawn) < distanciaMinimaEntreSpawns)
            {
                return false;
            }
        }

        return true;
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