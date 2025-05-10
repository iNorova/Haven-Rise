using UnityEngine;

public class ObjectInteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float hitDamage = 20f;
    public float hitRange = 3f;
    public float hitCooldown = 0.5f;
    [SerializeField] private LayerMask interactableLayer = (1 << 8);  // Layer 8 is the Destroyable layer

    [Header("Visual Feedback")]
    public GameObject hitEffectPrefab;  // Optional: visual feedback when hitting object
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

        // Debug log to verify layer mask
        Debug.Log($"Interaction Layer Mask: {interactableLayer.value}");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextHitTime)  // Left click
        {
            TryInteractWithObject();
            nextHitTime = Time.time + hitCooldown;
        }
    }

    void TryInteractWithObject()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen

        Debug.DrawRay(ray.origin, ray.direction * hitRange, Color.red, 1f); // Visualize the ray

        if (Physics.Raycast(ray, out hit, hitRange, interactableLayer))
        {
            Debug.Log($"Hit object: {hit.collider.gameObject.name} on layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            
            DestroyableObject destroyableObject = hit.collider.GetComponent<DestroyableObject>();
            if (destroyableObject != null)
            {
                Debug.Log($"Found DestroyableObject component, applying damage: {hitDamage}");
                // Apply damage to the object
                destroyableObject.TakeDamage(hitDamage);

                // Show hit effect at point of impact
                ShowHitEffect(hit.point, hit.normal);
            }
            else
            {
                Debug.LogWarning($"Hit object does not have DestroyableObject component: {hit.collider.gameObject.name}");
            }
        }
        else
        {
            Debug.Log("No destroyable object in range");
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