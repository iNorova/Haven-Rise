using UnityEngine;
using System.Collections;

public class PlantedTreeGrower : MonoBehaviour
{
    [Header("Growth Settings")]
    [Tooltip("Time in seconds for the sprout to grow into a full tree")]
    public float growthTime = 60f; // Default: 60 seconds (1 minute)
    
    [Header("Tree Prefabs")]
    [Tooltip("Array of full-grown tree prefabs. One will be randomly selected when growth is complete")]
    public GameObject[] fullGrownTreePrefabs;
    
    [Header("Audio")]
    [Tooltip("Optional audio source for growth sound (will use PlayClipAtPoint if null)")]
    public AudioSource audioSource;
    [Tooltip("Sound effect to play when tree finishes growing")]
    public AudioClip growthSound;
    [Range(0f, 1f)]
    public float growthSoundVolume = 0.85f;
    
    [Header("Tree Planting System Reference")]
    [Tooltip("Reference to TreePlantingSystem for growth sound and settings")]
    public TreePlantingSystem treePlantingSystem;
    
    private bool hasGrown = false;
    private bool growthStarted = false;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    
    void Start()
    {
        // Store position for spawning full tree
        // Store the rotation but we'll spawn the tree upright, not using the sprout's rotation
        spawnPosition = transform.position;
        spawnRotation = Quaternion.identity; // Always spawn trees upright
        
        // Auto-start growth if values are already set (for prefabs that have values assigned)
        if (fullGrownTreePrefabs != null && fullGrownTreePrefabs.Length > 0 && growthTime > 0.1f)
        {
            StartGrowth();
        }
        else
        {
            // Try to get values from TreePlantingSystem if not set
            if (treePlantingSystem == null)
            {
                treePlantingSystem = FindObjectOfType<TreePlantingSystem>();
            }
            
            if (treePlantingSystem != null)
            {
                if (fullGrownTreePrefabs == null || fullGrownTreePrefabs.Length == 0)
                {
                    fullGrownTreePrefabs = treePlantingSystem.fullGrownTreePrefabs;
                }
                if (growthTime <= 0.1f)
                {
                    growthTime = treePlantingSystem.treeGrowthTime;
                }
                if (growthSound == null)
                {
                    growthSound = treePlantingSystem.seedGrowSfx;
                    growthSoundVolume = treePlantingSystem.seedGrowSfxVolume;
                }
            }
        }
    }
    
    // Public method to start growth (called after values are set by TreePlantingSystem)
    public void StartGrowth()
    {
        if (growthStarted)
        {
            Debug.LogWarning("PlantedTreeGrower: StartGrowth() called multiple times. Ignoring.");
            return; // Prevent starting multiple times
        }
        
        // Log current state for debugging
        Debug.Log($"PlantedTreeGrower: StartGrowth() called. Growth time: {growthTime}, Prefabs array: {(fullGrownTreePrefabs != null ? fullGrownTreePrefabs.Length.ToString() : "null")}");
        
        growthStarted = true;
        StartCoroutine(GrowTree());
    }
    
