using UnityEngine;

/// <summary>
/// Coloca este script en el objeto que tiene el BoxCollider (Trigger) de detención.
/// El SphereCast del vehículo lo detectará; la IA consulta DebeDetenerse().
/// </summary>
public class TrafficLightObstacle : MonoBehaviour
{
    public TrafficLightController controlador;

    public bool DebeDetenerse()
    {
        return controlador != null && controlador.DebeDetenerVehiculos;
    }
}