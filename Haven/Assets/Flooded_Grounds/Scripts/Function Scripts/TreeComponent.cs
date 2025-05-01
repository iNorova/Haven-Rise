using UnityEngine;
using UnityEngine.Events;

public class TreeComponent : MonoBehaviour
{
    [Header("Tree Properties")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    [SerializeField] private GameObject treeStumpPrefab;  // Optional: prefab to spawn when tree is cut
    [SerializeField] private GameObject woodPrefab;       // The wood item that drops when tree is cut
    [SerializeField] private int minWoodDrops = 3;
    [SerializeField] private int maxWoodDrops = 6;

    [Header("Effects")]
    [SerializeField] private ParticleSystem hitEffect;    // Optional: particle effect when tree is hit
    [SerializeField] private AudioClip hitSound;          // Optional: sound when tree is hit
    [SerializeField] private AudioClip fallSound;         // Optional: sound when tree falls

    public UnityEvent onTreeCut = new UnityEvent();

    private AudioSource audioSource;
    private bool isCut = false;

    void Start()
    {
        currentHealth = maxHealth;
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void TakeDamage(float damage)
    {
        if (isCut) return;

        currentHealth -= damage;

        // Play hit effects
        if (hitEffect != null)
            hitEffect.Play();

        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        // Check if tree should fall
        if (currentHealth <= 0)
        {
            CutTree();
        }
    }

    void CutTree()
    {
        if (isCut) return;
        isCut = true;

        // Play fall sound
        if (fallSound != null && audioSource != null)
            audioSource.PlayOneShot(fallSound);

        // Spawn wood drops
        SpawnWoodDrops();

        // Spawn stump if we have one
        if (treeStumpPrefab != null)
        {
            Instantiate(treeStumpPrefab, transform.position, transform.rotation);
        }

        // Trigger the event
        onTreeCut.Invoke();

        // Destroy the tree immediately
        Destroy(gameObject);
    }

    void SpawnWoodDrops()
    {
        if (woodPrefab == null) return;

        int dropCount = Random.Range(minWoodDrops, maxWoodDrops + 1);
        
        for (int i = 0; i < dropCount; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 2f;
            randomOffset.y = 0;
            Vector3 spawnPos = transform.position + randomOffset;
            
            GameObject wood = Instantiate(woodPrefab, spawnPos, Random.rotation);
            
            // Add force to scatter the wood
            if (wood.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
            }
        }
    }
} 