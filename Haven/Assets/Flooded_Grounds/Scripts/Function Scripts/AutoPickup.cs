using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AutoPickup : MonoBehaviour
{
    [Header("Auto Pickup Settings")]
    [Tooltip("Enable automatic pickup when player collides with items")]
    public bool enableAutoPickup = true;
    
    [Tooltip("Pickup range in X, Y, Z directions - Adjust this in Inspector to change pickup distance in each axis")]
    public Vector3 pickupRange = new Vector3(2f, 2f, 2f);
    
    [Tooltip("How often to check for nearby items (seconds)")]
    [Range(0.05f, 0.5f)]
    public float checkInterval = 0.1f;
    
    [Tooltip("Cooldown between pickups for the same item (prevents spam)")]
    [Range(0.1f, 1f)]
    public float pickupCooldown = 0.2f;
    
    [Tooltip("Items to exclude from auto pickup (e.g., placed campfires, beds)")]
    public string[] excludedTags = { "Untagged" };
    
    [Header("Debug")]
    [Tooltip("Show debug messages in console")]
    public bool showDebugMessages = false;
    
    private HotbarManager hotbarManager;
    private InventoryManager inventoryManager;
    private CharacterController characterController;
    private Dictionary<GameObject, float> pickupCooldowns = new Dictionary<GameObject, float>();
    public HashSet<GameObject> itemsBeingPickedUp = new HashSet<GameObject>(); // Track items currently being picked up (made public for manual pickup check)
    private float lastCheckTime = 0f;
    
    // Public method to clear tracking for a dropped item so it can be picked up again
    public void ClearItemTracking(GameObject item)
    {
        if (item == null) return;
        
        // Remove from itemsBeingPickedUp
        if (itemsBeingPickedUp != null && itemsBeingPickedUp.Contains(item))
        {
            itemsBeingPickedUp.Remove(item);
        }
        
        // Remove from pickupCooldowns
        if (pickupCooldowns != null && pickupCooldowns.ContainsKey(item))
        {
            pickupCooldowns.Remove(item);
        }
    }
    
    void Start()
    {
        hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null)
        {
            Debug.LogError("AutoPickup: HotbarManager not found!");
        }
        
        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("AutoPickup: InventoryManager not found!");
        }
        
        // Check for CharacterController (CharacterController doesn't work with triggers)
        characterController = GetComponent<CharacterController>();
        
        // Add a separate trigger collider if using CharacterController
        if (characterController != null)
        {
            // Check if we already have a trigger collider
            Collider[] colliders = GetComponents<Collider>();
            bool hasTrigger = false;
            foreach (Collider col in colliders)
            {
                if (col.isTrigger && !(col is CharacterController))
                {
                    hasTrigger = true;
                    break;
                }
            }
            
            if (!hasTrigger)
            {
                // Add a trigger collider that's slightly larger than the CharacterController
                CapsuleCollider trigger = gameObject.AddComponent<CapsuleCollider>();
                trigger.isTrigger = true;
                trigger.radius = characterController.radius + 0.5f; // Slightly larger
                trigger.height = characterController.height + 0.5f;
                trigger.center = characterController.center;
                
                if (showDebugMessages)
                {
                    Debug.Log($"AutoPickup: Added trigger collider for CharacterController. Radius: {trigger.radius}, Height: {trigger.height}");
                }
            }
        }
        else
        {
            // For non-CharacterController, ensure we have a trigger collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;
                capsule.radius = 1f;
                capsule.height = 2f;
                
                if (showDebugMessages)
                {
                    Debug.Log("AutoPickup: Added CapsuleCollider as trigger");
                }
            }
            else if (!col.isTrigger)
            {
                Debug.LogWarning($"AutoPickup: Collider on player is not a trigger! Auto-pickup may not work. Collider type: {col.GetType().Name}");
            }
        }
    }
    
    void Update()
    {
        if (!enableAutoPickup) return;
        
        // Clean up old cooldowns and tracking
        if (pickupCooldowns.Count > 0)
        {
            List<GameObject> toRemove = new List<GameObject>();
            foreach (var kvp in pickupCooldowns)
            {
                if (kvp.Value <= Time.time || kvp.Key == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (GameObject obj in toRemove)
            {
                if (obj != null)
                {
                    pickupCooldowns.Remove(obj);
                }
            }
        }
        
        // Clean up itemsBeingPickedUp for destroyed items
        itemsBeingPickedUp.RemoveWhere(item => item == null);
        
        // Use OverlapSphere to find nearby items (more reliable than OnTriggerEnter)
        if (Time.time - lastCheckTime >= checkInterval)
        {
            lastCheckTime = Time.time;
            CheckForNearbyItems();
        }
    }
    
    void CheckForNearbyItems()
    {
        // Use OverlapBox to find all nearby pickupable items with separate X, Y, Z ranges
        Collider[] nearbyColliders = Physics.OverlapBox(transform.position, pickupRange * 0.5f, Quaternion.identity);
        
        foreach (Collider col in nearbyColliders)
        {
            if (col == null || col.gameObject == null) continue;
            
            // Skip self and player
            if (col.gameObject == this.gameObject) continue;
            if (col.gameObject.CompareTag("Player")) continue;
            if (col.transform.root == this.transform) continue; // Skip if it's part of player hierarchy
            
            // Try to pickup the item
            TryAutoPickup(col.gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!enableAutoPickup) return;
        
        TryAutoPickup(other.gameObject);
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!enableAutoPickup) return;
        
        // Also check items that stay in trigger (handles fast-moving items)
        TryAutoPickup(other.gameObject);
    }
    
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enableAutoPickup) return;
        
        // Handle CharacterController collisions
        TryAutoPickup(hit.gameObject);
    }
    
    void TryAutoPickup(GameObject item)
    {
        if (item == null || !item.activeSelf) return;
        
        // CRITICAL: Never pick up the player or anything in the player hierarchy
        if (item.CompareTag("Player") || item.transform.root.CompareTag("Player"))
        {
            return;
        }
        if (item.transform.root == this.transform || item.transform.root == this.transform.root)
        {
            return;
        }
        
        // Check if item is already being picked up
        if (itemsBeingPickedUp.Contains(item))
        {
            return;
        }
        
        // Check cooldown
        if (pickupCooldowns.ContainsKey(item) && pickupCooldowns[item] > Time.time)
        {
            return;
        }
        
        // Check distance using X, Y, Z ranges separately (make sure item is actually within range)
        Vector3 distance = item.transform.position - transform.position;
        distance.x = Mathf.Abs(distance.x);
        distance.y = Mathf.Abs(distance.y);
        distance.z = Mathf.Abs(distance.z);
        
        if (distance.x > pickupRange.x || distance.y > pickupRange.y || distance.z > pickupRange.z)
        {
            return;
        }
        
        // Check if item has Pickupable tag or ItemIconProvider component
        bool isPickupable = item.CompareTag("Pickupable");
        bool hasItemIcon = item.GetComponent<ItemIconProvider>() != null || item.GetComponentInParent<ItemIconProvider>() != null;
        
        // Also check child objects (items might have colliders on children)
        if (!isPickupable && !hasItemIcon)
        {
            // Check if any child has the tag or component
            foreach (Transform child in item.GetComponentsInChildren<Transform>())
            {
                if (child.CompareTag("Pickupable") || child.GetComponent<ItemIconProvider>() != null)
                {
                    isPickupable = true;
                    hasItemIcon = true;
                    break;
                }
            }
        }
        
        if (!isPickupable && !hasItemIcon)
        {
            return;
        }
        
        // Check if item is excluded
        foreach (string excludedTag in excludedTags)
        {
            if (item.CompareTag(excludedTag))
            {
                return;
            }
        }
        
        // Skip placed items (campfires, beds) - these should be picked up manually
        if (item.GetComponent<CampfirePickup>() != null || 
            item.GetComponent<BedPickup>() != null ||
            item.name.Contains("_Placed") || 
            item.name.Contains("_placed"))
        {
            return;
        }
        
        // Skip if item is already in hotbar or inventory
        if (hotbarManager != null)
        {
            for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
            {
                GameObject heldItem = hotbarManager.GetItem(i);
                if (heldItem == item)
                {
                    return; // Item is already in hotbar
                }
            }
        }
        
        // Try to pick up the item
        PickupItem(item);
    }
    
    void PickupItem(GameObject item)
    {
        if (item == null || hotbarManager == null)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[AutoPickup] Cannot pickup - item or hotbarManager is null");
            }
            return;
        }
        
        // Double-check item is still valid
        if (!item.activeSelf)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[AutoPickup] Cannot pickup '{item.name}' - item is inactive");
            }
            return;
        }
        
        // Check if item is already being picked up (by manual or auto pickup)
        if (itemsBeingPickedUp.Contains(item))
        {
            return; // Already being picked up
        }
        
        // Check if item is already in hotbar or inventory (prevent double pickup)
        if (hotbarManager != null)
        {
            for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
            {
                GameObject heldItem = hotbarManager.GetItem(i);
                if (heldItem == item)
                {
                    return; // Item is already in hotbar
                }
            }
        }
        
        // Mark as being picked up to prevent duplicates (from manual or auto pickup)
        itemsBeingPickedUp.Add(item);
        
        // Find empty slot in hotbar
        int emptySlot = -1;
        for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
        {
            if (hotbarManager.GetItem(i) == null)
            {
                emptySlot = i;
                break;
            }
        }
        
        if (emptySlot != -1)
        {
            // Pick up into hotbar
            if (showDebugMessages)
            {
                Debug.Log($"<color=green>[AutoPickup] ✓ Auto-picked up '{item.name}' into hotbar slot {emptySlot}</color>");
            }
            
            try
            {
                hotbarManager.PickupItem(item, emptySlot);
                
                // Set cooldown
                pickupCooldowns[item] = Time.time + pickupCooldown;
                
                // Remove from tracking after pickup completes
                StartCoroutine(RemoveFromTracking(item, true));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoPickup] Error picking up '{item.name}': {e.Message}");
                itemsBeingPickedUp.Remove(item);
            }
        }
        else if (inventoryManager != null && hotbarManager != null)
        {
            // Try inventory if hotbar is full - use TryPickupIntoInventory to properly prepare the item
            try
            {
                if (hotbarManager.TryPickupIntoInventory(item))
                {
                    if (showDebugMessages)
                    {
                        Debug.Log($"<color=green>[AutoPickup] ✓ Auto-picked up '{item.name}' into inventory (hotbar full)</color>");
                    }
                    
                    // Set cooldown
                    pickupCooldowns[item] = Time.time + pickupCooldown;
                    
                    // Remove from tracking after pickup completes
                    StartCoroutine(RemoveFromTracking(item, true));
                }
                else
                {
                    // Inventory full - remove from tracking
                    if (showDebugMessages)
                    {
                        Debug.LogWarning($"<color=yellow>[AutoPickup] ⚠ Cannot auto-pickup '{item.name}' - inventory and hotbar are full</color>");
                    }
                    itemsBeingPickedUp.Remove(item);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoPickup] Error adding '{item.name}' to inventory: {e.Message}");
                itemsBeingPickedUp.Remove(item);
            }
        }
        else
        {
            // No inventory manager - remove from tracking
            if (showDebugMessages)
            {
                Debug.LogWarning($"[AutoPickup] Cannot pickup '{item.name}' - hotbar full and no inventory manager");
            }
            itemsBeingPickedUp.Remove(item);
        }
    }
    
    IEnumerator RemoveFromTracking(GameObject item, bool success)
    {
        // Wait a frame to ensure pickup is complete
        yield return null;
        
        // Remove from tracking after pickup cooldown (longer if successful to prevent re-pickup)
        float waitTime = success ? pickupCooldown * 2f : pickupCooldown;
        yield return new WaitForSeconds(waitTime);
        
        if (item != null)
        {
            itemsBeingPickedUp.Remove(item);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Visualize pickup range as a box (X, Y, Z)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, pickupRange);
        
        // Also visualize trigger collider if it exists
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger && !(col is CharacterController))
        {
            Gizmos.color = Color.cyan;
            
            if (col is SphereCollider)
            {
                SphereCollider sphere = col as SphereCollider;
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider)
            {
                CapsuleCollider capsule = col as CapsuleCollider;
                Gizmos.DrawWireSphere(transform.position + capsule.center + Vector3.up * (capsule.height * 0.5f - capsule.radius), capsule.radius);
                Gizmos.DrawWireSphere(transform.position + capsule.center - Vector3.up * (capsule.height * 0.5f - capsule.radius), capsule.radius);
            }
            else if (col is BoxCollider)
            {
                BoxCollider box = col as BoxCollider;
                Gizmos.DrawWireCube(transform.position + box.center, box.size);
            }
        }
    }
}

