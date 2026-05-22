using UnityEngine;

/// <summary>
/// Instancia peatones en cada Crosswalk detectado en la escena.
/// Asigna automáticamente start/end/crosswalk a cada PedestrianAI.
/// </summary>
public class PedestrianSpawner : MonoBehaviour
{
    public GameObject[] pedestrianPrefabs;
    [Tooltip("Si se asigna, este spawner solo instanciará peatones para ese Crosswalk específico")]
    public Crosswalk targetCrosswalk;
    public int groupColumns = 3;
    public int groupRows = 4;
    public float spacingWidth = 0.7f;
    public float spacingDepth = 0.5f;
    public float spawnJitter = 0.15f;
    public Transform parent;

    private void Start()
    {
        Crosswalk[] crosswalks;
        if (targetCrosswalk != null)
        {
            crosswalks = new Crosswalk[] { targetCrosswalk };
        }
        else
        {
            crosswalks = FindObjectsOfType<Crosswalk>();
        }

        if (crosswalks == null || crosswalks.Length == 0) return;
        if (pedestrianPrefabs == null || pedestrianPrefabs.Length == 0) return;

        if (parent == null) parent = transform;

        for (int i = 0; i < crosswalks.Length; i++)
        {
            Crosswalk cw = crosswalks[i];
            Vector3 basePosition = cw.startPoint != null ? cw.startPoint.position : cw.transform.position;
            Vector3 forward = cw.startPoint != null && cw.endPoint != null
                ? (cw.endPoint.position - cw.startPoint.position).normalized
                : cw.transform.forward;
            if (forward.sqrMagnitude < 0.001f) forward = cw.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float halfWidth = (groupColumns - 1) * spacingWidth * 0.5f;

            for (int row = 0; row < groupRows; row++)
            {
                for (int col = 0; col < groupColumns; col++)
                {
                    Vector3 offset = right * (col * spacingWidth - halfWidth);
                    offset += -forward * row * spacingDepth;
                    Vector3 jitter = new Vector3(Random.Range(-spawnJitter, spawnJitter), 0f, Random.Range(-spawnJitter, spawnJitter));
                    Vector3 spawnPosition = basePosition + offset + jitter;

                    GameObject prefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Length)];
                    GameObject p = Instantiate(prefab, spawnPosition, Quaternion.identity, parent);

                    PedestrianAI ai = p.GetComponent<PedestrianAI>();
                    if (ai != null)
                    {
                        ai.startPoint = cw.startPoint;
                        ai.endPoint = cw.endPoint;
                        ai.crosswalk = cw;
                    }
                }
            }
        }
    }
}
