using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Activa el cruce de la torre cuando la moto entra en la recta final del mapa (alineada).
/// Después fuerza la conducción hacia el cruce.
/// </summary>
public class ActivadorTraficoTorre : MonoBehaviour
{
    [Header("Cuándo activar conducción automática")]
    [Tooltip("Solo activa cuando la moto está cerca del punto X/Z indicado.")]
    [SerializeField] private bool usarPuntoActivacionXZ = true;
    [SerializeField] private float puntoActivacionX = 650.5f;
    [SerializeField] private float puntoActivacionZ = 1238.05f;
    [Tooltip("Radio en metros: la moto entra aquí para la conducción automática.")]
    [SerializeField] private float metrosCercaParaActivar = 12f;
    [Tooltip("Los coches del cruce arrancan cuando la moto está a esta distancia del punto (antes que tú).")]
    [SerializeField] private float metrosAntesParaIniciarCoches = 45f;
    [Tooltip("Opcional: Empty en escena; si está asignado, usa su posición en lugar de X/Z.")]
    [SerializeField] private Transform marcadorPuntoActivacion;

    [Header("Zona manual (solo si desactivas punto X/Z arriba)")]
    [Tooltip("Crea dos Empty en la escena y arrástralos aquí. Si ambos están asignados, se usan en lugar del carril.")]
    [SerializeField] private Transform marcadorInicioRecta;
    [SerializeField] private Transform marcadorFinRecta;
    [Tooltip("Paso de cebra de la recta (opcional). Si está vacío, se busca cerca del semáforo del cruce.")]
    [SerializeField] private Transform referenciaPasoCebra;
    [SerializeField] private bool alinearMarcadoresConCebra = true;
    [Tooltip("Metros antes de la cebra donde empieza a contar la zona (activación al llegar aquí).")]
    [SerializeField] private float metrosAntesDeLaCebra = 14f;
    [Tooltip("Metros antes de la cebra donde termina la zona (justo antes de las franjas).")]
    [SerializeField] private float metrosFinAntesDeLaCebra = 3f;
    [Tooltip("Margen extra hacia atrás en la zona manual (además de metros antes de la cebra).")]
    [SerializeField] private float metrosExtensionZonaManual = 6f;

    [Header("Recta final por carril (si no usas marcadores)")]
    [SerializeField] private LanePath carrilRectaActivacion;
    [SerializeField] private string nombreCarrilRecta = "Carril_Derecho";
    [Tooltip("-1 = detectar automáticamente el tramo recto final del carril.")]
    [SerializeField] private int indiceWaypointInicioRecta = -1;
    [Tooltip("Metros extra hacia atrás en la recta para activar un poco antes del cruce.")]
    [SerializeField] private float metrosExtensionAtras = 22f;
    [SerializeField] private float alineacionMaximaGrados = 18f;
    [SerializeField] private float distanciaMaximaAlCarril = 10f;

    [Header("Referencias")]
    [SerializeField] private Transform referenciaTorre;
    [SerializeField] private string nombreTorreEnEscena = "TorreHerveo_Manizales";

    [Header("Semáforos")]
    [SerializeField] private TrafficLightController[] semaforosActivar;
    [SerializeField] private string nombreSemaforoEnEscena = "Traffic_light_01_SkF (5)";

    [Header("Moto")]
    [SerializeField] private MotoController moto;
    [SerializeField] private bool forzarConduccionMoto = true;
    [SerializeField] private float velocidadMotoKMH = 70f;
    [SerializeField] private Transform objetivoConduccion;

    [Header("Solo coches de la torre")]
    [SerializeField] private TrafficVehicleAI[] cochesDelCruce;
    [SerializeField] private LanePath carrilCoches;
    [SerializeField] private string nombreCarrilEnEscena = "CarrilFInal";
    [SerializeField] private string[] nombresAlternativosCarril = { "finalpath", "FinalPath", "CarrilFinal" };
    [SerializeField] private Transform contenedorCochesTorre;
    [Tooltip("Posición X/Z en mundo de cada coche (mismo orden que cochesDelCruce).")]
    [SerializeField] private Vector2[] posicionesCochesTorreXZ =
    {
        new Vector2(646.91f, 1262.16f),
        new Vector2(650.34f, 1255.28f),
        new Vector2(667.87f, 1264.81f),
        new Vector2(664.44f, 1270.91f),
        new Vector2(657.79f, 1259f),
        new Vector2(654.03f, 1265.71f)
    };
    [SerializeField] private float margenAlturaSobreSuelo = 0.08f;
    [SerializeField] private LayerMask capasSuelo = ~0;
    [SerializeField] private float velocidadCochesTorreKMH = 45f;

