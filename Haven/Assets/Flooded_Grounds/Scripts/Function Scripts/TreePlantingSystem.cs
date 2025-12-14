using UnityEngine;

public class TreePlantingSystem : MonoBehaviour
{
    [Header("Tree Assets")]
    public GameObject soilPrefab;           // The soil asset that appears when tree is cut
    public GameObject plantedTreePrefab;    // The combined soil + sprout prefab
    [Tooltip("Array of full-grown tree prefabs. One will be randomly selected when a sprout finishes growing.")]
    public GameObject[] fullGrownTreePrefabs;  // Array of full-grown tree prefabs for random selection

    [Header("Growth Settings")]
    [Tooltip("Time in seconds for planted sprouts to grow into full trees")]
    public float treeGrowthTime = 60f; // Default: 60 seconds (1 minute)

    [Header("Spawn Adjustments")]
    public Vector3 soilSpawnOffset = Vector3.zero; // Offset for the soil's position
    public Vector3 soilScale = Vector3.one;     // Scale for the soil
    public Vector3 soilRotation = Vector3.zero; // Rotation for the soil (Euler angles)
    public Vector3 plantedTreeSpawnOffset = Vector3.zero; // Offset for the planted tree's position
    public Vector3 plantedTreeScale = Vector3.one; // Scale for the planted tree
    public Vector3 plantedTreeRotation = Vector3.zero; // Rotation for the planted tree (Euler angles)

    [Header("Ground Adjustment")]
    public LayerMask groundLayer; // Layer(s) that represent the ground/terrain
    public float groundOffset = 0.1f; // Adjust this value to place the tree correctly on the ground
    public float raycastHeight = 10f; // Height from which to cast the ray downwards

    [Header("Audio")]
    public AudioSource sfxSource; // Optional; if null we will use PlayClipAtPoint
    public AudioClip plantSeedSfx;   // Sound effect when seed is planted
    [Range(0f, 1f)] public float plantSeedSfxVolume = 0.85f;
    public AudioClip seedGrowSfx;   // Sound effect when seed grows into tree
    [Range(0f, 1f)] public float seedGrowSfxVolume = 0.85f;

    // Method called by DestroyableObject when a tree is cut
    public void OnTreeCut(Vector3 treePosition)
    {
        // Spawn soil at the tree's position with specified adjustments
        GameObject newSoil = Instantiate(soilPrefab, treePosition + soilSpawnOffset, Quaternion.identity);
        newSoil.transform.localScale = soilScale;
        newSoil.transform.localRotation = Quaternion.Euler(soilRotation);

        // Ensure the spawned soil has the "Soil" tag and is on the "Soil" layer
        newSoil.tag = "Soil";
        int soilLayerInt = LayerMask.NameToLayer("Soil");
        if (soilLayerInt == -1)
        {
            Debug.LogWarning("TreePlantingSystem: 'Soil' layer not found. Please create a 'Soil' layer in Unity's Layer Manager.");
        }
        else
        {
            newSoil.layer = soilLayerInt;
        }
        
        Debug.Log($"TreePlantingSystem: Spawned soil at {treePosition}. Tag: {newSoil.tag}, Layer: {LayerMask.LayerToName(newSoil.layer)}.");
    }

    // Method called by SeedInteraction when player tries to plant on soil
    public void PlantSeedOnSoil(GameObject soilGameObject)
    {
        Debug.Log("TreePlantingSystem: Planting seed on detected soil.");
        
        // Play planting sound effect
        PlayPlantSeedSfx();
        
        // Hide the original soil object
        soilGameObject.SetActive(false);

        // Determine the spawn position for the planted tree
        Vector3 spawnPosition = soilGameObject.transform.position + plantedTreeSpawnOffset;
        float targetY = spawnPosition.y;

        // Perform a raycast downwards to find the actual ground height
        RaycastHit hit;
        Vector3 rayOrigin = new Vector3(spawnPosition.x, spawnPosition.y + raycastHeight, spawnPosition.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastHeight * 2, groundLayer))
        {
            targetY = hit.point.y + groundOffset;
            Debug.Log($"TreePlantingSystem: Ground detected at Y={hit.point.y}. Placing tree at Y={targetY}.");
        }
        else
        {
            Debug.LogWarning("TreePlantingSystem: No ground detected for planting. Using original Y position.");
        }

