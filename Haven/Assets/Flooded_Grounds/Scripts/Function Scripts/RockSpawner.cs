using UnityEngine;
using System.Collections.Generic;

public class RockSpawner : MonoBehaviour
{
    [Header("Rock Spawning Settings")]
    public List<GameObject> rockPrefabs = new List<GameObject>();  // List of rock prefabs to spawn (6 different assets)
    public int numberOfRocks = 100;               // Number of rocks to spawn
    public float minDistanceBetweenRocks = 1.5f;   // Minimum distance between spawned rocks
    public float spawnHeight = 0f;               // Height offset for spawning rocks
    public float rockRadius = 0.5f;                // Radius to check for structure collisions
    public float randomOffset = 0.8f;              // Random offset for natural look

    [Header("Spawn Area Settings")]
    public Vector2 spawnAreaSize = new Vector2(50f, 50f);  // Size of the spawn area (auto-calculated if autoCenter is enabled)
    public bool autoCenterOnTerrain = true;      // Automatically center spawner on terrain and set spawn area to terrain size
    public LayerMask groundLayer;                // Layer mask for the ground
    public LayerMask structureLayer;             // Layer mask for structures/buildings
    public LayerMask waterLayer;                 // Layer mask for water
    public Terrain targetTerrain;                // Reference to the terrain
    public float waterHeight = 0f;               // Height of the water plane
    public float waterCheckRadius = 0.3f;        // Radius to check for water
    public float waterCheckHeight = 0.5f;          // Height to check above water

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<Vector2> gridPositions = new List<Vector2>();
    private GameObject waterPlane;                // Reference to the water plane

    void Start()
    {
        Debug.Log("RockSpawner: Starting initialization...");
        
        // Check if terrain is assigned first (needed for auto-centering)
        if (targetTerrain == null)
        {
            Debug.LogError("RockSpawner: No terrain assigned! Please drag your terrain into the Target Terrain field in the inspector.");
            return;
        }

        // Auto-center spawner on terrain and set spawn area to terrain size
        if (autoCenterOnTerrain)
        {
            CenterSpawnerOnTerrain();
        }
        
        // Find water plane
        waterPlane = GameObject.FindGameObjectWithTag("Water");
        if (waterPlane == null)
        {
            Debug.LogWarning("RockSpawner: No water plane found with 'Water' tag. Make sure your water has the 'Water' tag.");
        }
        else
        {
            waterHeight = waterPlane.transform.position.y;
            Debug.Log($"RockSpawner: Found water plane at height {waterHeight}");
        }
        
        // Check if we have rock prefabs
        if (rockPrefabs == null || rockPrefabs.Count == 0)
        {
            Debug.LogError("RockSpawner: No rock prefabs assigned! Please add at least one rock prefab in the inspector.");
            return;
        }

        // Check if layers are set
        if (groundLayer.value == 0)
        {
            Debug.LogError("RockSpawner: Ground Layer is not set! Please set the Ground Layer in the inspector.");
            return;
        }

        if (structureLayer.value == 0)
        {
            Debug.LogWarning("RockSpawner: Structure Layer is not set. Rocks might spawn inside buildings!");
        }

        if (waterLayer.value == 0)
        {
            Debug.LogWarning("RockSpawner: Water Layer is not set. Rocks might spawn in water!");
        }

        Debug.Log($"RockSpawner: Initialization complete. Ready to spawn {numberOfRocks} rocks from {rockPrefabs.Count} different rock types.");
        Debug.Log($"RockSpawner: Spawn area size: {spawnAreaSize.x} x {spawnAreaSize.y}, centered at {transform.position}");
        InitializeGrid();
        SpawnRocks();
    }

    void CenterSpawnerOnTerrain()
    {
        if (targetTerrain == null)
        {
            Debug.LogWarning("RockSpawner: Cannot center on terrain - terrain not assigned!");
            return;
        }

        // Get terrain size
        Vector3 terrainSize = targetTerrain.terrainData.size;
        Vector3 terrainPosition = targetTerrain.transform.position;

        // Calculate terrain center
        Vector3 terrainCenter = terrainPosition + new Vector3(terrainSize.x * 0.5f, 0f, terrainSize.z * 0.5f);
        
        // Move spawner to terrain center (maintain current Y position)
        transform.position = new Vector3(terrainCenter.x, transform.position.y, terrainCenter.z);
        
        // Set spawn area size to match terrain size
        spawnAreaSize = new Vector2(terrainSize.x, terrainSize.z);
        
        Debug.Log($"RockSpawner: Auto-centered on terrain. Position: {transform.position}, Spawn area: {spawnAreaSize.x} x {spawnAreaSize.y}");
    }

