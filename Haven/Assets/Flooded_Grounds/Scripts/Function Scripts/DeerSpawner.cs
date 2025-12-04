using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Universal animal/creature spawner that can spawn any type of prefab.
/// Supports multiple prefab types, configurable spawn tags/layers, and flexible spawning rules.
/// </summary>
public class UniversalAnimalSpawner : MonoBehaviour
{
    [Header("Spawn Objects")]
    [Tooltip("List of animal/creature prefabs to spawn. If multiple prefabs are provided, spawner will randomly select from them.")]
    public List<GameObject> spawnPrefabs = new List<GameObject>();
    
    [Header("Spawn Settings")]
    [Tooltip("Number of animals/creatures to spawn.")]
    public int spawnCount = 10;
    
    [Tooltip("Radius around spawner position to spawn animals.")]
    public float spawnRadius = 100f;
    
    [Tooltip("Minimum distance between spawned animals to prevent clustering.")]
    public float minDistanceBetweenSpawns = 5f;
    
    [Tooltip("Height above ground to start raycast (should be high enough to clear terrain).")]
    public float raycastStartHeight = 100f;
    
    [Tooltip("Maximum distance to raycast downward to find ground.")]
    public float maxRaycastDistance = 200f;

    [Header("Spawn Conditions")]
    [Tooltip("Required tags for valid spawn locations (e.g., 'Grass', 'Ground'). Leave empty to spawn on any surface.")]
    public List<string> requiredTags = new List<string>();
    
    [Tooltip("Layers that count as valid spawn surfaces. Leave empty to use default raycast layers.")]
    public LayerMask spawnSurfaceLayers = Physics.DefaultRaycastLayers;
    
    [Tooltip("If true, spawner will only spawn on surfaces with matching tags. If false, tags are ignored.")]
    public bool requireTagMatch = true;

    [Header("Rotation")]
    [Tooltip("Random rotation range in degrees (0 = no rotation, 360 = full random rotation).")]
    [Range(0f, 360f)]
    public float randomRotationRange = 360f;

    [Header("Parenting")]
    [Tooltip("If true, spawned animals will be parented to this GameObject. If false, they spawn as root objects.")]
    public bool parentToSpawner = false;

    private List<Vector3> spawnedPositions = new List<Vector3>();

    void Start()
    {
        SpawnAnimals();
    }

    /// <summary>
    /// Main spawning method that attempts to spawn the configured number of animals.
    /// </summary>
    void SpawnAnimals()
    {
        // Validate prefabs
        if (spawnPrefabs == null || spawnPrefabs.Count == 0)
        {
            Debug.LogError($"UniversalAnimalSpawner on {gameObject.name}: No spawn prefabs assigned! Please add at least one prefab to the Spawn Prefabs list.");
            return;
        }

        // Remove null prefabs from list
        spawnPrefabs.RemoveAll(prefab => prefab == null);
        if (spawnPrefabs.Count == 0)
        {
            Debug.LogError($"UniversalAnimalSpawner on {gameObject.name}: All spawn prefabs are null! Please assign valid prefabs.");
            return;
        }

        int spawned = 0;
        int maxAttempts = spawnCount * 20; // Increased attempts for better success rate
        int attempts = 0;

        Debug.Log($"UniversalAnimalSpawner: Starting to spawn {spawnCount} animals from {spawnPrefabs.Count} prefab type(s)...");

        while (spawned < spawnCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Generate random position within spawn radius
            Vector3 randomPos = transform.position + new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                raycastStartHeight,
                Random.Range(-spawnRadius, spawnRadius)
            );

            // Raycast down to find the ground
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, maxRaycastDistance, spawnSurfaceLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 spawnPosition = hit.point;

                // Check if position is valid
                if (IsValidSpawnPosition(spawnPosition, hit.collider))
                {
                    // Select random prefab from list
                    GameObject selectedPrefab = spawnPrefabs[Random.Range(0, spawnPrefabs.Count)];
                    
                    // Calculate random rotation
                    float randomRotation = Random.Range(0f, randomRotationRange);
                    Quaternion spawnRotation = Quaternion.Euler(0, randomRotation, 0);

                    // Spawn the animal
                    GameObject spawnedAnimal = Instantiate(selectedPrefab, spawnPosition, spawnRotation);
                    
                    // Parent to spawner if configured
                    if (parentToSpawner)
                    {
                        spawnedAnimal.transform.SetParent(transform);
                    }

                    spawnedPositions.Add(spawnPosition);
                    spawned++;
                    
                    if (spawned % 5 == 0)
                    {
                        Debug.Log($"UniversalAnimalSpawner: Successfully spawned {spawned}/{spawnCount} animals...");
                    }
                }
            }
        }

        Debug.Log($"UniversalAnimalSpawner: Spawning complete. Successfully spawned {spawned} out of {spawnCount} requested animals (attempts: {attempts}).");
        
        if (spawned < spawnCount)
        {
            Debug.LogWarning($"UniversalAnimalSpawner: Could not spawn all requested animals. Only spawned {spawned} out of {spawnCount}. " +
                           $"Try increasing spawn radius, reducing min distance, or checking spawn conditions.");
        }
    }

    /// <summary>
    /// Checks if a position is valid for spawning.
    /// </summary>
    bool IsValidSpawnPosition(Vector3 position, Collider hitCollider)
    {
        // Check tag requirements
        if (requireTagMatch && requiredTags != null && requiredTags.Count > 0)
        {
            bool tagMatches = false;
            foreach (string tag in requiredTags)
            {
                if (!string.IsNullOrEmpty(tag) && hitCollider.CompareTag(tag))
                {
                    tagMatches = true;
                    break;
                }
            }
            if (!tagMatches)
            {
                return false;
            }
        }

        // Check minimum distance from other spawned animals
        foreach (Vector3 existingPosition in spawnedPositions)
        {
            if (Vector3.Distance(position, existingPosition) < minDistanceBetweenSpawns)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Manually trigger spawning (useful for respawning or runtime spawning).
    /// </summary>
    public void TriggerSpawn()
    {
        spawnedPositions.Clear();
        SpawnAnimals();
    }

    /// <summary>
    /// Clear all spawned animals (useful for cleanup or respawning).
    /// </summary>
    public void ClearSpawnedAnimals()
    {
        spawnedPositions.Clear();
        
        // Destroy all children if parenting is enabled
        if (parentToSpawner)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Draw spawn radius gizmo in editor for visualization.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
        
        // Draw spawn positions if any exist
        if (spawnedPositions != null && spawnedPositions.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in spawnedPositions)
            {
                Gizmos.DrawWireSphere(pos, minDistanceBetweenSpawns * 0.5f);
            }
        }
    }
}