        // Spawn the planted tree prefab (sprout) at the adjusted position
        GameObject newPlantedTree = Instantiate(plantedTreePrefab, new Vector3(spawnPosition.x, targetY, spawnPosition.z), Quaternion.identity);
        newPlantedTree.transform.localScale = plantedTreeScale;
        newPlantedTree.transform.localRotation = Quaternion.Euler(plantedTreeRotation);

        // Add PlantedTreeGrower component to handle growth over time
        PlantedTreeGrower grower = newPlantedTree.GetComponent<PlantedTreeGrower>();
        if (grower == null)
        {
            grower = newPlantedTree.AddComponent<PlantedTreeGrower>();
        }
        
        // Configure the grower component
        grower.fullGrownTreePrefabs = fullGrownTreePrefabs;
        grower.growthTime = treeGrowthTime;
        grower.treePlantingSystem = this;
        grower.growthSound = seedGrowSfx;
        grower.growthSoundVolume = seedGrowSfxVolume;
        
        // Validate that tree prefabs are assigned and warn about non-tree prefabs
        if (fullGrownTreePrefabs == null || fullGrownTreePrefabs.Length == 0)
        {
            Debug.LogError("TreePlantingSystem: fullGrownTreePrefabs array is empty or null! Trees will not grow properly. Please assign tree prefabs in the Inspector.");
        }
        else
        {
            int validCount = 0;
            int rockCount = 0;
            foreach (GameObject prefab in fullGrownTreePrefabs)
            {
                if (prefab != null)
                {
                    validCount++;
                    // Check if prefab name suggests it's a rock/boulder instead of a tree
                    string prefabName = prefab.name.ToLower();
                    if (prefabName.Contains("rock") || prefabName.Contains("boulder") || prefabName.Contains("stone") || prefabName.Contains("cobble"))
                    {
                        rockCount++;
                        Debug.LogWarning($"TreePlantingSystem: WARNING! Prefab '{prefab.name}' appears to be a ROCK/Boulder, not a TREE! Please assign only TREE prefabs to fullGrownTreePrefabs array!");
                    }
                }
            }
            if (validCount == 0)
            {
                Debug.LogError("TreePlantingSystem: All tree prefabs in fullGrownTreePrefabs array are null! Trees will not grow properly. Please assign valid prefabs in the Inspector.");
            }
            else
            {
                Debug.Log($"TreePlantingSystem: Configured grower with {validCount} valid prefab(s) out of {fullGrownTreePrefabs.Length} total.");
                if (rockCount > 0)
                {
                    Debug.LogError($"TreePlantingSystem: ERROR! Found {rockCount} ROCK/Boulder prefab(s) assigned to tree prefabs array! This will cause rocks to spawn instead of trees. Please fix this in the Inspector!");
                }
            }
        }
        
        // If the sprout prefab has an audio source, assign it
        if (sfxSource != null)
        {
            grower.audioSource = sfxSource;
        }
        
        // Start the growth process (this ensures values are set before growth starts)
        grower.StartGrowth();

        // Do NOT play growth sound here - it will play when the tree actually grows
        // The growth sound is now handled by PlantedTreeGrower when growth completes

        // Optionally, destroy the original soil object after a short delay or immediately
        // You might want to keep it if you plan to re-use it later, but for simple hide/replace, destroy is fine.
        Destroy(soilGameObject); // Destroy the soil GameObject after planting
        Debug.Log($"TreePlantingSystem: Soil replaced with PlantedTree (sprout). Growth time: {treeGrowthTime} seconds.");

        // Notify UIManager that a sprout seed has been planted
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnSproutSeedPlanted();
            Debug.Log("TreePlantingSystem: Notified UIManager of sprout seed planting.");
        }
    }

    // --- Audio helpers ---
    private void PlayPlantSeedSfx()
    {
        if (plantSeedSfx == null) return;
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(plantSeedSfx, plantSeedSfxVolume);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(plantSeedSfx, Camera.main.transform.position, plantSeedSfxVolume);
        }
    }

    private void PlaySeedGrowSfx(Vector3 position)
    {
        if (seedGrowSfx == null) return;
        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(seedGrowSfx, seedGrowSfxVolume);
        }
        else
        {
            // Play at the position where the tree was planted
            AudioSource.PlayClipAtPoint(seedGrowSfx, position, seedGrowSfxVolume);
        }
    }
} 