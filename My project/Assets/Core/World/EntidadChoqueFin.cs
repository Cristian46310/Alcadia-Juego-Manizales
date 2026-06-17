using UnityEngine;

/// <summary>
/// Marca peatones u otras entidades que deben activar el fin de partida al chocar.
/// Añadir al mismo objeto que tenga el Collider (o a un padre).
/// </summary>
public class EntidadChoqueFin : MonoBehaviour
{
    [Tooltip("Si es la meta (torre), carga Mensaje motivacional al chocar.")]
    public bool siempreMensajeMotivacional;
}
