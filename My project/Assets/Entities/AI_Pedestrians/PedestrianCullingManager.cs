using System.Collections.Generic;
using UnityEngine;

public class PedestrianCullingManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camaraObjetivo;
    [SerializeField] private Transform jugador;

    [Header("Distancias")]
    [SerializeField] private float radioSiempreActivo = 25f;
    [SerializeField] private float distanciaDesactivar = 70f;
    [SerializeField] private float distanciaReactivar = 55f;

    [Header("Rendimiento")]
    [SerializeField] private float intervaloChequeo = 0.25f;
    [SerializeField] private int maxPorTick = 20;
    [SerializeField] private float intervaloRescan = 2f;

    [Header("Visibilidad")]
    [SerializeField] private bool requerirEnCamaraParaActivar = true;

    private readonly List<PedestrianRef> peatones = new List<PedestrianRef>(256);
    private Plane[] frustumPlanes;
    private float timerChequeo;
    private float timerRescan;
    private int indiceCiclo;

    private class PedestrianRef
    {
        public GameObject go;
        public Transform tr;
        public Renderer[] renderers;
        public Collider[] colliders;
        public Animator animator;
        public Rigidbody rb;
        public MonoBehaviour pedestrianAI;
        public MonoBehaviour sidewalkWalker;
        public bool activo;
    }

    private void Awake()
    {
        if (camaraObjetivo == null)
        {
            camaraObjetivo = Camera.main;
        }

        if (jugador == null && camaraObjetivo != null)
        {
            jugador = camaraObjetivo.transform;
        }

        RefrescarListaPeatones();
    }

    private void Update()
    {
        timerChequeo += Time.deltaTime;
        timerRescan += Time.deltaTime;

        if (timerRescan >= intervaloRescan)
        {
            timerRescan = 0f;
            RefrescarListaPeatones();
        }

        if (timerChequeo < intervaloChequeo)
        {
            return;
        }

        timerChequeo = 0f;

        if (camaraObjetivo != null)
        {
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camaraObjetivo);
        }

        int total = peatones.Count;
        if (total == 0)
        {
            return;
        }

        int procesados = 0;
        while (procesados < maxPorTick && total > 0)
        {
            if (indiceCiclo >= total)
            {
                indiceCiclo = 0;
            }

            PedestrianRef ped = peatones[indiceCiclo];
            indiceCiclo++;
            procesados++;

            if (ped == null || ped.go == null)
            {
                continue;
            }

            bool debeEstarActivo = CalcularEstadoObjetivo(ped);
            if (debeEstarActivo != ped.activo)
            {
                AplicarEstado(ped, debeEstarActivo);
            }
        }
    }

    private void RefrescarListaPeatones()
    {
        peatones.Clear();

        GameObject[] encontrados = GameObject.FindGameObjectsWithTag("Peaton");
        for (int i = 0; i < encontrados.Length; i++)
        {
            GameObject go = encontrados[i];
            if (go == null)
            {
                continue;
            }

            PedestrianRef ped = new PedestrianRef
            {
                go = go,
                tr = go.transform,
                renderers = go.GetComponentsInChildren<Renderer>(true),
                colliders = go.GetComponentsInChildren<Collider>(true),
                animator = go.GetComponentInChildren<Animator>(true),
                rb = go.GetComponent<Rigidbody>(),
                pedestrianAI = go.GetComponent<PedestrianAI>(),
                sidewalkWalker = go.GetComponent<SidewalkPedestrianWalker>(),
                activo = true
            };

            peatones.Add(ped);
        }

        indiceCiclo = 0;
    }

    private bool CalcularEstadoObjetivo(PedestrianRef ped)
    {
        if (jugador == null)
        {
            return true;
        }

        float distanciaSqr = (ped.tr.position - jugador.position).sqrMagnitude;
        float siempreSqr = radioSiempreActivo * radioSiempreActivo;
        float desactivarSqr = distanciaDesactivar * distanciaDesactivar;
        float reactivarSqr = distanciaReactivar * distanciaReactivar;

        if (distanciaSqr <= siempreSqr)
        {
            return true;
        }

        if (ped.activo)
        {
            if (distanciaSqr > desactivarSqr)
            {
                return false;
            }
        }
        else if (distanciaSqr > reactivarSqr)
        {
            return false;
        }

        if (!requerirEnCamaraParaActivar)
        {
            return true;
        }

        return EstaEnVista(ped);
    }

    private bool EstaEnVista(PedestrianRef ped)
    {
        if (camaraObjetivo == null || frustumPlanes == null)
        {
            return true;
        }

        if (ped.renderers == null || ped.renderers.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < ped.renderers.Length; i++)
        {
            Renderer rendererActual = ped.renderers[i];
            if (rendererActual == null)
            {
                continue;
            }

            if (GeometryUtility.TestPlanesAABB(frustumPlanes, rendererActual.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private void AplicarEstado(PedestrianRef ped, bool activo)
    {
        ped.activo = activo;

        if (ped.pedestrianAI != null)
        {
            ped.pedestrianAI.enabled = activo;
        }

        if (ped.sidewalkWalker != null)
        {
            ped.sidewalkWalker.enabled = activo;
        }

        if (ped.animator != null)
        {
            ped.animator.enabled = activo;
        }

        if (ped.renderers != null)
        {
            for (int i = 0; i < ped.renderers.Length; i++)
            {
                if (ped.renderers[i] != null)
                {
                    ped.renderers[i].enabled = activo;
                }
            }
        }

        if (ped.colliders != null)
        {
            for (int i = 0; i < ped.colliders.Length; i++)
            {
                if (ped.colliders[i] != null)
                {
                    ped.colliders[i].enabled = activo;
                }
            }
        }

        if (ped.rb != null)
        {
            if (!activo)
            {
                ped.rb.isKinematic = false;  // ← Primero hazlo dinámico
                ped.rb.linearVelocity = Vector3.zero;
                ped.rb.angularVelocity = Vector3.zero;
                ped.rb.detectCollisions = false;
                ped.rb.isKinematic = true;   // ← Luego hazlo cinemático
            }
            else
            {
                ped.rb.isKinematic = false;
                ped.rb.detectCollisions = true;
            }
        }
    }
}
