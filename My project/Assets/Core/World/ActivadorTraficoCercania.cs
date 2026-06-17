using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Activa coches de tráfico cuando la moto está cerca.
/// Útil para evitar que todos se muevan desde el inicio.
/// </summary>
public class ActivadorTraficoCercania : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform moto;
    [SerializeField] private bool buscarMotoAutomaticamente = true;
    [SerializeField] private LanePath carrilPorDefecto;

    [Header("Carros a controlar")]
    [SerializeField] private TrafficVehicleAI[] carros = new TrafficVehicleAI[0];
    [SerializeField] private bool desactivarCarrosAlInicio = true;

    [Header("Distancias")]
    [SerializeField] private float distanciaActivacion = 60f;
    [SerializeField] private bool desactivarSiSeAleja;
    [SerializeField] private float distanciaDesactivacion = 70f;
    [SerializeField] private bool activarUnaSolaVez = true;

    private readonly HashSet<TrafficVehicleAI> activados = new HashSet<TrafficVehicleAI>();
    private readonly Dictionary<TrafficVehicleAI, Rigidbody> rigidbodies = new Dictionary<TrafficVehicleAI, Rigidbody>();

    private void Awake()
    {
        if (buscarMotoAutomaticamente && moto == null)
        {
            MotoController motoController = FindAnyObjectByType<MotoController>();
            if (motoController != null)
            {
                moto = motoController.transform;
            }
        }

        if (carrilPorDefecto == null)
        {
            carrilPorDefecto = FindAnyObjectByType<LanePath>();
        }

        if (carros == null)
        {
            return;
        }

        for (int i = 0; i < carros.Length; i++)
        {
            TrafficVehicleAI carro = carros[i];
            if (carro == null)
            {
                continue;
            }

            if (carro.TryGetComponent(out Rigidbody rb))
            {
                rigidbodies[carro] = rb;
            }

            if (desactivarCarrosAlInicio)
            {
                PausarCarro(carro);
            }
        }
    }

    private void Update()
    {
        if (moto == null || carros == null || carros.Length == 0)
        {
            return;
        }

        Vector3 motoPlano = Plano(moto.position);

        for (int i = 0; i < carros.Length; i++)
        {
            TrafficVehicleAI carro = carros[i];
            if (carro == null)
            {
                continue;
            }

            float distancia = Vector3.Distance(motoPlano, Plano(carro.transform.position));
            bool estaCerca = distancia <= distanciaActivacion;
            bool estaLejos = distancia >= Mathf.Max(distanciaDesactivacion, distanciaActivacion + 1f);

            if (estaCerca)
            {
                if (activarUnaSolaVez && activados.Contains(carro))
                {
                    continue;
                }

                ActivarCarro(carro);
                activados.Add(carro);
                continue;
            }

            if (!desactivarSiSeAleja || (activarUnaSolaVez && activados.Contains(carro)))
            {
                continue;
            }

            if (estaLejos)
            {
                PausarCarro(carro);
            }
        }
    }

    private void PausarCarro(TrafficVehicleAI carro)
    {
        carro.enabled = false;

        if (rigidbodies.TryGetValue(carro, out Rigidbody rb) && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    private void ActivarCarro(TrafficVehicleAI carro)
    {
        carro.destruirAlFinalDelCarril = false;

        LanePath carril = carro.Lane != null && carro.Lane.PointCount > 0
            ? carro.Lane
            : carrilPorDefecto;

        if (rigidbodies.TryGetValue(carro, out Rigidbody rb) && rb != null)
        {
            rb.isKinematic = false;
        }

        if (carril != null)
        {
            carro.ConfigurarCarril(carril, carril.GetIndiceWaypointMasCercano(carro.transform.position), mantenerPosicionSiCerca: true);
        }

        carro.enabled = true;
    }

    private static Vector3 Plano(Vector3 posicion)
    {
        return Vector3.ProjectOnPlane(posicion, Vector3.up);
    }
}
