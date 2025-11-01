using UnityEngine;

public class AnimalDropRateManager : MonoBehaviour
{
    [Header("Drop Item Settings")]
    [Tooltip("The item prefab that will drop")]
    public GameObject itemPrefab;
    
    [Header("Drop Chance")]
    [Tooltip("Percentage chance (0-100) for the item to drop at all")]
    [Range(0f, 100f)]
    public float dropChance = 100f;
    
    [Header("Quantity Settings")]
    [Tooltip("Base quantity to drop (if double chance fails)")]
    public int baseQuantity = 1;
    
    [Tooltip("Percentage chance (0-100) to drop double the base quantity")]
    [Range(0f, 100f)]
    public float doubleDropChance = 0f;
    
    [Header("Drop Physics")]
    [Tooltip("How far items scatter from the drop point")]
    public float scatterRadius = 1.5f;
    
    [Tooltip("Force applied to dropped items")]
    public float dropForce = 3f;
    
    /// <summary>
    /// Spawns the item based on drop chance and double drop chance
    /// </summary>
    public void SpawnDrop()
    {
        Debug.Log($"AnimalDropRateManager.SpawnDrop() called on {gameObject.name}");
        
        if (itemPrefab == null)
        {
            Debug.LogError($"AnimalDropRateManager on {gameObject.name}: No item prefab assigned! Cannot spawn drops.");
            return;
        }
        
        Debug.Log($"AnimalDropRateManager: Item prefab = {itemPrefab.name}, Drop Chance = {dropChance}%, Base Quantity = {baseQuantity}");
        
        // Check if item should drop at all
        float dropRoll = Random.Range(0f, 100f);
        Debug.Log($"AnimalDropRateManager: Drop roll = {dropRoll:F2}% (needs <= {dropChance:F2}%)");
        
        if (dropRoll > dropChance)
        {
            Debug.Log($"AnimalDropRateManager: Item did not drop (rolled {dropRoll:F2}%, needed <= {dropChance:F2}%)");
            return;
        }
        
        // Determine quantity (check for double drop)
        int quantity = baseQuantity;
        float doubleRoll = Random.Range(0f, 100f);
        if (doubleRoll <= doubleDropChance)
        {
            quantity = baseQuantity * 2;
            Debug.Log($"AnimalDropRateManager: Double drop triggered! (rolled {doubleRoll:F2}% <= {doubleDropChance:F2}%)");
        }
        
        Debug.Log($"AnimalDropRateManager: Dropping {quantity} {itemPrefab.name} (drop roll: {dropRoll:F2}% <= {dropChance:F2}%, double roll: {doubleRoll:F2}%)");
        
        // Spawn the items
        Vector3 spawnPosition = transform.position;
        Debug.Log($"AnimalDropRateManager: Spawn position = {spawnPosition}");
        
        for (int i = 0; i < quantity; i++)
        {
            SpawnSingleItem(spawnPosition, i);
        }
        
        Debug.Log($"AnimalDropRateManager: Finished spawning {quantity} items");
    }
    
    private void SpawnSingleItem(Vector3 centerPosition, int index)
    {
        // Calculate spawn position with scatter
        Vector3 randomOffset = Random.insideUnitSphere * scatterRadius;
        randomOffset.y = Mathf.Max(randomOffset.y, 0.2f); // Ensure items spawn slightly above ground
        Vector3 spawnPos = centerPosition + randomOffset;
        
        Debug.Log($"AnimalDropRateManager: Attempting to instantiate {itemPrefab.name} at {spawnPos}");
        
        // Instantiate the item
        GameObject droppedItem = Instantiate(itemPrefab, spawnPos, Random.rotation);
        
        if (droppedItem == null)
        {
            Debug.LogError($"AnimalDropRateManager: Failed to instantiate {itemPrefab.name}!");
            return;
        }
        
        Debug.Log($"AnimalDropRateManager: Successfully instantiated {droppedItem.name} (Instance ID: {droppedItem.GetInstanceID()})");
        
        // Ensure the item has a Rigidbody and is set up correctly for physics
        Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = droppedItem.AddComponent<Rigidbody>();
            Debug.Log($"AnimalDropRateManager: Added Rigidbody component to {droppedItem.name}");
        }
        else
        {
            Debug.Log($"AnimalDropRateManager: Found existing Rigidbody on {droppedItem.name}");
        }
        
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Ensure collider is enabled
        Collider col = droppedItem.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
            Debug.Log($"AnimalDropRateManager: Enabled collider on {droppedItem.name}");
        }
        else
        {
            Debug.LogWarning($"AnimalDropRateManager: No collider found on {droppedItem.name}");
        }
        
        // Add force to scatter the item
        Vector3 forceDirection = randomOffset.normalized + Vector3.up * 0.5f;
        rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
        
        // Add random rotation
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
        
        // Ensure the item is active
        droppedItem.SetActive(true);
        
        Debug.Log($"AnimalDropRateManager: Spawned {itemPrefab.name} #{index + 1} at position {spawnPos}, active: {droppedItem.activeSelf}, activeInHierarchy: {droppedItem.activeInHierarchy}");
    }
    
    // Visualize scatter radius in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, scatterRadius);
    }
}

