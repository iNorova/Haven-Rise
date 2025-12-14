using UnityEngine;
using UnityEngine.Events;

public class DestroyableObject : MonoBehaviour
{
    private const string DESTROYABLE_TAG = "Destroyable";
    private const int DESTROYABLE_LAYER = 8; // You can change this to any available layer number

    [Header("Object Properties")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private GameObject destroyedPrefab;  // Optional: prefab to spawn when object is destroyed
    [SerializeField] private GameObject[] dropPrefabs;   // Array of possible items that can drop
    [SerializeField] private int minDrops = 3;
    [SerializeField] private int maxDrops = 6;
    [SerializeField] private float dropScatterRadius = 2f;  // How far the drops scatter
    [SerializeField] private float dropForce = 5f;          // Force applied to dropped items
    [SerializeField] private bool isTree = false;           // Flag to identify if this is a tree
    public bool IsTree { get { return isTree; } } // Public getter for isTree

    [Header("Sprout Seed Drops")]
    [SerializeField] private bool canDropSproutSeed = false;
    [SerializeField] private GameObject sproutSeedPrefab;
    [SerializeField] private int minSproutSeedDrops = 1;
    [SerializeField] private int maxSproutSeedDrops = 1;
    [SerializeField] private Vector3 sproutSeedDropScale = Vector3.one;

    [Header("Effects")]
    [SerializeField] private ParticleSystem hitEffect;    // Optional: particle effect when object is hit
    [SerializeField] private AudioClip hitSound;          // Optional: sound when object is hit
    [SerializeField] private AudioClip destroySound;      // Optional: sound when object is destroyed

    public UnityEvent onObjectDestroyed = new UnityEvent();

    private AudioSource audioSource;
    private bool isDestroyed = false;

    private TreePlantingSystem treePlantingSystem;

    void Awake()
    {
        // Set the tag and layer automatically
        gameObject.tag = DESTROYABLE_TAG;
        gameObject.layer = DESTROYABLE_LAYER;
        
        Debug.Log($"DestroyableObject initialized: {gameObject.name} on layer {LayerMask.LayerToName(gameObject.layer)}");
    }

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = gameObject.AddComponent<AudioSource>();
        Debug.Log($"DestroyableObject {gameObject.name} health set to {currentHealth}");

        // Initialize TreePlantingSystem reference
        treePlantingSystem = FindObjectOfType<TreePlantingSystem>();
        if (treePlantingSystem == null)
        {
            Debug.LogWarning("DestroyableObject: TreePlantingSystem not found in scene. Tree planting features will not work.");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed)
        {
            Debug.Log($"Object {gameObject.name} is already destroyed");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"Object {gameObject.name} took {damage} damage. Current health: {currentHealth}");

        // Play hit effects
        if (hitEffect != null)
        {
            hitEffect.Play();
            Debug.Log("Hit effect played");
        }

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
            Debug.Log("Hit sound played");
        }

