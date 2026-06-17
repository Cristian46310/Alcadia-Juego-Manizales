using UnityEngine;

/// <summary>
/// Collider sólido en la torre para que la moto choque de verdad y active ChoqueFinDePartida.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MetaTorreChoque : MonoBehaviour
{
    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = false;

        EntidadChoqueFin entidad = GetComponent<EntidadChoqueFin>();
        if (entidad == null)
        {
            entidad = gameObject.AddComponent<EntidadChoqueFin>();
        }

        entidad.siempreMensajeMotivacional = true;
        gameObject.tag = "Edificio";
    }
}
