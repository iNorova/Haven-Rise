using UnityEngine;
using System.Collections.Generic;

public class UniversalObjectSpawner : MonoBehaviour
{
    [Header("Object Spawning Settings")]
    public List<GameObject> objectPrefabs = new List<GameObject>();  // List of objects to spawn
    public int numberOfObjects = 30;               // Number of objects to spawn
    public float minDistanceBetweenObjects = 2f;   // Minimum distance between spawned objects
    public float spawnHeight = 0f;                 // Height offset for spawning objects
    public float objectRadius = 1f;                // Radius to check for structure collisions
    public float randomOffset = 1f;                // Random offset for natural look
    public float randomRotationRange = 360f;       // Maximum random rotation in degrees

    [Header("Spawn Area Settings")]
    public Vector2 spawnAreaSize = new Vector2(50f, 50f);  // Size of the spawn area
    public LayerMask groundLayer;                // Layer mask for the ground
    public LayerMask structureLayer;             // Layer mask for structures/buildings
    public LayerMask waterLayer;                 // Layer mask for water
    public Terrain targetTerrain;                // Reference to the terrain
    public float waterHeight = 0f;               // Height of the water plane
    public float waterCheckRadius = 0.5f;        // Radius to check for water
    public float waterCheckHeight = 1f;          // Height to check above water

    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<Vector2> gridPositions = new List<Vector2>();
    private GameObject waterPlane;                // Reference to the water plane

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("UniversalObjectSpawner: Starting initialization...");
        
        // Find water plane
        waterPlane = GameObject.FindGameObjectWithTag("Water");
        if (waterPlane == null)
        {
            Debug.LogWarning("UniversalObjectSpawner: No water plane found with 'Water' tag. Make sure your water has the 'Water' tag.");
        }
        else
        {
            waterHeight = waterPlane.transform.position.y;
            Debug.Log($"UniversalObjectSpawner: Found water plane at height {waterHeight}");
        }
        
        // Check if we have object prefabs
        if (objectPrefabs == null || objectPrefabs.Count == 0)
        {
            Debug.LogError("UniversalObjectSpawner: No object prefabs assigned! Please add at least one prefab in the inspector.");
            return;
        }

        // Check if terrain is assigned
        if (targetTerrain == null)
        {
            Debug.LogError("UniversalObjectSpawner: No terrain assigned! Please drag your terrain into the Target Terrain field in the inspector.");
            return;
        }

        // Check if layers are set
        if (groundLayer.value == 0)
        {
            Debug.LogError("UniversalObjectSpawner: Ground Layer is not set! Please set the Ground Layer in the inspector.");
            return;
        }

        if (structureLayer.value == 0)
        {
            Debug.LogWarning("UniversalObjectSpawner: Structure Layer is not set. Objects might spawn inside buildings!");
        }

        if (waterLayer.value == 0)
        {
            Debug.LogWarning("UniversalObjectSpawner: Water Layer is not set. Objects might spawn in water!");
        }

        Debug.Log($"UniversalObjectSpawner: Initialization complete. Ready to spawn {numberOfObjects} objects.");
        InitializeGrid();
        SpawnObjects();
    }

    void InitializeGrid()
    {
        gridPositions.Clear();
        
        // Calculate grid size based on number of objects and spawn area
        float gridSize = Mathf.Sqrt((spawnAreaSize.x * spawnAreaSize.y) / numberOfObjects);
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

    void SpawnObjects()
    {
        Debug.Log("UniversalObjectSpawner: Starting object spawning process...");
        int successfulSpawns = 0;
        int maxAttempts = gridPositions.Count;

        foreach (Vector2 gridPos in gridPositions)
        {
            if (successfulSpawns >= numberOfObjects) break;

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
                // Get random object prefab
                GameObject randomPrefab = objectPrefabs[Random.Range(0, objectPrefabs.Count)];
                
                // Calculate random rotation
                float randomRotation = Random.Range(0f, randomRotationRange);

                // Spawn the object with original scale and components
                GameObject spawnedObject = Instantiate(randomPrefab, spawnPosition, 
                    Quaternion.Euler(0, randomRotation, 0));
                spawnedObject.transform.parent = transform;
                spawnedPositions.Add(spawnPosition);
                successfulSpawns++;
                
                if (successfulSpawns % 10 == 0)
                {
                    Debug.Log($"UniversalObjectSpawner: Successfully spawned {successfulSpawns} objects so far...");
                }
            }
        }

        Debug.Log($"UniversalObjectSpawner: Spawning complete. Successfully spawned {spawnedPositions.Count} objects.");
        
        if (spawnedPositions.Count < numberOfObjects)
        {
            Debug.LogWarning($"UniversalObjectSpawner: Could not spawn all requested objects. Only spawned {spawnedPositions.Count} out of {numberOfObjects} objects.");
        }
    }

    bool IsValidPosition(Vector3 position)
    {
        // Check if position is too close to other objects
        foreach (Vector3 existingPosition in spawnedPositions)
        {
            if (Vector3.Distance(position, existingPosition) < minDistanceBetweenObjects)
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

        // Check for any colliders in the structure layer within the object radius
        Collider[] colliders = Physics.OverlapSphere(position, objectRadius, structureLayer);
        
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
            // Check at the base of the object
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));

        // Draw structure check radius for the last spawned position (if any)
        if (spawnedPositions.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnedPositions[spawnedPositions.Count - 1], objectRadius);
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