        // Check if object should be destroyed
        if (currentHealth <= 0)
        {
            Debug.Log($"Object {gameObject.name} health reached 0, destroying...");
            DestroyObject();
        }
    }

    void DestroyObject()
    {
        if (isDestroyed)
        {
            Debug.Log($"Object {gameObject.name} is already destroyed");
            return;
        }
        
        isDestroyed = true;
        Debug.Log($"Destroying object: {gameObject.name}");

        // If this is a tree, notify TreePlantingSystem to spawn soil and sproutseed
        if (isTree)
        {
            if (treePlantingSystem != null)
            {
                treePlantingSystem.OnTreeCut(transform.position);
                Debug.Log("DestroyableObject: Notified TreePlantingSystem to spawn soil.");
            }
            else
            {
                Debug.LogWarning("DestroyableObject: TreePlantingSystem is null, cannot spawn soil.");
            }

            // Spawn sprout seeds directly from DestroyableObject if enabled
            if (canDropSproutSeed)
            {
                SpawnSproutSeedDrops();
            }

            // Notify UIManager to increase temperature when tree is cut
            if (UIManager.Instance != null)
            {
                UIManager.Instance.StartTemperatureIncrease();
                Debug.Log("Tree destroyed - increasing temperature");
            }
        }

        // Play destroy sound
        if (destroySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destroySound);
            Debug.Log("Destroy sound played");
        }

        // Spawn drops from AnimalDropRateManager if present (for special drop rates like leather)
        AnimalDropRateManager[] dropManagers = GetComponents<AnimalDropRateManager>();
        if (dropManagers == null || dropManagers.Length == 0)
        {
            // Try to find in children in case it's attached to a child object
            dropManagers = GetComponentsInChildren<AnimalDropRateManager>();
        }
        
        if (dropManagers != null && dropManagers.Length > 0)
        {
            // Spawn drops from AnimalDropRateManager system
            Debug.Log($"DestroyableObject: Found {dropManagers.Length} AnimalDropRateManager component(s), spawning drop rate items...");
            foreach (AnimalDropRateManager dropManager in dropManagers)
            {
                if (dropManager != null)
                {
                    dropManager.SpawnDrop();
                }
            }
        }
        
        // Also spawn legacy drops (for items like meat that use the old system)
        SpawnDrops();

        // Spawn destroyed prefab if we have one
        if (destroyedPrefab != null)
        {
            Instantiate(destroyedPrefab, transform.position, transform.rotation);
            Debug.Log("Destroyed prefab spawned");
        }

        // Trigger the event
        onObjectDestroyed.Invoke();
        Debug.Log("Destroy event invoked");

        // Destroy the object immediately
        Destroy(gameObject);
    }

    void SpawnDrops()
    {
        // Check if we should spawn drops at all
        if (minDrops <= 0 && maxDrops <= 0)
        {
            Debug.Log($"DestroyableObject: Min/Max drops are both 0 or less, skipping drop spawn.");
            return;
        }

        // Validate drop prefabs array
        if (dropPrefabs == null || dropPrefabs.Length == 0)
        {
            Debug.LogError($"DestroyableObject: No drop prefabs assigned for {gameObject.name}! Min drops: {minDrops}, Max drops: {maxDrops}. Please assign drop prefabs in the Inspector.");
            return;
        }

        // Filter out null prefabs
        System.Collections.Generic.List<GameObject> validPrefabs = new System.Collections.Generic.List<GameObject>();
        foreach (GameObject prefab in dropPrefabs)
        {
            if (prefab != null)
            {
                validPrefabs.Add(prefab);
            }
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogError($"DestroyableObject: All drop prefabs are null for {gameObject.name}! Cannot spawn drops.");
            return;
        }

        // Calculate drop count (ensure it's at least minDrops)
        int dropCount = Random.Range(minDrops, maxDrops + 1);
        
        // Safety check: ensure we spawn at least minDrops if minDrops > 0
        if (dropCount < minDrops && minDrops > 0)
        {
            Debug.LogWarning($"DestroyableObject: Random drop count ({dropCount}) was less than minDrops ({minDrops}). Forcing to minDrops.");
            dropCount = minDrops;
        }
        
        Debug.Log($"DestroyableObject: Spawning {dropCount} drops (min: {minDrops}, max: {maxDrops}) from {validPrefabs.Count} valid prefab(s).");
        
        int successfulSpawns = 0;
        for (int i = 0; i < dropCount; i++)
        {
            // Randomly select a prefab from the valid prefabs
            GameObject dropPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
            
            if (dropPrefab == null)
            {
                Debug.LogError($"DestroyableObject: Selected drop prefab is null at index {i}! Skipping this drop.");
                continue;
            }
            
            Vector3 randomOffset = Random.insideUnitSphere * dropScatterRadius;
            randomOffset.y = 0; // Keep drops on ground level
            Vector3 spawnPos = transform.position + randomOffset;
            
            // Ensure spawn position is slightly above ground to prevent falling through
            spawnPos.y = transform.position.y + 0.2f;
            
            GameObject drop = Instantiate(dropPrefab, spawnPos, Random.rotation);
            if (drop == null)
            {
                Debug.LogError($"DestroyableObject: Failed to instantiate drop prefab '{dropPrefab.name}'!");
                continue;
            }
            
            successfulSpawns++;
            Debug.Log($"DestroyableObject: Spawned drop {successfulSpawns}/{dropCount}: '{drop.name}' at position {spawnPos}");
            
            // Add force to scatter the drop
            if (drop.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                Vector3 forceDirection = Random.insideUnitSphere;
                forceDirection.y = Mathf.Max(forceDirection.y, 0.3f); // Ensure upward force
                rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning($"DestroyableObject: Drop '{drop.name}' has no Rigidbody component, cannot apply force.");
            }
        }
        
        if (successfulSpawns < dropCount)
        {
            Debug.LogWarning($"DestroyableObject: Only spawned {successfulSpawns} out of {dropCount} requested drops for {gameObject.name}.");
        }
        else
        {
            Debug.Log($"DestroyableObject: Successfully spawned all {successfulSpawns} drops for {gameObject.name}.");
        }
    }

    void SpawnSproutSeedDrops()
    {
        if (sproutSeedPrefab == null)
        {
            Debug.LogWarning("Sprout Seed Prefab is not assigned in DestroyableObject for " + gameObject.name + ". Cannot spawn sprout seeds.");
            return;
        }

        int seedDropCount = Random.Range(minSproutSeedDrops, maxSproutSeedDrops + 1);
        Debug.Log($"Spawning {seedDropCount} sprout seeds for {gameObject.name}.");

        for (int i = 0; i < seedDropCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * dropScatterRadius;
            // Ensure drops spawn slightly above the ground to prevent falling through
            randomOffset.y = Mathf.Max(randomOffset.y, 0.5f); // Ensure a minimum positive Y offset, adjust 0.5f if needed
            Vector3 spawnPos = transform.position + randomOffset;

            GameObject spawnedSeed = Instantiate(sproutSeedPrefab, spawnPos, Random.rotation);
            spawnedSeed.transform.localScale = sproutSeedDropScale;
            Debug.Log($"Spawned sprout seed: {spawnedSeed.name} at position {spawnedSeed.transform.position}"); // Added position log

            if (spawnedSeed.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddForce(Random.insideUnitSphere * dropForce, ForceMode.Impulse);
                Debug.Log("Applied force to sprout seed drop");
            }
        }
    }
    
    // Public method to configure this object as a tree with drop settings
    // This can be called when spawning trees to ensure they're properly configured
    public void ConfigureAsTree(bool enableSproutSeedDrops = true)
    {
        isTree = true;
        canDropSproutSeed = enableSproutSeedDrops;
        Debug.Log($"DestroyableObject: Configured '{gameObject.name}' as a tree (isTree=true, canDropSproutSeed={enableSproutSeedDrops}).");
    }
    
    // Public method to copy drop settings from another DestroyableObject
    public void CopyDropSettings(DestroyableObject source)
    {
        if (source == null)
        {
            Debug.LogWarning("DestroyableObject: Cannot copy drop settings from null source.");
            return;
        }
        
        // Ensure isTree is set if source is a tree
        if (source.IsTree)
        {
            isTree = true;
        }
        
        Debug.Log($"DestroyableObject: Copied tree configuration from '{source.gameObject.name}' to '{gameObject.name}' (isTree={isTree}).");
    }
} 