    private bool activado;
    private bool cochesEnMarcha;
    private int indiceInicioRectaCache = -1;
    private int indiceFinRectaCache = -1;

    private void Awake()
    {
        BuscarReferencias();
        CrearMarcadoresSiFaltan();
        AlinearMarcadoresAntesDeCebra();
        BuscarCochesDelCruceEnEscena();
        ResolverSemaforos();
        PrepararIndiceRecta();
        SincronizarCarrilTorre();
        PrepararSoloCochesTorre();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        CrearMarcadoresSiFaltan();
        AlinearMarcadoresAntesDeCebra();
    }

    public void RepartirCochesTorreEnEditor()
    {
        BuscarReferencias();
        SincronizarCarrilTorre();
        PrepararSoloCochesTorre();
    }

    public void SincronizarCarrilTorreEnEditor()
    {
        BuscarReferencias();
        SincronizarCarrilTorre();
    }
#endif

    private void Update()
    {
        if (activado)
        {
            ActualizarConduccionForzada();
            return;
        }

        if (moto == null)
        {
            moto = FindAnyObjectByType<MotoController>();
            if (moto == null)
            {
                return;
            }
        }

        if (usarPuntoActivacionXZ)
        {
            if (!cochesEnMarcha && MotoCercaDelPuntoActivacion(metrosAntesParaIniciarCoches))
            {
                IniciarCochesDelCruce();
            }

            if (MotoCercaDelPuntoActivacion(metrosCercaParaActivar))
            {
                ActivarConduccionMoto();
            }

            return;
        }

        if (!MotoEnRectaFinal())
        {
            return;
        }

        if (!cochesEnMarcha)
        {
            IniciarCochesDelCruce();
        }

        ActivarConduccionMoto();
    }

    private void IniciarCochesDelCruce()
    {
        if (cochesEnMarcha)
        {
            return;
        }

        cochesEnMarcha = true;
        PonerSemaforosEnVerde();
        ActivarCochesTorre();
        Debug.Log("[ActivadorTraficoTorre] Coches del cruce en marcha.");
    }

    private void ActivarConduccionMoto()
    {
        if (activado)
        {
            return;
        }

        activado = true;

        if (!cochesEnMarcha)
        {
            IniciarCochesDelCruce();
        }

        if (forzarConduccionMoto && moto != null)
        {
            moto.ActivarConduccionForzada(velocidadMotoKMH, 0f);
        }

        Debug.Log("[ActivadorTraficoTorre] Conducción automática de la moto activada.");
    }

    private void ActualizarConduccionForzada()
    {
        if (!forzarConduccionMoto || moto == null)
        {
            return;
        }

        moto.ActivarConduccionForzada(velocidadMotoKMH, 0f);
    }

    private bool UsaZonaManual => marcadorInicioRecta != null && marcadorFinRecta != null;

    private bool MotoEnRectaFinal()
    {
        if (usarPuntoActivacionXZ)
        {
            return MotoCercaDelPuntoActivacion();
        }

        if (UsaZonaManual)
        {
            return MotoEnSegmentoRecta(
                Plano(marcadorInicioRecta.position),
                Plano(marcadorFinRecta.position),
                extensionAtras: metrosExtensionZonaManual);
        }

        if (carrilRectaActivacion == null || carrilRectaActivacion.PointCount < 2)
        {
            return false;
        }

        PrepararIndiceRecta();

        Vector3 inicio = Plano(carrilRectaActivacion.GetPoint(indiceInicioRectaCache));
        Vector3 fin = Plano(carrilRectaActivacion.GetPoint(indiceFinRectaCache));
        return MotoEnSegmentoRecta(inicio, fin, metrosExtensionAtras);
    }

