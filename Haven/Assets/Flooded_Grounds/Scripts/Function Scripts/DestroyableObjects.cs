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
        if (dropPrefabs == null || dropPrefabs.Length == 0)
        {
            Debug.Log("No general drop prefabs assigned");
            return;
        }

        int dropCount = Random.Range(minDrops, maxDrops + 1);
        Debug.Log($"Spawning {dropCount} general drops.");
        
        for (int i = 0; i < dropCount; i++)
        {
            // Randomly select a prefab from the array
            GameObject dropPrefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            
            Vector3 randomOffset = Random.insideUnitSphere * dropScatterRadius;
            randomOffset.y = 0;
            Vector3 spawnPos = transform.position + randomOffset;
            
            GameObject drop = Instantiate(dropPrefab, spawnPos, Random.rotation);
            Debug.Log($"Spawned general drop: {drop.name}");
            
            // Add force to scatter the drop
            if (drop.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddForce(Random.insideUnitSphere * dropForce, ForceMode.Impulse);
                Debug.Log("Applied force to general drop");
            }
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
} 