using UnityEngine;

public class TrafficLightIntersectionController : MonoBehaviour
{
    [Header("Grupos del Cruce")]
    public TrafficLightController[] grupoA;
    public TrafficLightController[] grupoB;

    [Header("Tiempos")]
    public float duracionVerde = 10f;
    public float duracionAmarillo = 2f;
    public float duracionTodoRojo = 1f;

    [Header("Inicio")]
    public bool iniciarConGrupoAEnVerde = true;

    private enum FaseCruce
    {
        AVerde,
        AAmarillo,
        TodoRojoAntesDeB,
        BVerde,
        BAmarillo,
        TodoRojoAntesDeA
    }

    private FaseCruce faseActual;
    private float temporizadorFase;

    private void Start()
    {
        DesactivarModoAutomatico(grupoA);
        DesactivarModoAutomatico(grupoB);

        faseActual = iniciarConGrupoAEnVerde ? FaseCruce.AVerde : FaseCruce.BVerde;
        AplicarFase();
    }

    private void Update()
    {
        temporizadorFase += Time.deltaTime;

        if (temporizadorFase < ObtenerDuracionFaseActual())
        {
            return;
        }

        AvanzarFase();
        AplicarFase();
    }

    private void AvanzarFase()
    {
        faseActual = faseActual switch
        {
            FaseCruce.AVerde => FaseCruce.AAmarillo,
            FaseCruce.AAmarillo => FaseCruce.TodoRojoAntesDeB,
            FaseCruce.TodoRojoAntesDeB => FaseCruce.BVerde,
            FaseCruce.BVerde => FaseCruce.BAmarillo,
            FaseCruce.BAmarillo => FaseCruce.TodoRojoAntesDeA,
            _ => FaseCruce.AVerde
        };
    }

    private float ObtenerDuracionFaseActual()
    {
        return faseActual switch
        {
            FaseCruce.AVerde => duracionVerde,
            FaseCruce.BVerde => duracionVerde,
            FaseCruce.AAmarillo => duracionAmarillo,
            FaseCruce.BAmarillo => duracionAmarillo,
            _ => duracionTodoRojo
        };
    }

    private void AplicarFase()
    {
        temporizadorFase = 0f;

        switch (faseActual)
        {
            case FaseCruce.AVerde:
                AplicarEstadoAGrupo(grupoA, TrafficLightController.TrafficLightState.Green);
                AplicarEstadoAGrupo(grupoB, TrafficLightController.TrafficLightState.Red);
                break;
            case FaseCruce.AAmarillo:
                AplicarEstadoAGrupo(grupoA, TrafficLightController.TrafficLightState.Yellow);
                AplicarEstadoAGrupo(grupoB, TrafficLightController.TrafficLightState.Red);
                break;
            case FaseCruce.BVerde:
                AplicarEstadoAGrupo(grupoA, TrafficLightController.TrafficLightState.Red);
                AplicarEstadoAGrupo(grupoB, TrafficLightController.TrafficLightState.Green);
                break;
            case FaseCruce.BAmarillo:
                AplicarEstadoAGrupo(grupoA, TrafficLightController.TrafficLightState.Red);
                AplicarEstadoAGrupo(grupoB, TrafficLightController.TrafficLightState.Yellow);
                break;
            default:
                AplicarEstadoAGrupo(grupoA, TrafficLightController.TrafficLightState.Red);
                AplicarEstadoAGrupo(grupoB, TrafficLightController.TrafficLightState.Red);
                break;
        }
    }

    private void DesactivarModoAutomatico(TrafficLightController[] grupo)
    {
        if (grupo == null)
        {
            return;
        }

        for (int i = 0; i < grupo.Length; i++)
        {
            if (grupo[i] != null)
            {
                grupo[i].SetAutomaticoActivo(false);
            }
        }
    }

    private void AplicarEstadoAGrupo(TrafficLightController[] grupo, TrafficLightController.TrafficLightState estado)
    {
        if (grupo == null)
        {
            return;
        }

        for (int i = 0; i < grupo.Length; i++)
        {
            if (grupo[i] != null)
            {
                grupo[i].SetState(estado);
            }
        }
    }
}
