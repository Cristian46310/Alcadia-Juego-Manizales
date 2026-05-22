using UnityEngine;

/// <summary>
/// Al chocar con un coche o persona, guarda el puntaje y carga Taller o Mensaje motivacional.
/// Mensaje motivacional: ≥70 km/h al chocar. Taller: entre 40 y 69 km/h.
/// </summary>
public class ChoqueFinDePartida : MonoBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string escenaTaller = "Taller";
    [SerializeField] private string escenaMensaje = "MensajeMotivacional 1";
    [SerializeField] private string escenaPorDefecto = "Taller";

    [Header("Velocidad al chocar (km/h)")]
    [Tooltip("A esta velocidad o más → MensajeMotivacional.")]
    [SerializeField] private float velocidadMinimaMensajeKMH = 70f;
    [Tooltip("Entre estos valores (inclusive) → Taller.")]
    [SerializeField] private float velocidadMinimaTallerKMH = 40f;
    [SerializeField] private float velocidadMaximaTallerKMH = 69f;

    [Header("Qué cuenta como choque")]
    [Tooltip("Tags adicionales (además de coches con TrafficVehicleAI y EntidadChoqueFin).")]
    [SerializeField] private string[] tagsEntidadChoque = { "Vehiculo", "Peaton" };

    private bool yaProcesado;
    private MotoController moto;
    private Rigidbody cuerpoRigido;

    private void Awake()
    {
        moto = GetComponent<MotoController>();
        cuerpoRigido = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (yaProcesado)
        {
            return;
        }

        if (!EsEntidadChoque(collision.collider))
        {
            return;
        }

        float velocidadImpacto = collision.relativeVelocity.magnitude * 3.6f;
        ProcesarChoque(velocidadImpacto);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (yaProcesado)
        {
            return;
        }

        if (!EsEntidadChoque(other))
        {
            return;
        }

        ProcesarChoque(ObtenerVelocidadActualKMH());
    }

    private bool EsEntidadChoque(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.GetComponentInParent<TrafficVehicleAI>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<EntidadChoqueFin>() != null)
        {
            return true;
        }

        if (tagsEntidadChoque == null)
        {
            return false;
        }

        for (int i = 0; i < tagsEntidadChoque.Length; i++)
        {
            string tag = tagsEntidadChoque[i];
            if (string.IsNullOrEmpty(tag))
            {
                continue;
            }

            if (collider.CompareTag(tag))
            {
                return true;
            }
        }

        return false;
    }

    private void ProcesarChoque(float velocidadImpacto)
    {
        var controladorPuntaje = DistanceScoreController.Instancia;
        if (controladorPuntaje == null)
        {
            controladorPuntaje = FindFirstObjectByType<DistanceScoreController>();
        }

        if (controladorPuntaje == null)
        {
            Debug.LogWarning("[ChoqueFinDePartida] No se encontró DistanceScoreController.");
            return;
        }

        float velocidadKmh = Mathf.Max(velocidadImpacto, ObtenerVelocidadActualKMH());

        yaProcesado = true;
        PuntajePartida.GuardarDesde(controladorPuntaje);

        string escena = ElegirEscena(velocidadKmh);
        Debug.Log(
            $"[ChoqueFinDePartida] Choque — {velocidadKmh:F0} km/h, " +
            $"puntaje={controladorPuntaje.PuntajeActual} → {escena}");

        GestorEscenas.CargarSolo(escena);
    }

    private float ObtenerVelocidadActualKMH()
    {
        if (moto != null)
        {
            return moto.VelocidadActualKMH;
        }

        if (cuerpoRigido == null)
        {
            return 0f;
        }

        Vector3 velocidadPlano = Vector3.ProjectOnPlane(cuerpoRigido.linearVelocity, Vector3.up);
        return velocidadPlano.magnitude * 3.6f;
    }

    private string ElegirEscena(float velocidadKmh)
    {
        if (velocidadKmh >= velocidadMinimaMensajeKMH)
        {
            return escenaMensaje;
        }

        if (velocidadKmh >= velocidadMinimaTallerKMH && velocidadKmh <= velocidadMaximaTallerKMH)
        {
            return escenaTaller;
        }

        return escenaPorDefecto;
    }
}
