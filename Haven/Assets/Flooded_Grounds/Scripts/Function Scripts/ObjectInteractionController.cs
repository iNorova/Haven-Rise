using UnityEngine;

public class ObjectInteractionController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float hitDamage = 20f;  // Default damage (fallback if no item equipped)
    public float hitRange = 3f;
    public float hitCooldown = 0.5f;
    [SerializeField] private LayerMask interactableLayer = (1 << 8);  // Layer 8 is the Destroyable layer

    [Header("Item Damage Settings")]
    public float axeDamage = 25f;      // 100 health / 4 hits = 25 damage per hit
    public float rockDamage = 14.29f;  // 100 health / 7 hits ≈ 14.29 damage per hit

    [Header("Visual Feedback")]
    public GameObject hitEffectPrefab;  // Optional: visual feedback when hitting object
    public float effectDuration = 0.2f;

    private Camera playerCamera;
    private float nextHitTime;
    private CharController_Motor motorController;
    private HotbarManager hotbarManager;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        motorController = GetComponent<CharController_Motor>();
        hotbarManager = FindObjectOfType<HotbarManager>();
        
        if (playerCamera == null)
        {
            Debug.LogError("No camera found in children of player!");
        }

        if (hotbarManager == null)
        {
            Debug.LogWarning("HotbarManager not found. Item-based damage will use default values.");
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
                // Calculate damage based on currently equipped item
                float damageToApply = GetDamageForCurrentItem();
                Debug.Log($"Found DestroyableObject component, applying damage: {damageToApply} (Item: {GetCurrentItemName()})");
                // Apply damage to the object
                destroyableObject.TakeDamage(damageToApply);

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
    
    // Get the damage value based on the currently equipped item
    private float GetDamageForCurrentItem()
    {
        if (hotbarManager == null) return hitDamage;
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return hitDamage;
        
        ItemIconProvider iconProvider = currentItem.GetComponent<ItemIconProvider>();
        if (iconProvider == null) return hitDamage;
        
        string itemName = iconProvider.itemName;
        if (string.IsNullOrEmpty(itemName)) return hitDamage;
        
        // Check item name (case-insensitive)
        string itemNameLower = itemName.ToLower();
        if (itemNameLower.Contains("axe"))
        {
            return axeDamage;
        }
        else if (itemNameLower.Contains("rock") || itemNameLower.Contains("stone"))
        {
            return rockDamage;
        }
        
        // Default damage for other items
        return hitDamage;
    }
    
    // Helper method to get current item name for debugging
    private string GetCurrentItemName()
    {
        if (hotbarManager == null) return "None";
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return "None";
        
        ItemIconProvider iconProvider = currentItem.GetComponent<ItemIconProvider>();
        if (iconProvider == null) return currentItem.name;
        
        return iconProvider.itemName ?? currentItem.name;
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