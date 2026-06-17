using UnityEngine;

/// <summary>
/// Coloca un trigger al final del recorrido. Si la moto entra tras recorrer la distancia mínima,
/// termina la partida y carga Mensaje motivacional (meta del juego).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZonaFinRecorrido : MonoBehaviour
{
    [SerializeField] private float metrosMinimosRecorridos = 300f;
    [SerializeField] private ChoqueFinDePartida choqueFin;
    [SerializeField] private DistanceScoreController puntaje;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (choqueFin == null)
        {
            choqueFin = FindAnyObjectByType<ChoqueFinDePartida>();
        }

        if (puntaje == null)
        {
            puntaje = FindAnyObjectByType<DistanceScoreController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (choqueFin == null || choqueFin.YaProcesado)
        {
            return;
        }

        if (other.GetComponentInParent<MotoController>() == null)
        {
            return;
        }

        float metros = puntaje != null
            ? puntaje.MetrosRecorridos
            : DistanceScoreController.Instancia != null
                ? DistanceScoreController.Instancia.MetrosRecorridos
                : 0f;

        if (metros < metrosMinimosRecorridos)
        {
            return;
        }

        choqueFin.FinalizarRecorridoMensajeMotivacional();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.35f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider caja)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(caja.center, caja.size);
        }
    }
}
