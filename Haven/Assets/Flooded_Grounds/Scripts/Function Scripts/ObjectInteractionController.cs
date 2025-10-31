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

    [Header("Audio Settings")]
    public AudioClip itemBreakSound;  // Sound effect to play when an item breaks (e.g., axe)
    
    private Camera playerCamera;
    private float nextHitTime;
    private CharController_Motor motorController;
    private HotbarManager hotbarManager;
    private AudioSource audioSource;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        motorController = GetComponent<CharController_Motor>();
        hotbarManager = FindObjectOfType<HotbarManager>();
        
        // Setup audio source for break sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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
        // Check if player has a valid item (axe or rock) equipped
        if (!IsValidItemEquipped())
        {
            Debug.Log("Cannot interact with destroyable objects: No axe or rock equipped.");
            return;
        }
        
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

                // Reduce durability if current item has durability component (e.g., axe)
                ReduceItemDurability();

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
    
    // Check if a valid item (axe or rock) is currently equipped
    private bool IsValidItemEquipped()
    {
        if (hotbarManager == null) return false;
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return false;
        
        ItemIconProvider iconProvider = currentItem.GetComponent<ItemIconProvider>();
        if (iconProvider == null) return false;
        
        string itemName = iconProvider.itemName;
        if (string.IsNullOrEmpty(itemName)) return false;
        
        // Check if item name contains "axe", "rock", or "stone" (case-insensitive)
        string itemNameLower = itemName.ToLower();
        return itemNameLower.Contains("axe") || itemNameLower.Contains("rock") || itemNameLower.Contains("stone");
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
    
    // Reduce durability of the currently equipped item when it hits something
    private void ReduceItemDurability()
    {
        if (hotbarManager == null) return;
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return;
        
        // Check if the item has a durability component (e.g., axe)
        // Check item itself first, then check children (e.g., Axe child of AxeHolder)
        ItemDurability durability = currentItem.GetComponent<ItemDurability>();
        if (durability == null)
        {
            durability = currentItem.GetComponentInChildren<ItemDurability>();
        }
        
        if (durability != null)
        {
            // Reduce durability by 1 per hit
            durability.ReduceDurability(1f);
            
            // Check if item is broken
            if (durability.IsBroken())
            {
                HandleItemBroken(currentItem);
            }
        }
    }
    
    // Handle when an item breaks (durability reaches 0)
    private void HandleItemBroken(GameObject brokenItem)
    {
        if (hotbarManager == null) return;
        
        int brokenSlotIndex = -1;
        
        // Find which slot the broken item is in
        for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
        {
            if (hotbarManager.GetItem(i) == brokenItem)
            {
                brokenSlotIndex = i;
                break;
            }
        }
        
        if (brokenSlotIndex >= 0)
        {
            Debug.Log($"[ObjectInteractionController] Item {brokenItem.name} broke! Removing from hotbar slot {brokenSlotIndex}");
            
            bool wasSelectedSlot = (brokenSlotIndex == hotbarManager.selectedSlot);
            
            // Deactivate and unparent the item first
            brokenItem.SetActive(false);
            brokenItem.transform.SetParent(null);
            
            // Clear the item from hotbar
            hotbarManager.SetItem(brokenSlotIndex, null);
            
            // Reset animation state when item breaks (allows slot switching)
            hotbarManager.ResetAnimationState();
            
            // Play break sound effect
            if (itemBreakSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(itemBreakSound);
                Debug.Log("Item break sound played");
            }
            
            // Update hotbar UI to reflect the empty slot
            hotbarManager.UpdateHotbarUI();
            
            // If this was the currently selected slot, update selection
            if (wasSelectedSlot)
            {
                // Try to select the next available slot, or first slot if none
                bool foundSlot = false;
                for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
                {
                    if (hotbarManager.GetItem(i) != null)
                    {
                        hotbarManager.SelectSlot(i);
                        foundSlot = true;
                        break;
                    }
                }
                // If no items left, select first slot anyway
                if (!foundSlot && hotbarManager.hotbarSlots.Length > 0)
                {
                    hotbarManager.SelectSlot(0);
                }
            }
            
            // Destroy the broken item
            Destroy(brokenItem);
            
            Debug.Log($"[ObjectInteractionController] Broken item {brokenItem.name} destroyed and removed from hotbar");
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