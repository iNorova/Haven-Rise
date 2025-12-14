using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Universal animal/creature spawner that can spawn any type of prefab (deer, ghouls, etc.).
/// Supports terrain-based spawning, water avoidance, house/structure spawning, and flexible spawn rules.
/// Each prefab can have its own spawn count.
/// </summary>
public class UniversalAnimalSpawner : MonoBehaviour
{
    [Header("Spawn Objects - Per Prefab Settings")]
    [Tooltip("List of prefabs to spawn. Each prefab can have its own spawn count.")]
    public List<GameObject> spawnPrefabsList = new List<GameObject>();
    
    [Tooltip("Spawn count for each prefab (must match Spawn Prefabs List size). Index 0 = count for prefab at index 0, etc.")]
    public List<int> spawnCountsList = new List<int>();
    
    [Header("Spawn Settings (Legacy - Use Spawn Configs Above)")]
    [Tooltip("DEPRECATED: Use Spawn Configs instead. Old prefab list (only used if Spawn Configs is empty).")]
    public List<GameObject> spawnPrefabs = new List<GameObject>();
    
    [Tooltip("DEPRECATED: Use Spawn Configs instead. Total spawn count (only used if Spawn Configs is empty).")]
    public int spawnCount = 10;
    
    [Tooltip("Spawn method: Radius uses circular area, Grid uses grid-based distribution for wider spread.")]
    public SpawnMethod spawnMethod = SpawnMethod.Radius;
    
    [Tooltip("Radius around spawner position to spawn animals (used when Spawn Method is Radius).")]
    public float spawnRadius = 100f;
    
    [Tooltip("Size of spawn area for grid-based spawning (used when Spawn Method is Grid). Wider area = more spread out.")]
    public Vector2 spawnAreaSize = new Vector2(200f, 200f);
    
    [Tooltip("Minimum distance between spawned animals to prevent clustering.")]
    public float minDistanceBetweenSpawns = 5f;
    
    [Tooltip("Height above ground to start raycast (should be high enough to clear terrain).")]
    public float raycastStartHeight = 100f;
    
    [Tooltip("Maximum distance to raycast downward to find ground.")]
    public float maxRaycastDistance = 200f;
    
    [Tooltip("Height offset for spawning (added to ground position).")]
    public float spawnHeight = 0f;

    [Header("Terrain Settings")]
    [Tooltip("Reference to the terrain (optional, but recommended for terrain-based spawning).")]
    public Terrain targetTerrain;
    
    [Tooltip("If true, uses terrain.SampleHeight for accurate terrain positioning. Requires Target Terrain.")]
    public bool useTerrainHeight = false;

    [Header("Spawn Conditions")]
    [Tooltip("Required tags for valid spawn locations (e.g., 'Grass', 'Ground'). Leave empty to spawn on any surface.")]
    public List<string> requiredTags = new List<string>();
    
    [Tooltip("Tags that count as valid terrain (e.g., 'Ground', 'Terrain').")]
    public List<string> validTerrainTags = new List<string>();
    
    [Tooltip("Tags that count as houses/structures (e.g., 'House', 'Building').")]
    public List<string> validHouseTags = new List<string>();
    
    [Tooltip("Layers that count as valid spawn surfaces (terrain/ground).")]
    public LayerMask groundLayer;
    
    [Tooltip("Layers that count as houses/structures (allows spawning in buildings).")]
    public LayerMask structureLayer;
    
    [Tooltip("If true, spawner will only spawn on surfaces with matching tags. If false, tags are ignored.")]
    public bool requireTagMatch = true;
    
    [Tooltip("Allow spawning on terrain (ground).")]
    public bool allowTerrainSpawn = true;
    
    [Tooltip("Allow spawning in/on houses (structures).")]
    public bool allowHouseSpawn = false;
    
    [Tooltip("Random offset for grid-based spawning (adds natural variation).")]
    public float randomOffset = 2f;

    [Header("Water Avoidance")]
    [Tooltip("Layer mask for water (prevents spawning in water).")]
    public LayerMask waterLayer;
    
