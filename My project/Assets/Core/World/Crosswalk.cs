using UnityEngine;

/// <summary>
/// Componente que marca un paso peatonal (zebra) entre dos puntos.
/// Se puede enlazar a un TrafficLightController para coordinar el cruce.
/// </summary>
public class Crosswalk : MonoBehaviour
{
    [Tooltip("Punto de inicio del peatón (donde aparecen)")]
    public Transform startPoint;

    [Tooltip("Punto destino del peatón (lado opuesto del cruce)")]
    public Transform endPoint;

    [Tooltip("Opcional: si se asigna, el cruce solo permitirá peatones cuando el semáforo esté en verde")]
    public TrafficLightController controlador;

    [Tooltip("Si está activo, los peatones podrán cruzar cuando el semáforo esté en rojo en lugar de verde")]
    public bool cruzaEnRojo = false;

    public bool CanPedestriansCross()
    {
        if (controlador == null) return true;
        if (cruzaEnRojo)
        {
            return controlador.EstadoActual == TrafficLightController.TrafficLightState.Red;
        }
        return controlador.EstadoActual == TrafficLightController.TrafficLightState.Green;
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(startPoint.position, 0.1f);
        Gizmos.DrawSphere(endPoint.position, 0.1f);
        Gizmos.DrawLine(startPoint.position, endPoint.position);
    }
}
