using UnityEngine;

/// <summary>
/// Marca coches del cruce de la torre. Al chocar con ellos termina la partida (meta).
/// </summary>
public class VehiculoCruceTorre : MonoBehaviour
{
    [Tooltip("No frena por la moto del jugador (evita que parezca que vienen a chocarte).")]
    public bool ignorarMotoEnSensor = true;

}