    [Tooltip("Height of the water plane (auto-detected if water has 'Water' tag).")]
    public float waterHeight = 0f;
    
    [Tooltip("Radius to check for water colliders.")]
    public float waterCheckRadius = 1f;
    
    [Tooltip("Height to check above spawn position for water.")]
    public float waterCheckHeight = 1f;
    
    [Tooltip("Enable water avoidance checks.")]
    public bool avoidWater = true;

    [Header("Rotation")]
    [Tooltip("Random rotation range in degrees (0 = no rotation, 360 = full random rotation).")]
    [Range(0f, 360f)]
    public float randomRotationRange = 360f;

    [Header("Parenting")]
    [Tooltip("If true, spawned animals will be parented to this GameObject. If false, they spawn as root objects.")]
    public bool parentToSpawner = false;

    [Header("NavMesh Validation (for AI creatures like ghouls)")]
    [Tooltip("If true, only spawns on valid NavMesh positions. Use this for AI creatures that need NavMeshAgent.")]
    public bool requireNavMesh = false;
    
    [Tooltip("Maximum distance to search for valid NavMesh position if spawn point is not on NavMesh.")]
    public float navMeshSearchRadius = 5f;

    [Header("Visual Debug")]
    [Tooltip("Show spawn area gizmos in Scene view.")]
    public bool showGizmos = true;
    
    [Tooltip("Color of spawn area gizmo (Radius method).")]
    public Color radiusSpawnColor = new Color(0f, 1f, 0f, 0.3f); // Green with transparency
    
    [Tooltip("Color of spawn area gizmo (Grid method).")]
    public Color gridSpawnColor = new Color(0f, 1f, 0.5f, 0.3f); // Cyan-green with transparency
    
    [Tooltip("Color of water level gizmo.")]
    public Color waterLevelColor = new Color(0f, 0.5f, 1f, 0.3f); // Blue with transparency
    
    [Tooltip("Color of spawned position markers.")]
    public Color spawnedPositionColor = Color.yellow;
    
    [Tooltip("Show detailed grid lines in Scene view (Grid method only).")]
    public bool showGridLines = false;

    public enum SpawnMethod
    {
        Radius,  // Circular spawn area
        Grid     // Grid-based spawn area (better for wide distribution)
    }

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<Vector2> gridPositions = new List<Vector2>();
    private GameObject waterPlane;
    private bool hasSpawned = false; // Track if spawner has already spawned

    void Start()
    {
        // Check if spawner has already spawned (from saved game)
        CheckSavedSpawnState();

        // If marked as spawned, verify that spawned objects still exist
        if (hasSpawned)
        {
            // Check if spawned objects actually still exist in the scene
            bool objectsStillExist = CheckIfObjectsStillExist();
            
            if (!objectsStillExist)
            {
                // Objects were destroyed (e.g., scene reload), reset spawner
                Debug.Log($"UniversalAnimalSpawner on {gameObject.name}: Marked as spawned but objects don't exist. Resetting and respawning.");
                hasSpawned = false;
                // Continue to spawn below
            }
            else
            {
                Debug.Log($"[UniversalAnimalSpawner] {gameObject.name} has already spawned and objects exist. Skipping spawn.");
                return;
            }
        }

        // Find water plane if not manually set
        if (avoidWater && waterPlane == null)
        {
            waterPlane = GameObject.FindGameObjectWithTag("Water");
            if (waterPlane != null)
            {
                waterHeight = waterPlane.transform.position.y;
                Debug.Log($"UniversalAnimalSpawner: Found water plane at height {waterHeight}");
            }
        }

        // Initialize grid if using grid method
        if (spawnMethod == SpawnMethod.Grid)
        {
            InitializeGrid();
        }

        SpawnAnimals();
    }

