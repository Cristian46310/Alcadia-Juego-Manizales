using UnityEngine;

public class LanePath : MonoBehaviour
{
    public Transform[] puntos;
    public bool loop;
    public Color colorGizmo = Color.cyan;

    public int PointCount => puntos == null ? 0 : puntos.Length;

    public Vector3 GetPoint(int index)
    {
        if (PointCount == 0)
        {
            return transform.position;
        }

        index = Mathf.Clamp(index, 0, PointCount - 1);
        return puntos[index] != null ? puntos[index].position : transform.position;
    }

    public int GetIndiceWaypointMasCercano(Vector3 posicion)
    {
        if (PointCount == 0)
        {
            return 0;
        }

        Vector3 plano = Vector3.ProjectOnPlane(posicion, Vector3.up);
        int mejor = 0;
        float mejorDist = float.MaxValue;

        for (int i = 0; i < PointCount; i++)
        {
            float dist = Vector3.SqrMagnitude(Vector3.ProjectOnPlane(GetPoint(i), Vector3.up) - plano);
            if (dist < mejorDist)
            {
                mejorDist = dist;
                mejor = i;
            }
        }

        return mejor;
    }

    /// <summary>
    /// Waypoint hacia el que debe avanzar un coche colocado en posicion (sobre el trazado del carril).
    /// </summary>
    public int GetIndiceWaypointEnPosicion(Vector3 posicion)
    {
        if (PointCount <= 1)
        {
            return 0;
        }

        Vector3 p = Vector3.ProjectOnPlane(posicion, Vector3.up);
        int mejorSegmento = 0;
        float mejorDist = float.MaxValue;
        float mejorT = 0f;

        for (int i = 0; i < PointCount - 1; i++)
        {
            Vector3 a = Vector3.ProjectOnPlane(GetPoint(i), Vector3.up);
            Vector3 b = Vector3.ProjectOnPlane(GetPoint(i + 1), Vector3.up);
            Vector3 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f)
            {
                continue;
            }

            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / lenSq);
            Vector3 proyeccion = a + ab * t;
            float dist = Vector3.SqrMagnitude(p - proyeccion);
            if (dist < mejorDist)
            {
                mejorDist = dist;
                mejorSegmento = i;
                mejorT = t;
            }
        }

        if (mejorT > 0.55f)
        {
            return Mathf.Min(mejorSegmento + 1, PointCount - 1);
        }

        return mejorSegmento;
    }

    public Quaternion GetRotacionEnPosicion(Vector3 posicion)
    {
        if (PointCount < 2)
        {
            return transform.rotation;
        }

        int indice = GetIndiceWaypointEnPosicion(posicion);
        int siguiente = GetNextIndex(indice);
        Vector3 dir = Vector3.ProjectOnPlane(GetPoint(siguiente) - GetPoint(indice), Vector3.up);
        if (dir.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    /// <summary>
    /// Toma los hijos directos del carril como waypoints (orden de jerarquía).
    /// </summary>
    public void SincronizarPuntosDesdeHijos()
    {
        if (transform.childCount == 0)
        {
            return;
        }

        var lista = new System.Collections.Generic.List<Transform>(transform.childCount);
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name.StartsWith("PuntoTorre_"))
            {
                continue;
            }

            lista.Add(hijo);
        }

        if (lista.Count > 0)
        {
            puntos = lista.ToArray();
        }
    }

    public int GetNextIndex(int currentIndex)
    {
        if (PointCount <= 1)
        {
            return 0;
        }

        int nextIndex = currentIndex + 1;

        if (nextIndex >= PointCount)
        {
            return loop ? 0 : PointCount - 1;
        }

        return nextIndex;
    }

    /// <summary>
    /// Longitud en planta desde un waypoint hasta el final del carril (sin contar el cierre del loop).
    /// </summary>
    public float GetLongitudHaciaAdelante(int indiceInicio, int indiceFinInclusive = -1)
    {
        if (PointCount < 2)
        {
            return 0f;
        }

        int fin = indiceFinInclusive < 0 ? PointCount - 1 : Mathf.Clamp(indiceFinInclusive, 0, PointCount - 1);
        int inicio = Mathf.Clamp(indiceInicio, 0, PointCount - 1);
        if (fin <= inicio)
        {
            return 0f;
        }

        float longitud = 0f;
        for (int i = inicio; i < fin; i++)
        {
            longitud += DistanciaPlano(GetPoint(i), GetPoint(i + 1));
        }

        return longitud;
    }

    /// <summary>
    /// Posición y orientación a una distancia recorriendo el carril hacia adelante desde indiceInicio.
    /// </summary>
    public bool TrySampleHaciaAdelante(
        int indiceInicio,
        float distanciaMetros,
        out Vector3 posicion,
        out Quaternion rotacion,
        out int indiceWaypoint)
    {
        posicion = transform.position;
        rotacion = transform.rotation;
        indiceWaypoint = 0;

        if (PointCount == 0)
        {
            return false;
        }

        int inicio = Mathf.Clamp(indiceInicio, 0, PointCount - 1);
        indiceWaypoint = inicio;

        if (PointCount == 1 || distanciaMetros <= 0f)
        {
            posicion = GetPoint(inicio);
            rotacion = ObtenerRotacionHaciaSiguiente(inicio);
            return true;
        }

        float restante = distanciaMetros;
        for (int i = inicio; i < PointCount - 1; i++)
        {
            Vector3 a = GetPoint(i);
            Vector3 b = GetPoint(i + 1);
            float segmento = DistanciaPlano(a, b);
            if (segmento < 0.01f)
            {
                continue;
            }

            if (restante <= segmento)
            {
                float t = restante / segmento;
                posicion = Vector3.Lerp(a, b, t);
                indiceWaypoint = i;
                Vector3 dir = Vector3.ProjectOnPlane(b - a, Vector3.up);
                rotacion = dir.sqrMagnitude > 0.001f
                    ? Quaternion.LookRotation(dir.normalized, Vector3.up)
                    : transform.rotation;
                return true;
            }

            restante -= segmento;
            indiceWaypoint = i + 1;
        }

        int ultimo = PointCount - 1;
        posicion = GetPoint(ultimo);
        indiceWaypoint = Mathf.Max(inicio, ultimo - 1);
        rotacion = ObtenerRotacionHaciaSiguiente(indiceWaypoint);
        return true;
    }

    private Quaternion ObtenerRotacionHaciaSiguiente(int indice)
    {
        int siguiente = GetNextIndex(indice);
        Vector3 dir = Vector3.ProjectOnPlane(GetPoint(siguiente) - GetPoint(indice), Vector3.up);
        if (dir.sqrMagnitude < 0.001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private static float DistanciaPlano(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(
            Vector3.ProjectOnPlane(a, Vector3.up),
            Vector3.ProjectOnPlane(b, Vector3.up));
    }

    private void OnDrawGizmos()
    {
        if (PointCount == 0)
        {
            return;
        }

        Gizmos.color = colorGizmo;

        for (int i = 0; i < PointCount; i++)
        {
            if (puntos[i] == null)
            {
                continue;
            }

            Vector3 current = puntos[i].position;
            Gizmos.DrawSphere(current, 0.35f);

            int nextIndex = i + 1;
            bool shouldConnect = nextIndex < PointCount || loop;

            if (!shouldConnect)
            {
                continue;
            }

            Vector3 next = puntos[nextIndex % PointCount] != null
                ? puntos[nextIndex % PointCount].position
                : current;

            Gizmos.DrawLine(current, next);
        }
    }
}