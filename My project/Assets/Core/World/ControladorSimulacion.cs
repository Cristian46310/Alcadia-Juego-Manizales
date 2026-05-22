using UnityEngine;

/// <summary>
/// Reservado para lógica global de simulación. El choque forzado cerca de la Torre
/// lo gestiona <see cref="EventoChoqueForzadoTorre"/>.
/// </summary>
public class ControladorSimulacion : MonoBehaviour
{
    [SerializeField] private EventoChoqueForzadoTorre eventoTorre;

    private void Awake()
    {
        if (eventoTorre == null)
        {
            eventoTorre = FindFirstObjectByType<EventoChoqueForzadoTorre>();
        }
    }
}