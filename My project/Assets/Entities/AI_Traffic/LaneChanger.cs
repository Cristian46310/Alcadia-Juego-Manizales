using System.Collections;
using UnityEngine;

/// <summary>
/// Agrega este componente al vehículo de IA junto a TrafficVehicleAI.
/// Detecta si lleva un obstáculo lento al frente por más de 'tiempoParaCambiar'
/// segundos y, si el carril adyacente está libre, ejecuta el cambio.
/// </summary>
[RequireComponent(typeof(TrafficVehicleAI))]
public class LaneChanger : MonoBehaviour
{
    [Header("Carriles disponibles")]
    public LanePath[] carrilesAdyacentes;

    [Header("Parámetros")]
    [Tooltip("Segundos con obstáculo lento antes de intentar cambiar")]
    public float tiempoParaCambiar = 2.5f;

    [Tooltip("Velocidad del obstáculo considerada 'lento' (km/h)")]
    public float umbralVelocidadLento = 15f;

    [Tooltip("Longitud del SphereCast lateral para verificar carril libre")]
    public float largoSensorLateral = 8f;

    [Tooltip("Radio del SphereCast lateral")]
    public float radioSensorLateral = 1.2f;

    public LayerMask capasVerificacion = ~0;

    private TrafficVehicleAI ia;
    private float tiempoConObstaculo;
    private bool cambiandoCarril;

    private void Awake()
    {
        ia = GetComponent<TrafficVehicleAI>();
    }

    private void FixedUpdate()
    {
        if (cambiandoCarril || carrilesAdyacentes.Length == 0) return;

        if (ObstaculoLentoAlFrente())
        {
            tiempoConObstaculo += Time.fixedDeltaTime;

            if (tiempoConObstaculo >= tiempoParaCambiar)
            {
                IntentarCambioDeCarril();
            }
        }
        else
        {
            tiempoConObstaculo = 0f;
        }
    }

    private bool ObstaculoLentoAlFrente()
    {
        Vector3 origen = transform.position + Vector3.up * ia.alturaSensor;

        if (!Physics.SphereCast(
                origen,
                ia.radioSensor,
                transform.forward,
                out RaycastHit hit,
                ia.largoSensor,
                capasVerificacion,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        // Ignora semáforos; solo reacciona a vehículos/jugador
        if (hit.collider.GetComponent<TrafficLightObstacle>() != null) return false;

        // Verifica si el obstáculo va lento
        if (hit.rigidbody != null)
        {
            float velocidadKMH = hit.rigidbody.linearVelocity.magnitude * 3.6f;
            return velocidadKMH < umbralVelocidadLento;
        }

        // Obstáculo estático (edificio, barrera) también cuenta
        return true;
    }

    private void IntentarCambioDeCarril()
    {
        foreach (LanePath candidato in carrilesAdyacentes)
        {
            if (candidato == ia.Lane) continue;
            if (!CarrilLibre(candidato)) continue;

            // Encontrar el waypoint más cercano en el carril candidato
            int indiceMasCercano = EncontrarWaypointMasCercano(candidato);
            StartCoroutine(EjecutarCambio(candidato, indiceMasCercano));
            return;
        }

        // Ningún carril disponible: resetear temporizador y volver a evaluar
        tiempoConObstaculo = 0f;
    }

    private bool CarrilLibre(LanePath carril)
    {
        // SphereCast diagonal hacia el carril para verificar espacio
        Vector3 dirCarril = (carril.GetPoint(0) - transform.position).normalized;
        Vector3 origen = transform.position + Vector3.up * ia.alturaSensor;

        // Verificación lateral
        Vector3 dirLateral = Vector3.Cross(Vector3.up,
            transform.forward).normalized * Mathf.Sign(
            Vector3.Dot(dirCarril, transform.right));

        return !Physics.SphereCast(
            origen,
            radioSensorLateral,
            dirLateral,
            out _,
            largoSensorLateral,
            capasVerificacion,
            QueryTriggerInteraction.Ignore);
    }

    private int EncontrarWaypointMasCercano(LanePath carril)
    {
        int mejor = 0;
        float menorDistancia = float.MaxValue;

        for (int i = 0; i < carril.PointCount; i++)
        {
            float dist = Vector3.Distance(transform.position, carril.GetPoint(i));
            if (dist < menorDistancia)
            {
                menorDistancia = dist;
                mejor = i;
            }
        }

        // Avanzar un waypoint para no teleportar hacia atrás
        return carril.GetNextIndex(mejor);
    }

    private IEnumerator EjecutarCambio(LanePath nuevoCarril, int indice)
    {
        cambiandoCarril = true;
        tiempoConObstaculo = 0f;

        // Pequeña pausa para que la rotación del vehículo inicie el giro naturalmente
        yield return new WaitForSeconds(0.2f);

        ia.ConfigurarCarril(nuevoCarril, indice);

        // Cooldown antes de poder cambiar de carril de nuevo
        yield return new WaitForSeconds(4f);
        cambiandoCarril = false;
    }
}