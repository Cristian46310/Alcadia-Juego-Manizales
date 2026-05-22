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