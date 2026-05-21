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
        if (carriles == null || carriles.Length == 0 || prefabsVehiculo == null || prefabsVehiculo.Length == 0)
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

        Vector3 puntoSpawn = carrilElegido.GetPoint(0);

        if (jugador != null && Vector3.Distance(jugador.position, puntoSpawn) < distanciaMinimaAlJugador)
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
        vehiculo.ConfigurarCarril(carrilElegido);
        vehiculo.ConfigurarVelocidadObjetivo(Random.Range(velocidadMinimaKMH, velocidadMaximaKMH));
        vehiculosActivos.Add(vehiculo);
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