    /// <summary>
    /// Check if spawner has already spawned based on saved state
    /// </summary>
    private void CheckSavedSpawnState()
    {
        // Check if there's a saved spawn state for this spawner
        // We'll use the spawner's instance ID or name as a key
        string spawnerKey = PauseMenuManager.SavedSpawnerPrefix + GetSpawnerIndex() + PauseMenuManager.SavedSpawnerHasSpawnedSuffix;
        if (PlayerPrefs.HasKey(spawnerKey))
        {
            hasSpawned = PlayerPrefs.GetInt(spawnerKey, 0) == 1;
        }
        else
        {
            // Check if spawner has children (spawned objects might be parented)
            // Or check if there are spawned objects in the scene
            hasSpawned = CheckIfObjectsAlreadySpawned();
        }
    }

    /// <summary>
    /// Check if objects have already been spawned by this spawner
    /// </summary>
    private bool CheckIfObjectsAlreadySpawned()
    {
        // Check if spawner has children (if parentToSpawner is true)
        if (parentToSpawner && transform.childCount > 0)
        {
            return true;
        }

        // Check if spawned positions list has items (if it was saved somehow)
        if (spawnedPositions != null && spawnedPositions.Count > 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if spawned objects still exist in the scene
    /// </summary>
    private bool CheckIfObjectsStillExist()
    {
        // Check if spawner has children (if parentToSpawner is true)
        if (parentToSpawner && transform.childCount > 0)
        {
            return true;
        }

        // Check spawned positions and verify objects exist at those positions
        if (spawnedPositions != null && spawnedPositions.Count > 0)
        {
            // Check a few random positions to see if objects still exist
            int checksToDo = Mathf.Min(3, spawnedPositions.Count);
            for (int i = 0; i < checksToDo; i++)
            {
                Vector3 checkPos = spawnedPositions[Random.Range(0, spawnedPositions.Count)];
                Collider[] colliders = Physics.OverlapSphere(checkPos, 2f);
                foreach (Collider col in colliders)
                {
                    // Check if any of the colliders match our spawn prefabs
                    foreach (GameObject prefab in spawnPrefabsList)
                    {
                        if (prefab != null && col.gameObject.name.Contains(prefab.name))
                        {
                            return true; // Found at least one spawned object
                        }
                    }
                    // Also check legacy prefabs list
                    foreach (GameObject prefab in spawnPrefabs)
                    {
                        if (prefab != null && col.gameObject.name.Contains(prefab.name))
                        {
                            return true; // Found at least one spawned object
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Get spawner index for save/load system
    /// </summary>
    private int GetSpawnerIndex()
    {
        // Get all spawners of this type and find our index
        UniversalAnimalSpawner[] allSpawners = FindObjectsOfType<UniversalAnimalSpawner>();
        for (int i = 0; i < allSpawners.Length; i++)
        {
            if (allSpawners[i] == this)
            {
                return i;
            }
        }
        return 0;
    }

    /// <summary>
    /// Set if spawner has spawned (for loading saved games)
    /// </summary>
    public void SetHasSpawned(bool spawned)
    {
        hasSpawned = spawned;
    }

    /// <summary>
    /// Check if spawner has spawned
    /// </summary>
    public bool GetHasSpawned()
    {
        return hasSpawned;
    }

    /// <summary>
    /// Main spawning method that attempts to spawn the configured number of animals.
    /// </summary>
    void SpawnAnimals()
    {
        // Build spawn list from prefabs and counts
        List<GameObject> prefabsToSpawn = new List<GameObject>();
        List<int> spawnCounts = new List<int>();
        int totalSpawnCount = 0;

        // Use new per-prefab system if available
        if (spawnPrefabsList != null && spawnPrefabsList.Count > 0 && spawnCountsList != null && spawnCountsList.Count > 0)
        {
            // Match prefabs with their spawn counts
            int maxIndex = Mathf.Min(spawnPrefabsList.Count, spawnCountsList.Count);
            for (int i = 0; i < maxIndex; i++)
            {
                if (spawnPrefabsList[i] != null && spawnCountsList[i] > 0)
                {
                    prefabsToSpawn.Add(spawnPrefabsList[i]);
                    spawnCounts.Add(spawnCountsList[i]);
                    totalSpawnCount += spawnCountsList[i];
                }
            }
        }
        // Fallback to legacy system
        else if (spawnPrefabs != null && spawnPrefabs.Count > 0)
        {
            foreach (var prefab in spawnPrefabs)
            {
                if (prefab != null)
                {
                    prefabsToSpawn.Add(prefab);
                    spawnCounts.Add(spawnCount / spawnPrefabs.Count); // Distribute evenly
                    totalSpawnCount += spawnCount / spawnPrefabs.Count;
                }
            }
        }

        // Validate
        if (prefabsToSpawn.Count == 0)
        {
            Debug.LogError($"UniversalAnimalSpawner on {gameObject.name}: No valid spawn prefabs assigned! Please add prefabs to Spawn Prefabs List and matching counts to Spawn Counts List.");
            return;
        }

        int spawned = 0;
        int maxAttempts = totalSpawnCount * (spawnMethod == SpawnMethod.Grid ? 2 : 20);
        int attempts = 0;

        Debug.Log($"UniversalAnimalSpawner: Starting to spawn {totalSpawnCount} total animals from {prefabsToSpawn.Count} prefab type(s) using {spawnMethod} method...");
        
        // Track spawn counts per prefab
        int[] spawnedPerPrefab = new int[prefabsToSpawn.Count];

        // Determine which positions to try based on spawn method
        List<Vector2> positionsToTry = new List<Vector2>();
        if (spawnMethod == SpawnMethod.Grid)
        {
            positionsToTry = new List<Vector2>(gridPositions);
        }
        else
        {
            // Generate random positions for radius method
            for (int i = 0; i < maxAttempts; i++)
            {
                positionsToTry.Add(new Vector2(
                    Random.Range(-spawnRadius, spawnRadius),
                    Random.Range(-spawnRadius, spawnRadius)
                ));
            }
        }

        foreach (Vector2 posOffset in positionsToTry)
        {
            if (spawned >= spawnCount) break;
            attempts++;

            Vector3 randomPos;
            if (spawnMethod == SpawnMethod.Grid)
            {
                // Grid-based: use grid position with random offset
                float randomOffsetX = Random.Range(-randomOffset, randomOffset);
                float randomOffsetZ = Random.Range(-randomOffset, randomOffset);
                randomPos = new Vector3(
                    posOffset.x + randomOffsetX + transform.position.x,
                    0,
                    posOffset.y + randomOffsetZ + transform.position.z
                );
                
                // Use terrain height if available
                if (useTerrainHeight && targetTerrain != null)
                {
                    randomPos.y = targetTerrain.SampleHeight(randomPos) + spawnHeight;
                }
                else
                {
                    randomPos.y = raycastStartHeight;
                }
            }
            else
            {
                // Radius-based: use offset from spawner
                randomPos = transform.position + new Vector3(
                    posOffset.x,
                    raycastStartHeight,
                    posOffset.y
                );
            }

            // Raycast down to find the ground and get collider info
            RaycastHit hit;
            bool hitFound = false;
            Vector3 spawnPosition = randomPos;

            // Always do a raycast to get collider information (needed for validation)
            LayerMask raycastLayers = groundLayer | structureLayer;
            if (raycastLayers.value == 0)
            {
                raycastLayers = Physics.DefaultRaycastLayers;
            }

            // Adjust raycast start position if using terrain height
            Vector3 raycastStart = randomPos;
            if (useTerrainHeight && targetTerrain != null && spawnMethod == SpawnMethod.Grid)
            {
                // Use terrain height for spawn position, but still raycast from slightly above for collider detection
                spawnPosition = new Vector3(randomPos.x, targetTerrain.SampleHeight(randomPos) + spawnHeight, randomPos.z);
                raycastStart = spawnPosition + Vector3.up * 5f; // Start raycast slightly above terrain
            }

            if (Physics.Raycast(raycastStart, Vector3.down, out hit, maxRaycastDistance, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                // If not using terrain height, use raycast hit point
                if (!useTerrainHeight || spawnMethod == SpawnMethod.Radius)
                {
                    spawnPosition = hit.point + Vector3.up * spawnHeight;
                }
                hitFound = true;
            }
            else
            {
                continue; // No ground found, skip this position
            }

            // Check if position is valid
            if (hitFound && IsValidSpawnPosition(spawnPosition, hit.collider))
            {
                // Check water avoidance
                if (avoidWater && IsInWater(spawnPosition))
                {
                    continue;
                }

                // Check if it's a valid spawn location (terrain or house)
                if (hitFound && !IsValidSpawnLocation(spawnPosition, hit.collider))
                {
                    continue;
                }

                // Check NavMesh validation if required
                if (requireNavMesh)
                {
                    Vector3 validNavMeshPosition = GetValidNavMeshPosition(spawnPosition);
                    if (validNavMeshPosition == Vector3.zero)
                    {
                        continue; // No valid NavMesh position found, skip this spawn
                    }
                    spawnPosition = validNavMeshPosition;
                }

                // Select prefab based on spawn counts (weighted selection)
                int prefabIndex = SelectPrefabIndex(prefabsToSpawn, spawnCounts, spawnedPerPrefab);
                if (prefabIndex == -1)
                {
                    continue; // All prefabs have reached their spawn count
                }

                GameObject selectedPrefab = prefabsToSpawn[prefabIndex];
                
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
                spawnedPerPrefab[prefabIndex]++;
                spawned++;
                
                // Mark spawner as having spawned (only once when first object spawns)
                if (!hasSpawned)
                {
                    hasSpawned = true;
                }
                
                if (spawned % 5 == 0)
                {
                    Debug.Log($"UniversalAnimalSpawner: Successfully spawned {spawned}/{totalSpawnCount} animals...");
                }
            }
        }

        // Log spawn summary
        Debug.Log($"UniversalAnimalSpawner: Spawning complete. Successfully spawned {spawned} out of {totalSpawnCount} requested animals (attempts: {attempts}).");
        
        // Log per-prefab counts
        for (int i = 0; i < prefabsToSpawn.Count; i++)
        {
            string prefabName = prefabsToSpawn[i] != null ? prefabsToSpawn[i].name : "Unknown";
            Debug.Log($"  - {prefabName}: {spawnedPerPrefab[i]}/{spawnCounts[i]} spawned");
        }
        
        if (spawned < totalSpawnCount)
        {
            Debug.LogWarning($"UniversalAnimalSpawner: Could not spawn all requested animals. Only spawned {spawned} out of {totalSpawnCount}. " +
                           $"Try increasing spawn radius/area, reducing min distance, or checking spawn conditions.");
        }
    }

    /// <summary>
    /// Selects which prefab to spawn based on remaining spawn counts (weighted selection).
    /// </summary>
    private int SelectPrefabIndex(List<GameObject> prefabs, List<int> spawnCounts, int[] spawnedPerPrefab)
    {
        // Build list of available prefabs (those that haven't reached their spawn count)
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (spawnedPerPrefab[i] < spawnCounts[i])
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return -1; // All prefabs have reached their spawn count
        }

        // Weighted random selection (prefabs with more remaining spawns are more likely)
        int totalRemaining = 0;
        foreach (int idx in availableIndices)
        {
            totalRemaining += spawnCounts[idx] - spawnedPerPrefab[idx];
        }

        if (totalRemaining <= 0)
        {
            return availableIndices[Random.Range(0, availableIndices.Count)];
        }

        int random = Random.Range(0, totalRemaining);
        int current = 0;
        foreach (int idx in availableIndices)
        {
            current += spawnCounts[idx] - spawnedPerPrefab[idx];
            if (random < current)
            {
                return idx;
            }
        }

        return availableIndices[0]; // Fallback
    }

    /// <summary>
    /// Initializes grid positions for grid-based spawning.
    /// </summary>
    void InitializeGrid()
    {
        gridPositions.Clear();
        
        // Calculate grid size based on number of animals and spawn area
        float gridSize = Mathf.Sqrt((spawnAreaSize.x * spawnAreaSize.y) / spawnCount);
        int gridX = Mathf.CeilToInt(spawnAreaSize.x / gridSize);
        int gridZ = Mathf.CeilToInt(spawnAreaSize.y / gridSize);

        // Create grid positions
        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                float posX = (x * gridSize) - (spawnAreaSize.x / 2) + (gridSize / 2);
                float posZ = (z * gridSize) - (spawnAreaSize.y / 2) + (gridSize / 2);
                gridPositions.Add(new Vector2(posX, posZ));
            }
        }

        // Shuffle grid positions for random distribution
        for (int i = 0; i < gridPositions.Count; i++)
        {
            Vector2 temp = gridPositions[i];
            int randomIndex = Random.Range(i, gridPositions.Count);
            gridPositions[i] = gridPositions[randomIndex];
            gridPositions[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Checks if a position is valid for spawning.
    /// </summary>
    bool IsValidSpawnPosition(Vector3 position, Collider hitCollider)
    {
        // Check terrain bounds if terrain is assigned
        if (targetTerrain != null)
        {
            Vector3 terrainPosition = position - targetTerrain.transform.position;
            if (terrainPosition.x < 0 || terrainPosition.x > targetTerrain.terrainData.size.x ||
                terrainPosition.z < 0 || terrainPosition.z > targetTerrain.terrainData.size.z)
            {
                return false;
            }
        }

        // Check tag requirements
        if (requireTagMatch && requiredTags != null && requiredTags.Count > 0 && hitCollider != null)
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
    /// Checks if spawn location is valid (terrain or house).
    /// </summary>
    bool IsValidSpawnLocation(Vector3 position, Collider hitCollider)
    {
        if (hitCollider == null) return false;

        bool isTerrain = false;
        bool isHouse = false;

        // Check layers
        if (groundLayer.value != 0 && ((1 << hitCollider.gameObject.layer) & groundLayer.value) != 0)
        {
            isTerrain = true;
        }

        if (structureLayer.value != 0 && ((1 << hitCollider.gameObject.layer) & structureLayer.value) != 0)
        {
            isHouse = true;
        }

        // Check tags
        if (validTerrainTags != null && validTerrainTags.Count > 0)
        {
            foreach (string tag in validTerrainTags)
            {
                if (!string.IsNullOrEmpty(tag) && hitCollider.CompareTag(tag))
                {
                    isTerrain = true;
                    break;
                }
            }
        }

        if (validHouseTags != null && validHouseTags.Count > 0)
        {
            foreach (string tag in validHouseTags)
            {
                if (!string.IsNullOrEmpty(tag) && hitCollider.CompareTag(tag))
                {
                    isHouse = true;
                    break;
                }
            }
        }

        // Return true if it matches allowed spawn types
        return (allowTerrainSpawn && isTerrain) || (allowHouseSpawn && isHouse);
    }

    /// <summary>
    /// Checks if position is in water.
    /// </summary>
    bool IsInWater(Vector3 position)
    {
        // Check if position is below water height
        if (position.y <= waterHeight)
        {
            return true;
        }

        // Check for water colliders in a sphere around the position
        if (waterLayer.value != 0)
        {
            // Check at the base
            Collider[] waterColliders = Physics.OverlapSphere(position, waterCheckRadius, waterLayer);
            if (waterColliders.Length > 0)
            {
                return true;
            }

            // Check slightly above the position
            Vector3 checkPosition = position + Vector3.up * waterCheckHeight;
            waterColliders = Physics.OverlapSphere(checkPosition, waterCheckRadius, waterLayer);
            if (waterColliders.Length > 0)
            {
                return true;
            }
        }

        // Check if water plane exists
        if (waterPlane != null)
        {
            Bounds waterBounds = new Bounds(waterPlane.transform.position, waterPlane.transform.localScale);
            if (waterBounds.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a valid NavMesh position near the given position. Returns Vector3.zero if none found.
    /// </summary>
    private Vector3 GetValidNavMeshPosition(Vector3 position)
    {
        UnityEngine.AI.NavMeshHit hit;
        // Try to find a valid NavMesh position within search radius
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out hit, navMeshSearchRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero; // No valid NavMesh position found
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
    /// Draw spawn area gizmos in editor for visualization.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Draw spawn area based on method
        if (spawnMethod == SpawnMethod.Radius)
        {
            // Draw radius circle (top view)
            Gizmos.color = radiusSpawnColor;
            DrawCircle(transform.position, spawnRadius, 32);
            
            // Draw radius indicator lines
            Gizmos.color = new Color(radiusSpawnColor.r, radiusSpawnColor.g, radiusSpawnColor.b, 1f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * spawnRadius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * spawnRadius);
            Gizmos.DrawLine(transform.position, transform.position - Vector3.forward * spawnRadius);
            Gizmos.DrawLine(transform.position, transform.position - Vector3.right * spawnRadius);
            
            // Draw center marker
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
        else
        {
            // Draw grid area (top view)
            Gizmos.color = gridSpawnColor;
            Vector3 center = transform.position;
            Vector3 size = new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y);
            Gizmos.DrawWireCube(center, size);
            
            // Draw grid lines if enabled
            if (showGridLines && targetTerrain != null)
            {
                Gizmos.color = new Color(gridSpawnColor.r, gridSpawnColor.g, gridSpawnColor.b, 0.5f);
                int gridLines = 10; // Number of grid lines to show
                float stepX = spawnAreaSize.x / gridLines;
                float stepZ = spawnAreaSize.y / gridLines;
                
                for (int i = 0; i <= gridLines; i++)
                {
                    float x = center.x - spawnAreaSize.x / 2 + stepX * i;
                    float z1 = center.z - spawnAreaSize.y / 2;
                    float z2 = center.z + spawnAreaSize.y / 2;
                    Gizmos.DrawLine(new Vector3(x, center.y, z1), new Vector3(x, center.y, z2));
                    
                    float z = center.z - spawnAreaSize.y / 2 + stepZ * i;
                    float x1 = center.x - spawnAreaSize.x / 2;
                    float x2 = center.x + spawnAreaSize.x / 2;
                    Gizmos.DrawLine(new Vector3(x1, center.y, z), new Vector3(x2, center.y, z));
                }
            }
            
            // Draw corner markers
            Gizmos.color = new Color(gridSpawnColor.r, gridSpawnColor.g, gridSpawnColor.b, 1f);
            float halfX = spawnAreaSize.x / 2;
            float halfZ = spawnAreaSize.y / 2;
            Gizmos.DrawWireSphere(center + new Vector3(-halfX, 0, -halfZ), 1f);
            Gizmos.DrawWireSphere(center + new Vector3(halfX, 0, -halfZ), 1f);
            Gizmos.DrawWireSphere(center + new Vector3(-halfX, 0, halfZ), 1f);
            Gizmos.DrawWireSphere(center + new Vector3(halfX, 0, halfZ), 1f);
        }
        
        // Draw water height level
        if (avoidWater && waterHeight != 0)
        {
            Gizmos.color = waterLevelColor;
            if (spawnMethod == SpawnMethod.Radius)
            {
                DrawCircle(transform.position + Vector3.up * waterHeight, spawnRadius, 32);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position + Vector3.up * waterHeight, 
                    new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
            }
        }
        
        // Draw spawn positions if any exist
        if (spawnedPositions != null && spawnedPositions.Count > 0)
        {
            Gizmos.color = spawnedPositionColor;
            foreach (Vector3 pos in spawnedPositions)
            {
                Gizmos.DrawWireSphere(pos, minDistanceBetweenSpawns * 0.5f);
                // Draw line to show it's spawned
                Gizmos.DrawLine(pos, pos + Vector3.up * 2f);
            }
        }
    }

    /// <summary>
    /// Helper method to draw a circle gizmo.
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    /// <summary>
    /// Draw gizmos even when not selected (lighter color).
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw a subtle indicator when not selected
        if (spawnMethod == SpawnMethod.Radius)
        {
            Gizmos.color = new Color(radiusSpawnColor.r, radiusSpawnColor.g, radiusSpawnColor.b, 0.1f);
            DrawCircle(transform.position, spawnRadius, 16);
        }
        else
        {
            Gizmos.color = new Color(gridSpawnColor.r, gridSpawnColor.g, gridSpawnColor.b, 0.1f);
            Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
        }
    }
}
