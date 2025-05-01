using UnityEngine;

public class TreeCuttingController : MonoBehaviour
{
    [Header("Cutting Settings")]
    public float hitDamage = 20f;
    public float hitRange = 3f;
    public float hitCooldown = 0.5f;
    public LayerMask treeLayer;  // Set this to the layer your trees are on

    [Header("Visual Feedback")]
    public GameObject hitEffectPrefab;  // Optional: visual feedback when hitting tree
    public float effectDuration = 0.2f;

    private Camera playerCamera;
    private float nextHitTime;
    private CharController_Motor motorController;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        motorController = GetComponent<CharController_Motor>();
        
        if (playerCamera == null)
        {
            Debug.LogError("No camera found in children of player!");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextHitTime)  // Left click
        {
            TryHitTree();
            nextHitTime = Time.time + hitCooldown;
        }
    }

    void TryHitTree()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen

        if (Physics.Raycast(ray, out hit, hitRange, treeLayer))
        {
            TreeComponent tree = hit.collider.GetComponent<TreeComponent>();
            if (tree != null)
            {
                // Apply damage to the tree
                tree.TakeDamage(hitDamage);

                // Show hit effect at point of impact
                ShowHitEffect(hit.point, hit.normal);
            }
        }
    }

    void ShowHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(effect, effectDuration);
        }
    }

    // Optional: Draw hit range in editor for debugging
    void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerCamera.transform.position, hitRange);
        }
    }
} 