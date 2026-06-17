using UnityEngine;

/// <summary>
/// Al chocar con coche, persona, edificio o barrera, guarda el puntaje y carga Taller o Mensaje motivacional.
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
    [Tooltip("Tags adicionales (coches, peatones, Barrera, Edificio, EntidadChoqueFin).")]
    [SerializeField] private string[] tagsEntidadChoque = { "Vehiculo", "Peaton", "Barrera", "Edificio" };

    private bool yaProcesado;
    private MotoController moto;
    private Rigidbody cuerpoRigido;

    public bool YaProcesado => yaProcesado;

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
        ProcesarChoque(velocidadImpacto, collision.collider);
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

        ProcesarChoque(ObtenerVelocidadActualKMH(), other);
    }

    private bool EsEntidadChoque(Collider collider)
    {
        if (collider == null || collider.isTrigger)
        {
            return false;
        }

        if (EsSuelo(collider))
        {
            return false;
        }

        if (collider.GetComponentInParent<VehiculoCruceTorre>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<TrafficVehicleAI>() != null)
        {
            return true;
        }

        if (collider.GetComponentInParent<PedestrianAI>() != null)
        {
            return true;
        }

        EntidadChoqueFin entidad = collider.GetComponentInParent<EntidadChoqueFin>() ??
                                   collider.GetComponent<EntidadChoqueFin>();
        if (entidad != null)
        {
            return true;
        }

        if (NombreEsBarrera(collider.gameObject.name))
        {
            return true;
        }

        if (NombreEsDivisor(collider.gameObject.name))
        {
            return true;
        }

        if (NombreEsEdificio(collider.gameObject.name))
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

    private static bool EsSuelo(Collider collider)
    {
        string nombre = collider.gameObject.name;
        return nombre.StartsWith("Plane") ||
               nombre.Equals("Terrain") ||
               collider.CompareTag("Suelo");
    }

    private static bool NombreEsBarrera(string nombre)
    {
        return !string.IsNullOrEmpty(nombre) &&
               nombre.IndexOf("barrera", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool NombreEsDivisor(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
        {
            return false;
        }

        if (nombre.StartsWith("Cube"))
        {
            return true;
        }

        return nombre.IndexOf("divisor", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool NombreEsEdificio(string nombre)
    {
        return !string.IsNullOrEmpty(nombre) &&
               nombre.IndexOf("Building", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool EsChoqueCruceTorre(Collider collider)
    {
        return collider != null && collider.GetComponentInParent<VehiculoCruceTorre>() != null;
    }

    private void ProcesarChoque(float velocidadImpacto, Collider colliderChoque)
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
        bool choqueCruceTorre = EsChoqueCruceTorre(colliderChoque);

        yaProcesado = true;
        PuntajePartida.GuardarDesde(controladorPuntaje);

        if (moto != null)
        {
            moto.DesactivarConduccionForzada();
        }

        string escena = choqueCruceTorre ? escenaMensaje : ElegirEscena(velocidadKmh);
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

    /// <summary>
    /// Meta del recorrido (<see cref="ZonaFinRecorrido"/>): guarda puntaje y abre Mensaje motivacional.
    /// </summary>
    public void FinalizarRecorridoMensajeMotivacional()
    {
        if (yaProcesado)
        {
            return;
        }

        var controladorPuntaje = DistanceScoreController.Instancia;
        if (controladorPuntaje == null)
        {
            controladorPuntaje = FindAnyObjectByType<DistanceScoreController>();
        }

        if (controladorPuntaje == null)
        {
            Debug.LogWarning("[ChoqueFinDePartida] Meta sin DistanceScoreController.");
            return;
        }

        yaProcesado = true;
        PuntajePartida.GuardarDesde(controladorPuntaje);

        if (moto != null)
        {
            moto.DesactivarConduccionForzada();
        }

        float velocidadKmh = Mathf.Max(velocidadMinimaMensajeKMH, ObtenerVelocidadActualKMH());
        Debug.Log(
            $"[ChoqueFinDePartida] Meta del recorrido — {velocidadKmh:F0} km/h → {escenaMensaje}");

        GestorEscenas.CargarSolo(escenaMensaje);
    }
}