    IEnumerator GrowTree()
    {
        Debug.Log($"PlantedTreeGrower: Starting growth timer for {growthTime} seconds at position {spawnPosition}");
        
        // Verify we have prefabs before waiting
        if (fullGrownTreePrefabs == null || fullGrownTreePrefabs.Length == 0)
        {
            Debug.LogError($"PlantedTreeGrower: No fullGrownTreePrefabs assigned! Cannot grow tree. Check TreePlantingSystem settings.");
            yield break;
        }
        
        // Check for valid prefabs
        int validCount = 0;
        foreach (GameObject prefab in fullGrownTreePrefabs)
        {
            if (prefab != null) validCount++;
        }
        
        if (validCount == 0)
        {
            Debug.LogError($"PlantedTreeGrower: All prefabs in fullGrownTreePrefabs array are null! Cannot grow tree. Check TreePlantingSystem settings.");
            yield break;
        }
        
        Debug.Log($"PlantedTreeGrower: Found {validCount} valid tree prefab(s) out of {fullGrownTreePrefabs.Length} total.");
        
        // Wait for the growth time
        yield return new WaitForSeconds(growthTime);
        
        // Check if already grown (prevent double growth)
        if (hasGrown)
        {
            Debug.LogWarning("PlantedTreeGrower: Attempted to grow tree twice! Aborting.");
            yield break;
        }
        
        hasGrown = true;
        
        Debug.Log("PlantedTreeGrower: Growth complete! Spawning full-grown tree.");
        
        // Randomly select and spawn a full-grown tree from the prefab array
        GameObject selectedPrefab = GetRandomTreePrefab();
        
        if (selectedPrefab != null)
        {
            Debug.Log($"PlantedTreeGrower: Selected tree prefab '{selectedPrefab.name}' for spawning.");
            
            // Warn if selected prefab appears to be a rock/boulder instead of a tree
            string prefabName = selectedPrefab.name.ToLower();
            if (prefabName.Contains("rock") || prefabName.Contains("boulder") || prefabName.Contains("stone") || prefabName.Contains("cobble"))
            {
                Debug.LogError($"PlantedTreeGrower: ERROR! Selected prefab '{selectedPrefab.name}' is a ROCK/Boulder, not a TREE! Check your TreePlantingSystem's fullGrownTreePrefabs array in the Inspector!");
            }
            
            Debug.Log($"PlantedTreeGrower: Sprout scale: {transform.localScale}, Position: {spawnPosition}, Spawn rotation: {spawnRotation.eulerAngles}");
            
            // Spawn the tree upright (Quaternion.identity) regardless of sprout rotation
            // Trees should always grow upward, not sideways
            GameObject fullTree = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            
            if (fullTree != null)
            {
                // Use the prefab's original scale instead of the sprout's tiny scale
                // The sprout might have a very small scale (like 0.05) which would make the full tree invisible
                // Reset to Vector3.one so the tree uses its prefab's scale, or use the prefab's scale directly
                fullTree.transform.localScale = Vector3.one; // Use default scale
                
                // Ensure the tree is upright (identity rotation means no rotation - pointing up)
                fullTree.transform.rotation = Quaternion.identity;
                
                // Make sure the tree is active
                fullTree.SetActive(true);
                
                // Ensure the full tree has proper components (like DestroyableObject for cutting)
                // You might need to adjust this based on your tree prefab structure
                
                Debug.Log($"PlantedTreeGrower: Successfully spawned full-grown tree '{fullTree.name}' at {spawnPosition}.");
                Debug.Log($"PlantedTreeGrower: Tree scale: {fullTree.transform.localScale}, Active: {fullTree.activeSelf}, Tag: {fullTree.tag}");
            }
            else
            {
                Debug.LogError($"PlantedTreeGrower: Failed to instantiate tree prefab '{selectedPrefab.name}'!");
            }
        }
        else
        {
            Debug.LogError("PlantedTreeGrower: GetRandomTreePrefab() returned null! No valid prefabs in array. Check TreePlantingSystem settings.");
        }
        
        // Play growth sound
        PlayGrowthSound();
        
        // Destroy the sprout
        Debug.Log($"PlantedTreeGrower: Destroying sprout at {spawnPosition}");
        Destroy(gameObject);
    }
    
    void PlayGrowthSound()
    {
        if (growthSound == null)
        {
            // Try to get growth sound from TreePlantingSystem
            if (treePlantingSystem != null)
            {
                growthSound = treePlantingSystem.seedGrowSfx;
                growthSoundVolume = treePlantingSystem.seedGrowSfxVolume;
            }
        }
        
        if (growthSound == null) return;
        
        if (audioSource != null)
        {
            audioSource.PlayOneShot(growthSound, growthSoundVolume);
        }
        else
        {
            // Play at the position where the tree grew
            AudioSource.PlayClipAtPoint(growthSound, spawnPosition, growthSoundVolume);
        }
    }
    
    // Public method to check if tree has grown (useful for external systems)
    public bool HasGrown()
    {
        return hasGrown;
    }
    
    // Public method to get remaining growth time (useful for UI)
    public float GetRemainingGrowthTime()
    {
        return hasGrown ? 0f : growthTime;
    }
    
    // Helper method to randomly select a tree prefab from the array
    private GameObject GetRandomTreePrefab()
    {
        if (fullGrownTreePrefabs == null || fullGrownTreePrefabs.Length == 0)
        {
            return null;
        }
        
        // Filter out any null entries in the array
        System.Collections.Generic.List<GameObject> validPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject prefab in fullGrownTreePrefabs)
        {
            if (prefab != null)
            {
                validPrefabs.Add(prefab);
            }
        }
        
        if (validPrefabs.Count == 0)
        {
            return null;
        }
        
        // Randomly select from valid prefabs
        int randomIndex = Random.Range(0, validPrefabs.Count);
        return validPrefabs[randomIndex];
    }
}
