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

    [Header("Effects")]
    [SerializeField] private ParticleSystem hitEffect;    // Optional: particle effect when object is hit
    [SerializeField] private AudioClip hitSound;          // Optional: sound when object is hit
    [SerializeField] private AudioClip destroySound;      // Optional: sound when object is destroyed

    public UnityEvent onObjectDestroyed = new UnityEvent();

    private AudioSource audioSource;
    private bool isDestroyed = false;

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

        // If this is a tree, notify UIManager to increase temperature
        if (isTree && UIManager.Instance != null)
        {
            UIManager.Instance.StartTemperatureIncrease();
            Debug.Log("Tree destroyed - increasing temperature");
        }

        // Play destroy sound
        if (destroySound != null && audioSource != null)
        {
            audioSource.PlayOneShot(destroySound);
            Debug.Log("Destroy sound played");
        }

        // Spawn drops
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
            Debug.Log("No drop prefabs assigned");
            return;
        }

        int dropCount = Random.Range(minDrops, maxDrops + 1);
        Debug.Log($"Spawning {dropCount} drops");
        
        for (int i = 0; i < dropCount; i++)
        {
            // Randomly select a prefab from the array
            GameObject dropPrefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            
            Vector3 randomOffset = Random.insideUnitSphere * dropScatterRadius;
            randomOffset.y = 0;
            Vector3 spawnPos = transform.position + randomOffset;
            
            GameObject drop = Instantiate(dropPrefab, spawnPos, Random.rotation);
            Debug.Log($"Spawned drop: {drop.name}");
            
            // Add force to scatter the drop
            if (drop.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddForce(Random.insideUnitSphere * dropForce, ForceMode.Impulse);
                Debug.Log("Applied force to drop");
            }
        }
    }
} 