    private bool MotoCercaDelPuntoActivacion(float radioMetros = -1f)
    {
        if (moto == null)
        {
            return false;
        }

        if (radioMetros < 0f)
        {
            radioMetros = metrosCercaParaActivar;
        }

        Vector3 punto = ObtenerPuntoActivacionMundo();
        Vector3 motoPlano = Plano(moto.transform.position);
        float distancia = Vector3.Distance(motoPlano, Plano(punto));
        return distancia <= radioMetros;
    }

    private Vector3 ObtenerPuntoActivacionMundo()
    {
        if (marcadorPuntoActivacion != null)
        {
            return marcadorPuntoActivacion.position;
        }

        float altura = moto != null ? moto.transform.position.y : transform.position.y;
        return new Vector3(puntoActivacionX, altura, puntoActivacionZ);
    }

    private bool MotoEnSegmentoRecta(Vector3 inicio, Vector3 fin, float extensionAtras)
    {
        Vector3 segmento = fin - inicio;
        float largo = segmento.magnitude;
        if (largo < 1f || moto == null)
        {
            return false;
        }

        Vector3 dirRecta = segmento / largo;
        Vector3 motoPlano = Plano(moto.transform.position);

        float progreso = Vector3.Dot(motoPlano - inicio, dirRecta);
        if (progreso < -extensionAtras || progreso > largo + 12f)
        {
            return false;
        }

        Vector3 masCercano = inicio + dirRecta * Mathf.Clamp(progreso, 0f, largo);
        if (Vector3.Distance(motoPlano, masCercano) > distanciaMaximaAlCarril)
        {
            return false;
        }

        float angulo = Vector3.Angle(moto.transform.forward, dirRecta);
        if (angulo > alineacionMaximaGrados)
        {
            return false;
        }

        return Vector3.Dot(moto.transform.forward, dirRecta) > 0.25f;
    }

    private void PrepararIndiceRecta()
    {
        if (carrilRectaActivacion == null || carrilRectaActivacion.PointCount < 2)
        {
            return;
        }

        if (indiceInicioRectaCache >= 0 && indiceWaypointInicioRecta < 0)
        {
            return;
        }

        if (indiceWaypointInicioRecta >= 0)
        {
            indiceInicioRectaCache = Mathf.Clamp(
                indiceWaypointInicioRecta,
                0,
                carrilRectaActivacion.PointCount - 2);
        }
        else
        {
            indiceInicioRectaCache = DetectarInicioRectaFinal(carrilRectaActivacion);
        }

        indiceFinRectaCache = ObtenerIndiceFinRecta(carrilRectaActivacion, indiceInicioRectaCache);
    }

    private int ObtenerIndiceFinRecta(LanePath carril, int indiceInicio)
    {
        Vector3 meta = objetivoConduccion != null
            ? objetivoConduccion.position
            : referenciaTorre != null
                ? referenciaTorre.position
                : carril.GetPoint(carril.PointCount - 1);

        Vector3 metaPlano = Plano(meta);
        int mejor = Mathf.Clamp(indiceInicio + 1, 0, carril.PointCount - 1);
        float mejorDist = float.MaxValue;

        for (int i = indiceInicio; i < carril.PointCount; i++)
        {
            float dist = Vector3.SqrMagnitude(Plano(carril.GetPoint(i)) - metaPlano);
            if (dist < mejorDist)
            {
                mejorDist = dist;
                mejor = i;
            }
        }

        return mejor;
    }

    private static int DetectarInicioRectaFinal(LanePath carril)
    {
        int count = carril.PointCount;
        if (count < 3)
        {
            return 0;
        }

        int inicio = count - 2;
        const float anguloMaximoCurva = 14f;

        for (int i = count - 3; i >= 0; i--)
        {
            Vector3 a = Plano(carril.GetPoint(i));
            Vector3 b = Plano(carril.GetPoint(i + 1));
            Vector3 c = Plano(carril.GetPoint(i + 2));
            Vector3 dir1 = b - a;
            Vector3 dir2 = c - b;
            if (dir1.sqrMagnitude < 0.25f || dir2.sqrMagnitude < 0.25f)
            {
                break;
            }

            if (Vector3.Angle(dir1, dir2) > anguloMaximoCurva)
            {
                break;
            }

            inicio = i;
        }

        return Mathf.Max(0, inicio - 1);
    }