    void InitializeGrid()
    {
        gridPositions.Clear();
        
        // Calculate grid size based on number of rocks and spawn area
        float gridSize = Mathf.Sqrt((spawnAreaSize.x * spawnAreaSize.y) / numberOfRocks);
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

    void SpawnRocks()
    {
        Debug.Log("RockSpawner: Starting rock spawning process...");
        int successfulSpawns = 0;
        int maxAttempts = gridPositions.Count;

        foreach (Vector2 gridPos in gridPositions)
        {
            if (successfulSpawns >= numberOfRocks) break;

            // Add random offset to grid position
            float randomOffsetX = Random.Range(-randomOffset, randomOffset);
            float randomOffsetZ = Random.Range(-randomOffset, randomOffset);
            Vector3 spawnPosition = new Vector3(
                gridPos.x + randomOffsetX + transform.position.x,
                0,
                gridPos.y + randomOffsetZ + transform.position.z
            );

            // Get the height at this position from the terrain
            spawnPosition.y = targetTerrain.SampleHeight(spawnPosition) + spawnHeight;

            // Check if position is valid and not in water
            if (IsValidPosition(spawnPosition) && !IsNearStructure(spawnPosition) && !IsInWater(spawnPosition))
            {
                // Get random rock prefab from the list (supports 6 different assets)
                GameObject randomRockPrefab = rockPrefabs[Random.Range(0, rockPrefabs.Count)];
                
                // Spawn the rock with random rotation
                float randomRotationY = Random.Range(0f, 360f);
                float randomRotationX = Random.Range(-15f, 15f); // Slight tilt for natural look
                float randomRotationZ = Random.Range(-15f, 15f);
                GameObject rock = Instantiate(randomRockPrefab, spawnPosition, 
                    Quaternion.Euler(randomRotationX, randomRotationY, randomRotationZ));
                rock.transform.parent = transform;
                spawnedPositions.Add(spawnPosition);
                successfulSpawns++;
                
                if (successfulSpawns % 20 == 0)
                {
                    Debug.Log($"RockSpawner: Successfully spawned {successfulSpawns} rocks so far...");
                }
            }
        }

        Debug.Log($"RockSpawner: Spawning complete. Successfully spawned {spawnedPositions.Count} rocks.");
        
        if (spawnedPositions.Count < numberOfRocks)
        {
            Debug.LogWarning($"RockSpawner: Could not spawn all requested rocks. Only spawned {spawnedPositions.Count} out of {numberOfRocks} rocks.");
        }
    }

    bool IsValidPosition(Vector3 position)
    {
        // Check if position is too close to other rocks
        foreach (Vector3 existingPosition in spawnedPositions)
        {
            if (Vector3.Distance(position, existingPosition) < minDistanceBetweenRocks)
            {
                return false;
            }
        }

        // Check if position is within terrain bounds
        Vector3 terrainPosition = position - targetTerrain.transform.position;
        if (terrainPosition.x < 0 || terrainPosition.x > targetTerrain.terrainData.size.x ||
            terrainPosition.z < 0 || terrainPosition.z > targetTerrain.terrainData.size.z)
        {
            return false;
        }

        return true;
    }

    bool IsNearStructure(Vector3 position)
    {
        if (structureLayer.value == 0) return false;

        // Check for any colliders in the structure layer within the rock radius
        Collider[] colliders = Physics.OverlapSphere(position, rockRadius, structureLayer);
        
        // If any colliders found, position is too close to a structure
        if (colliders.Length > 0)
        {
            return true;
        }

        // Additional check for structures above the spawn point
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f, structureLayer))
        {
            return true;
        }

        return false;
    }

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
            // Check at the base of the rock
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

        // Check if water plane exists and is above the position
        if (waterPlane != null)
        {
            // Get the water plane's bounds
            Bounds waterBounds = new Bounds(waterPlane.transform.position, waterPlane.transform.localScale);
            if (waterBounds.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    // Optional: Visualize spawn area and structure check radius in editor
    void OnDrawGizmosSelected()
    {
        // Draw spawn area
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));

        // Draw structure check radius for the last spawned position (if any)
        if (spawnedPositions.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnedPositions[spawnedPositions.Count - 1], rockRadius);
        }

        // Draw water height and check radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + Vector3.up * waterHeight, 
            new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
        
        if (spawnedPositions.Count > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnedPositions[spawnedPositions.Count - 1], waterCheckRadius);
            Gizmos.DrawWireSphere(spawnedPositions[spawnedPositions.Count - 1] + Vector3.up * waterCheckHeight, waterCheckRadius);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

