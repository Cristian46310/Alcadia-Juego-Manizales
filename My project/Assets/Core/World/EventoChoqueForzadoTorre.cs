using UnityEngine;

/// <summary>
/// Cerca de la Torre del Cable (semáforos): acelera la moto sola, genera un bloqueo y provoca
/// el choque para mostrar MensajeMotivacional.
/// </summary>
public class EventoChoqueForzadoTorre : MonoBehaviour
{
    [Header("Referencia Torre del Cable")]
    [SerializeField] private Transform referenciaTorre;
    [SerializeField] private string nombreTorreEnEscena = "TorreHerveo_Manizales";

    [Header("Cuándo activar")]
    [Tooltip("Metros recorridos mínimos antes de poder disparar el evento.")]
    [SerializeField] private float metrosMinimosRecorridos = 650f;
    [Tooltip("Distancia a la torre (m) para activar un poco antes de llegar.")]
    [SerializeField] private float metrosAntesDeLaTorre = 110f;

    [Header("Secuencia de choque")]
    [SerializeField] private float velocidadObjetivoKMH = 75f;
    [SerializeField] private float direccionGiroForzada = 0f;
    [SerializeField] private float distanciaObstaculoAdelante = 22f;
    [SerializeField] private float tiempoMaximoHastaChoque = 8f;

    [Header("Referencias de juego")]
    [SerializeField] private MotoController moto;
    [SerializeField] private ChoqueFinDePartida choqueFin;
    [SerializeField] private TrafficSpawner spawnerTrafico;
    [SerializeField] private DistanceScoreController puntaje;

    private bool secuenciaIniciada;
    private float tiempoSecuencia;
    private GameObject obstaculoGenerado;

    private void Awake()
    {
        if (referenciaTorre == null && !string.IsNullOrEmpty(nombreTorreEnEscena))
        {
            var torreGo = GameObject.Find(nombreTorreEnEscena);
            if (torreGo != null)
            {
                referenciaTorre = torreGo.transform;
            }
        }

        if (moto == null)
        {
            moto = FindFirstObjectByType<MotoController>();
        }

        if (choqueFin == null && moto != null)
        {
            choqueFin = moto.GetComponent<ChoqueFinDePartida>();
        }

        if (spawnerTrafico == null)
        {
            spawnerTrafico = FindFirstObjectByType<TrafficSpawner>();
        }

        if (puntaje == null)
        {
            puntaje = FindFirstObjectByType<DistanceScoreController>();
        }
    }

    private void Update()
    {
        if (secuenciaIniciada || choqueFin != null && choqueFin.YaProcesado)
        {
            if (secuenciaIniciada)
            {
                ActualizarSecuencia();
            }

            return;
        }

        if (!PuedeActivarEvento())
        {
            return;
        }

        IniciarSecuenciaChoqueForzado();
    }

    private bool PuedeActivarEvento()
    {
        if (referenciaTorre == null || moto == null)
        {
            return false;
        }

        float metros = puntaje != null
            ? puntaje.MetrosRecorridos
            : DistanceScoreController.Instancia != null
                ? DistanceScoreController.Instancia.MetrosRecorridos
                : 0f;

        if (metros < metrosMinimosRecorridos)
        {
            return false;
        }

        Vector3 motoPlano = Vector3.ProjectOnPlane(moto.transform.position, Vector3.up);
        Vector3 torrePlano = Vector3.ProjectOnPlane(referenciaTorre.position, Vector3.up);
        float distanciaATorre = Vector3.Distance(motoPlano, torrePlano);

        return distanciaATorre <= metrosAntesDeLaTorre;
    }

    private void IniciarSecuenciaChoqueForzado()
    {
        secuenciaIniciada = true;
        tiempoSecuencia = 0f;

        Debug.Log("[EventoTorre] Secuencia de choque forzado iniciada (Torre del Cable).");

        moto.ActivarConduccionForzada(velocidadObjetivoKMH, direccionGiroForzada);
        CrearObstaculoDeBloqueo();
    }

    private void ActualizarSecuencia()
    {
        tiempoSecuencia += Time.deltaTime;

        if (choqueFin != null && choqueFin.YaProcesado)
        {
            return;
        }

        if (tiempoSecuencia >= tiempoMaximoHastaChoque)
        {
            FinalizarConMensajeMotivacional();
        }
    }

    private void CrearObstaculoDeBloqueo()
    {
        Vector3 posicion = moto.transform.position +
                           moto.transform.forward * distanciaObstaculoAdelante;
        posicion.y = moto.transform.position.y + 0.5f;
        Quaternion rotacion = moto.transform.rotation;

        if (spawnerTrafico != null)
        {
            obstaculoGenerado = spawnerTrafico.SpawnObstaculoBloqueo(posicion, rotacion);
        }

        if (obstaculoGenerado == null)
        {
            obstaculoGenerado = CrearObstaculoBasico(posicion, rotacion);
        }
    }

    private static GameObject CrearObstaculoBasico(Vector3 posicion, Quaternion rotacion)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ObstaculoChoqueForzado";
        go.transform.SetPositionAndRotation(posicion, rotacion);
        go.transform.localScale = new Vector3(2.5f, 1.5f, 4f);

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false;
        }

        if (go.GetComponent<EntidadChoqueFin>() == null)
        {
            go.AddComponent<EntidadChoqueFin>();
        }

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = go.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        return go;
    }

    private void FinalizarConMensajeMotivacional()
    {
        if (choqueFin == null)
        {
            Debug.LogWarning("[EventoTorre] Sin ChoqueFinDePartida; no se puede cerrar la partida.");
            return;
        }

        choqueFin.ProcesarChoqueMensajeMotivacionalForzado();
    }

    private void OnDrawGizmosSelected()
    {
        if (referenciaTorre == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawWireSphere(referenciaTorre.position, metrosAntesDeLaTorre);
    }
}