    private Vector3 ObtenerCentroCochesTorre()
    {
        if (cochesDelCruce == null || cochesDelCruce.Length == 0)
        {
            return referenciaTorre != null ? referenciaTorre.position : transform.position;
        }

        Vector3 suma = Vector3.zero;
        int n = 0;
        for (int i = 0; i < cochesDelCruce.Length; i++)
        {
            if (cochesDelCruce[i] == null)
            {
                continue;
            }

            suma += cochesDelCruce[i].transform.position;
            n++;
        }

        return n > 0 ? suma / n : transform.position;
    }

    private void PonerSemaforosEnVerde()
    {
        if (semaforosActivar == null)
        {
            return;
        }

        for (int i = 0; i < semaforosActivar.Length; i++)
        {
            TrafficLightController semaforo = semaforosActivar[i];
            if (semaforo == null)
            {
                continue;
            }

            semaforo.SetAutomaticoActivo(false);
            semaforo.SetState(TrafficLightController.TrafficLightState.Green);
        }
    }

    private void PrepararSoloCochesTorre()
    {
        if (cochesDelCruce == null || carrilCoches == null || carrilCoches.PointCount == 0)
        {
            return;
        }

        DistribuirCochesEnPosiciones();

        for (int i = 0; i < cochesDelCruce.Length; i++)
        {
            ConfigurarCocheTorre(cochesDelCruce[i], enMarcha: false);
        }
    }

    private void ActivarCochesTorre()
    {
        if (cochesDelCruce == null)
        {
            return;
        }

        SincronizarCarrilTorre();

        for (int i = 0; i < cochesDelCruce.Length; i++)
        {
            ConfigurarCocheTorre(cochesDelCruce[i], enMarcha: true);
        }
    }

    private void DistribuirCochesEnPosiciones()
    {
        Transform contenedor = ObtenerContenedorCochesTorre();
        if (contenedor == null || posicionesCochesTorreXZ == null)
        {
            return;
        }

        for (int i = 0; i < cochesDelCruce.Length; i++)
        {
            TrafficVehicleAI carro = cochesDelCruce[i];
            if (carro == null || i >= posicionesCochesTorreXZ.Length)
            {
                continue;
            }

            Vector2 xz = posicionesCochesTorreXZ[i];
            ColocarCocheEnPosicionMundo(carro, contenedor, xz.x, xz.y);
        }
    }

    private void ColocarCocheEnPosicionMundo(TrafficVehicleAI carro, Transform contenedor, float x, float z)
    {
        Vector3 posicion = new Vector3(x, 0f, z);
        posicion.y = ObtenerAlturaSuelo(posicion);

        int waypoint = carrilCoches.GetIndiceWaypointEnPosicion(posicion);
        Quaternion rotacion = carrilCoches.GetRotacionEnPosicion(posicion);
        Vector3 centroCarril = carrilCoches.GetPoint(waypoint);
        Vector3 lateral = Vector3.Cross(Vector3.up, rotacion * Vector3.forward).normalized;
        Vector3 desplazamiento = Vector3.ProjectOnPlane(posicion - centroCarril, Vector3.up);

        carro.offsetLateralCarril = Vector3.Dot(desplazamiento, lateral);
        carro.ColocarEnCarril(carrilCoches, waypoint, posicion, rotacion);

        if (carro.transform.parent != contenedor)
        {
            carro.transform.SetParent(contenedor, true);
        }
    }

    private float ObtenerAlturaSuelo(Vector3 posicionPlano)
    {
        Vector3 origen = new Vector3(posicionPlano.x, 200f, posicionPlano.z);
        if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit, 400f, capasSuelo, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y + margenAlturaSobreSuelo;
        }

        if (carrilCoches != null && carrilCoches.PointCount > 0)
        {
            return carrilCoches.GetPoint(0).y + margenAlturaSobreSuelo;
        }

