using UnityEngine;
using System.Collections.Generic;

public class TreeSpawner : MonoBehaviour
{
    [Header("Tree Prefabs")]
    [SerializeField] private GameObject[] treePrefabs;  // Different tree variants to spawn
    
    [Header("Spawn Settings")]
    [SerializeField] private int numberOfTrees = 100;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float minDistance = 5f;  // Minimum distance between trees
    
    [Header("Terrain Settings")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private float minHeight = 1f;    // Minimum terrain height to spawn trees
    [SerializeField] private float maxHeight = 50f;   // Maximum terrain height to spawn trees
    [SerializeField] private float maxSteepness = 30f;  // Maximum slope angle for tree spawning

    [Header("Seed Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private string seedString = "";

    private System.Random seededRandom;
    private List<Vector3> spawnedTreePositions = new List<Vector3>();

    void Start()
    {
        InitializeSeed();
        SpawnTrees();
    }

    void InitializeSeed()
    {
        int seed;
        if (useRandomSeed)
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
            seedString = seed.ToString();
        }
        else
        {
            seed = string.IsNullOrEmpty(seedString) ? 0 : seedString.GetHashCode();
        }
        seededRandom = new System.Random(seed);
        Debug.Log($"Initializing tree spawner with seed: {seedString}");
    }

    float GetSeededRandom()
    {
        return (float)seededRandom.NextDouble();
    }

    Vector3 GetRandomTerrainPosition()
    {
        float x = GetSeededRandom() * terrain.terrainData.size.x;
        float z = GetSeededRandom() * terrain.terrainData.size.z;
        float y = terrain.SampleHeight(new Vector3(x, 0, z));
        return new Vector3(x, y, z) + terrain.transform.position;
    }

    bool IsValidSpawnPoint(Vector3 position)
    {
        // Check height constraints
        if (position.y < minHeight || position.y > maxHeight)
            return false;

        // Check slope
        float slope = GetTerrainSlope(position);
        if (slope > maxSteepness)
            return false;

        // Check distance from other trees
        foreach (Vector3 treePos in spawnedTreePositions)
        {
            if (Vector3.Distance(position, treePos) < minDistance)
                return false;
        }

        return true;
    }

    float GetTerrainSlope(Vector3 worldPosition)
    {
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(
            (worldPosition.x - terrain.transform.position.x) / terrain.terrainData.size.x,
            (worldPosition.z - terrain.transform.position.z) / terrain.terrainData.size.z
        );
        return Vector3.Angle(normal, Vector3.up);
    }

    void SpawnTrees()
    {
        int attempts = 0;
        int maxAttempts = numberOfTrees * 10;  // Prevent infinite loops
        int treesSpawned = 0;

        while (treesSpawned < numberOfTrees && attempts < maxAttempts)
        {
            Vector3 position = GetRandomTerrainPosition();
            
            if (IsValidSpawnPoint(position))
            {
                // Select random tree prefab
                int treeIndex = seededRandom.Next(0, treePrefabs.Length);
                GameObject treePrefab = treePrefabs[treeIndex];

                // Create tree with random rotation and scale
                float rotationY = GetSeededRandom() * 360f;
                float scale = Mathf.Lerp(minScale, maxScale, GetSeededRandom());
                
                GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0, rotationY, 0));
                tree.transform.localScale = Vector3.one * scale;
                tree.transform.parent = transform;

                spawnedTreePositions.Add(position);
                treesSpawned++;
            }
            
            attempts++;
        }

        Debug.Log($"Spawned {treesSpawned} trees after {attempts} attempts");
    }

    // Optional: Method to save tree positions for persistence
    public Vector3[] GetTreePositions()
    {
        return spawnedTreePositions.ToArray();
    }
} 