using UnityEngine;

/// <summary>
/// Marca colliders del mapa (Cube divisor, Building, barreras) para fin de partida al iniciar Test.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ColisionesMapaBootstrap : MonoBehaviour
{
    [SerializeField] private string[] prefijosNombreColision = { "Cube", "Barrera", "barrera", "Building", "divisor" };
    [SerializeField] private string[] nombresExcluidos = { "Plane", "Terrain", "Road_" };

    private void Awake()
    {
        int preparados = 0;
        var colliders = FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger)
            {
                continue;
            }

            GameObject objeto = collider.gameObject;
            if (!DebePrepararColision(objeto))
            {
                continue;
            }

            AsegurarMarcadorChoque(objeto);
            preparados++;
        }

        Debug.Log($"[ColisionesMapa] Colliders de fin de partida preparados: {preparados}");
    }

    private bool DebePrepararColision(GameObject objeto)
    {
        if (objeto == null || DebeOmitir(objeto))
        {
            return false;
        }

        string nombre = objeto.name;

        for (int i = 0; i < nombresExcluidos.Length; i++)
        {
            if (nombre.StartsWith(nombresExcluidos[i]))
            {
                return false;
            }
        }

        if (objeto.CompareTag("Barrera") || objeto.CompareTag("Edificio"))
        {
            return true;
        }

        for (int i = 0; i < prefijosNombreColision.Length; i++)
        {
            string prefijo = prefijosNombreColision[i];
            if (string.IsNullOrEmpty(prefijo))
            {
                continue;
            }

            if (nombre.StartsWith(prefijo))
            {
                return true;
            }

            if (nombre.IndexOf(prefijo, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool DebeOmitir(GameObject objeto)
    {
        if (objeto.GetComponentInParent<MotoController>() != null)
        {
            return true;
        }

        if (objeto.GetComponentInParent<Canvas>() != null)
        {
            return true;
        }

        if (objeto.GetComponent<TrafficLightController>() != null ||
            objeto.GetComponent<TrafficLightObstacle>() != null ||
            objeto.GetComponent<LanePath>() != null ||
            objeto.GetComponent<TrafficVehicleAI>() != null)
        {
            return true;
        }

        return false;
    }

    private static void AsegurarMarcadorChoque(GameObject objeto)
    {
        if (objeto.GetComponent<EntidadChoqueFin>() == null)
        {
            objeto.AddComponent<EntidadChoqueFin>();
        }
    }
}