        return posicionPlano.y;
    }

    private Quaternion ObtenerRotacionCarril(int waypoint)
    {
        int siguiente = carrilCoches.GetNextIndex(waypoint);
        Vector3 adelante = Vector3.ProjectOnPlane(
            carrilCoches.GetPoint(siguiente) - carrilCoches.GetPoint(waypoint),
            Vector3.up);

        if (adelante.sqrMagnitude < 0.001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(adelante.normalized, Vector3.up);
    }

    private Transform ObtenerContenedorCochesTorre()
    {
        if (contenedorCochesTorre != null)
        {
            return contenedorCochesTorre;
        }

        contenedorCochesTorre = transform.Find("CochesCruceTorre");
        if (contenedorCochesTorre == null)
        {
            var go = new GameObject("CochesCruceTorre");
            go.transform.SetParent(transform, false);
            contenedorCochesTorre = go.transform;
        }

        return contenedorCochesTorre;
    }

    private void ConfigurarCocheTorre(TrafficVehicleAI carro, bool enMarcha)
    {
        if (carro == null)
        {
            return;
        }

        if (carro.GetComponent<VehiculoCruceTorre>() == null)
        {
            carro.gameObject.AddComponent<VehiculoCruceTorre>();
        }

        carro.destruirAlFinalDelCarril = true;

        Rigidbody rb = carro.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !enMarcha;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (!enMarcha)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        VehiculoCruceTorre marca = carro.GetComponent<VehiculoCruceTorre>();
        if (marca != null)
        {
            marca.ignorarMotoEnSensor = true;
        }

        if (enMarcha && carrilCoches != null && carrilCoches.PointCount >= 2)
        {
            carro.lanePath = carrilCoches;
            int wp = carrilCoches.GetIndiceWaypointEnPosicion(carro.transform.position);
            carro.ActualizarWaypointEnCarril(wp);
        }

        carro.enabled = enMarcha;
        carro.ConfigurarVelocidadObjetivo(enMarcha ? velocidadCochesTorreKMH : 0f);
    }

    private void SincronizarCarrilTorre()
    {
        if (carrilCoches == null)
        {
            return;
        }

        RestaurarCarrilSinPuntosGenerados();
        carrilCoches.SincronizarPuntosDesdeHijos();
    }

    private void RestaurarCarrilSinPuntosGenerados()
    {
        if (carrilCoches == null)
        {
            return;
        }

        Transform raiz = carrilCoches.transform;
        for (int i = raiz.childCount - 1; i >= 0; i--)
        {
            Transform hijo = raiz.GetChild(i);
            if (!hijo.name.StartsWith("PuntoTorre_"))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(hijo.gameObject);
            }
            else
            {
                DestroyImmediate(hijo.gameObject);
            }
        }

        if (carrilCoches.puntos == null)
        {
            return;
        }

        var lista = new List<Transform>(carrilCoches.puntos.Length);
        for (int i = 0; i < carrilCoches.puntos.Length; i++)
        {
            Transform punto = carrilCoches.puntos[i];
            if (punto != null && !punto.name.StartsWith("PuntoTorre_"))
            {
                lista.Add(punto);
            }
        }

        if (lista.Count > 0)
        {
            carrilCoches.puntos = lista.ToArray();
        }
    }

    private LanePath BuscarCarrilTorreEnEscena()
    {
        LanePath encontrado = BuscarLanePathPorNombre(nombreCarrilEnEscena);
        if (encontrado != null)
        {
            return encontrado;
        }

        if (nombresAlternativosCarril == null)
        {
            return null;
        }

        for (int i = 0; i < nombresAlternativosCarril.Length; i++)
        {
            encontrado = BuscarLanePathPorNombre(nombresAlternativosCarril[i]);
            if (encontrado != null)
            {
                return encontrado;
            }
        }

        return null;
    }

    private static LanePath BuscarLanePathPorNombre(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
        {
            return null;
        }

        GameObject carrilGo = GameObject.Find(nombre);
        return carrilGo != null ? carrilGo.GetComponent<LanePath>() : null;
    }

    private void BuscarReferencias()
    {
        if (marcadorInicioRecta == null)
        {
            GameObject inicio = GameObject.Find("InicoTorre");
            if (inicio == null)
            {
                inicio = GameObject.Find("InicioTorre");
            }

            if (inicio != null)
            {
                marcadorInicioRecta = inicio.transform;
            }
        }

        if (marcadorFinRecta == null)
        {
            GameObject fin = GameObject.Find("FIntoree");
            if (fin == null)
            {
                fin = GameObject.Find("FinTorre");
            }

            if (fin != null)
            {
                marcadorFinRecta = fin.transform;
            }
        }

        if (referenciaTorre == null && !string.IsNullOrEmpty(nombreTorreEnEscena))
        {
            GameObject torre = GameObject.Find(nombreTorreEnEscena);
            if (torre != null)
            {
                referenciaTorre = torre.transform;
            }
        }

        if (carrilRectaActivacion == null && !string.IsNullOrEmpty(nombreCarrilRecta))
        {
            GameObject carrilGo = GameObject.Find(nombreCarrilRecta);
            if (carrilGo != null)
            {
                carrilRectaActivacion = carrilGo.GetComponent<LanePath>();
            }
        }

        if (carrilCoches == null)
        {
            carrilCoches = BuscarCarrilTorreEnEscena();
        }

        if (carrilCoches != null)
        {
            SincronizarCarrilTorre();
        }

        if (moto == null)
        {
            moto = FindAnyObjectByType<MotoController>();
        }

        CrearMarcadoresSiFaltan();
        AlinearMarcadoresAntesDeCebra();
        BuscarCochesDelCruceEnEscena();
    }

    private void BuscarCochesDelCruceEnEscena()
    {
        if (TieneCochesAsignados())
        {
            return;
        }

        Vector3 centro = objetivoConduccion != null
            ? objetivoConduccion.position
            : referenciaTorre != null
                ? referenciaTorre.position
                : transform.position;

        TrafficVehicleAI[] todos = FindObjectsByType<TrafficVehicleAI>(FindObjectsSortMode.None);
        var lista = new List<TrafficVehicleAI>();

        for (int i = 0; i < todos.Length; i++)
        {
            TrafficVehicleAI coche = todos[i];
            if (coche == null)
            {
                continue;
            }

            string nombre = coche.gameObject.name;
            if (!EsCocheDelCrucePorNombre(nombre))
            {
                continue;
            }

            if (Vector3.Distance(Plano(coche.transform.position), Plano(centro)) > 45f)
            {
                continue;
            }

            if (!lista.Contains(coche))
            {
                lista.Add(coche);
            }
        }

        if (lista.Count > 0)
        {
            cochesDelCruce = lista.ToArray();
        }
    }

    private bool TieneCochesAsignados()
    {
        if (cochesDelCruce == null || cochesDelCruce.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < cochesDelCruce.Length; i++)
        {
            if (cochesDelCruce[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool EsCocheDelCrucePorNombre(string nombre)
    {
        return nombre.Contains("ARCADE")
            || nombre.Contains("Car02")
            || nombre.Contains("Pickup");
    }

    [ContextMenu("Recrear marcadores InicoTorre / FIntoree")]
    public void RecrearMarcadoresRecta()
    {
        marcadorInicioRecta = null;
        marcadorFinRecta = null;
        CrearMarcadoresSiFaltan();
        AlinearMarcadoresAntesDeCebra();
    }

    [ContextMenu("Alinear marcadores antes del paso de cebra")]
    public void AlinearMarcadoresAntesDeCebraMenu()
    {
        AlinearMarcadoresAntesDeCebra();
    }

    private void CrearMarcadoresSiFaltan()
    {
        if (marcadorInicioRecta == null)
        {
            marcadorInicioRecta = ObtenerOCrearMarcador("InicoTorre", new Vector3(705f, 0.15f, 1158f));
        }

        if (marcadorFinRecta == null)
        {
            marcadorFinRecta = ObtenerOCrearMarcador("FIntoree", new Vector3(686f, 0.15f, 1170f));
        }
    }

    private void AlinearMarcadoresAntesDeCebra()
    {
        if (!alinearMarcadoresConCebra || marcadorInicioRecta == null || marcadorFinRecta == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RecordObject(marcadorInicioRecta, "Alinear InicoTorre");
            UnityEditor.Undo.RecordObject(marcadorFinRecta, "Alinear FIntoree");
        }
#endif

        if (!TryObtenerPosicionCebra(out Vector3 cebra))
        {
            OrientarMarcadorHacia(marcadorInicioRecta, marcadorFinRecta);
            return;
        }

        Vector3 haciaTorre = ObtenerDireccionHaciaTorre(Plano(cebra));

        Vector3 dir = haciaTorre.normalized;
        float finDist = Mathf.Max(1f, metrosFinAntesDeLaCebra);
        float inicioDist = finDist + Mathf.Max(3f, metrosAntesDeLaCebra);

        Vector3 fin = Plano(cebra) - dir * finDist;
        Vector3 inicio = Plano(cebra) - dir * inicioDist;

        marcadorFinRecta.position = new Vector3(fin.x, marcadorFinRecta.position.y, fin.z);
        marcadorInicioRecta.position = new Vector3(inicio.x, marcadorInicioRecta.position.y, inicio.z);

        OrientarMarcadorHacia(marcadorInicioRecta, marcadorFinRecta);
    }

    private bool TryObtenerPosicionCebra(out Vector3 cebra)
    {
        if (referenciaPasoCebra != null)
        {
            cebra = referenciaPasoCebra.position;
            return true;
        }

        Vector3 refRecta = ObtenerReferenciaRectaAproximada();

        Crosswalk[] cruces = FindObjectsByType<Crosswalk>(FindObjectsSortMode.None);
        float mejorDist = float.MaxValue;
        bool encontrado = false;
        cebra = refRecta;

        for (int i = 0; i < cruces.Length; i++)
        {
            Crosswalk cruce = cruces[i];
            if (cruce == null)
            {
                continue;
            }

            Vector3 pos = Plano(cruce.transform.position);
            if (!EstaEnRectaFinalDelMapa(pos))
            {
                continue;
            }

            float dist = Vector3.SqrMagnitude(pos - Plano(refRecta));
            if (dist < mejorDist)
            {
                mejorDist = dist;
                cebra = cruce.transform.position;
                encontrado = true;
            }
        }

        if (encontrado)
        {
            return true;
        }

        GameObject cebraVisual = GameObject.Find("Cube (51)");
        if (cebraVisual != null)
        {
            cebra = cebraVisual.transform.position;
            return true;
        }

        cebra = refRecta;
        return true;
    }

    private Vector3 ObtenerReferenciaRectaAproximada()
    {
        if (marcadorFinRecta != null && marcadorInicioRecta != null)
        {
            return (marcadorFinRecta.position + marcadorInicioRecta.position) * 0.5f;
        }

        if (objetivoConduccion != null)
        {
            return new Vector3(687.6f, 0f, objetivoConduccion.position.z - 82f);
        }

        return new Vector3(687.6f, 0f, 1173f);
    }

    private static bool EstaEnRectaFinalDelMapa(Vector3 pos)
    {
        return pos.x >= 630f && pos.x <= 720f && pos.z >= 1135f && pos.z <= 1205f;
    }

    private Vector3 ObtenerDireccionHaciaTorre(Vector3 desde)
    {
        if (carrilRectaActivacion != null && carrilRectaActivacion.PointCount >= 2)
        {
            int ultimo = carrilRectaActivacion.PointCount - 1;
            int prev = Mathf.Max(0, ultimo - 1);
            Vector3 dirCarril = Plano(carrilRectaActivacion.GetPoint(ultimo)) -
                                Plano(carrilRectaActivacion.GetPoint(prev));
            if (dirCarril.sqrMagnitude > 0.01f)
            {
                return dirCarril.normalized;
            }
        }

        Vector3 meta = objetivoConduccion != null
            ? Plano(objetivoConduccion.position)
            : referenciaTorre != null
                ? Plano(referenciaTorre.position)
                : desde + new Vector3(-1f, 0f, 1f);

        Vector3 hacia = meta - desde;
        if (hacia.sqrMagnitude < 0.01f)
        {
            hacia = new Vector3(-1f, 0f, 1f);
        }

        return hacia.normalized;
    }

    private Transform ObtenerOCrearMarcador(string nombre, Vector3 posicionMundo)
    {
        Transform existente = transform.Find(nombre);
        if (existente != null)
        {
            return existente;
        }

        GameObject buscado = GameObject.Find(nombre);
        if (buscado != null)
        {
            buscado.transform.SetParent(transform, true);
            return buscado.transform;
        }

        var marcador = new GameObject(nombre);
        marcador.transform.SetParent(transform, true);
        marcador.transform.position = posicionMundo;
        marcador.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(marcador, "Crear " + nombre);
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

        return marcador.transform;
    }

    private static void OrientarMarcadorHacia(Transform origen, Transform destino)
    {
        if (origen == null || destino == null)
        {
            return;
        }

        Vector3 dir = Vector3.ProjectOnPlane(destino.position - origen.position, Vector3.up);
        if (dir.sqrMagnitude > 0.01f)
        {
            origen.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    private void ResolverSemaforos()
    {
        if (semaforosActivar != null && semaforosActivar.Length > 0)
        {
            return;
        }

        var lista = new List<TrafficLightController>();
        if (!string.IsNullOrEmpty(nombreSemaforoEnEscena))
        {
            GameObject semaforoGo = GameObject.Find(nombreSemaforoEnEscena);
            if (semaforoGo != null)
            {
                lista.AddRange(semaforoGo.GetComponentsInChildren<TrafficLightController>(true));
            }
        }

        GameObject otro = GameObject.Find("Traffic_light_01_SkF (2)");
        if (otro != null)
        {
            TrafficLightController[] extras = otro.GetComponentsInChildren<TrafficLightController>(true);
            for (int i = 0; i < extras.Length; i++)
            {
                if (!lista.Contains(extras[i]))
                {
                    lista.Add(extras[i]);
                }
            }
        }

        semaforosActivar = lista.ToArray();
    }

    private static Vector3 Plano(Vector3 posicion) => Vector3.ProjectOnPlane(posicion, Vector3.up);

    private void OnDrawGizmosSelected()
    {
        if (usarPuntoActivacionXZ)
        {
            Vector3 punto = ObtenerPuntoActivacionMundo();
            Vector3 puntoPlano = Plano(punto);

            Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.9f);
            Gizmos.DrawWireSphere(puntoPlano, metrosAntesParaIniciarCoches);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(puntoPlano, metrosCercaParaActivar);
            Gizmos.DrawSphere(puntoPlano, 1.5f);
            return;
        }

        Vector3 inicio;
        Vector3 fin;
        float extension;

        if (UsaZonaManual)
        {
            inicio = Plano(marcadorInicioRecta.position);
            fin = Plano(marcadorFinRecta.position);
            extension = 0f;
        }
        else if (carrilRectaActivacion != null && carrilRectaActivacion.PointCount >= 2)
        {
            int idxInicio = indiceWaypointInicioRecta >= 0
                ? indiceWaypointInicioRecta
                : DetectarInicioRectaFinal(carrilRectaActivacion);
            int idxFin = indiceFinRectaCache >= 0
                ? indiceFinRectaCache
                : carrilRectaActivacion.PointCount - 1;
            inicio = Plano(carrilRectaActivacion.GetPoint(idxInicio));
            fin = Plano(carrilRectaActivacion.GetPoint(idxFin));
            extension = metrosExtensionAtras;
        }
        else
        {
            return;
        }

        Vector3 dir = (fin - inicio).normalized;
        Vector3 inicioExt = inicio - dir * extension;
        float ancho = distanciaMaximaAlCarril;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(inicioExt, fin);
        Gizmos.DrawSphere(inicioExt, 1.2f);
        Gizmos.DrawSphere(fin, 1.2f);

        Vector3 lateral = Vector3.Cross(Vector3.up, dir).normalized * ancho;
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawLine(inicioExt + lateral, fin + lateral);
        Gizmos.DrawLine(inicioExt - lateral, fin - lateral);
    }

    private void OnDisable()
    {
        if (moto != null && moto.ConduccionForzadaActiva)
        {
            moto.DesactivarConduccionForzada();
        }
    }
}
