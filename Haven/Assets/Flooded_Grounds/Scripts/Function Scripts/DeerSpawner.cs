using UnityEngine;

public class DeerSpawner : MonoBehaviour
{
    public GameObject deerPrefab;
    public int deerCount = 10;
    public float spawnRadius = 100f;

    void Start()
    {
        SpawnDeer();
    }

    void SpawnDeer()
    {
        int spawned = 0;
        int attempts = 0;
        while (spawned < deerCount && attempts < deerCount * 10)
        {
            attempts++;
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                100f, // Start high above the terrain
                Random.Range(-spawnRadius, spawnRadius)
            );

            // Raycast down to find the ground
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 200f))
            {
                // Check if the hit object is tagged as Grass
                if (hit.collider.CompareTag("Grass"))
                {
                    Instantiate(deerPrefab, hit.point, Quaternion.identity);
                    spawned++;
                }
            }
        }
    }
}
