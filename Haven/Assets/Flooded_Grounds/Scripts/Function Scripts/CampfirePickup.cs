using UnityEngine;
using System.Collections;

public class CampfirePickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.F;     // Key to pick up the campfire
    public float interactionRange = 3f;        // How close player needs to be to pick up

    private Camera playerCamera;
    private GameObject player;
    private HotbarManager hotbarManager;
    private InventoryManager inventoryManager;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("CampfirePickup: No main camera found!");
        }

        player = GameObject.FindGameObjectWithTag("Player");
        
        hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null)
        {
            Debug.LogError("CampfirePickup: HotbarManager not found!");
        }

        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("CampfirePickup: InventoryManager not found!");
        }
        
        // Ensure campfire has colliders enabled and is on a raycastable layer
        EnsureCampfireIsPickupable();
    }
    
    void EnsureCampfireIsPickupable()
    {
        // Make sure campfire has at least one enabled collider
        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool hasEnabledCollider = false;
        foreach (Collider col in colliders)
        {
            if (col != null && col.enabled)
            {
                hasEnabledCollider = true;
                break;
            }
        }
        
        if (!hasEnabledCollider && colliders.Length > 0)
        {
            Debug.LogWarning($"[CampfirePickup] Campfire '{gameObject.name}' has colliders but none are enabled! Enabling first collider...");
            if (colliders[0] != null)
            {
                colliders[0].enabled = true;
            }
        }
        
        // Ensure campfire is not on "Ignore Raycast" layer
        if (gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
        {
            Debug.LogWarning($"[CampfirePickup] Campfire '{gameObject.name}' is on 'Ignore Raycast' layer! Moving to 'Pickup' or 'Default' layer...");
            int pickupLayer = LayerMask.NameToLayer("Pickup");
            if (pickupLayer == -1)
            {
                pickupLayer = LayerMask.NameToLayer("Default");
            }
            gameObject.layer = pickupLayer;
            
            // Also set all children
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = pickupLayer;
            }
        }
        
        Debug.Log($"[CampfirePickup] Campfire '{gameObject.name}' initialized. Layer: {LayerMask.LayerToName(gameObject.layer)}, HasColliders: {colliders.Length > 0}, EnabledColliders: {hasEnabledCollider}");
    }

    void Update()
    {
        if (playerCamera == null || player == null)
            return;

        // Check if player is close enough
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > interactionRange)
        {
            return;
        }

        // Check if player is looking at this campfire and pressing F
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupCampfire();
        }
    }

    void TryPickupCampfire()
    {
        // Raycast to check if player is looking at this campfire
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        // Use a layer mask that includes all layers except Ignore Raycast
        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        
        if (Physics.Raycast(ray, out hit, interactionRange, layerMask))
        {
            // Check if we hit this campfire or any of its children
            GameObject hitObject = hit.collider.gameObject;
            if (hitObject == this.gameObject || hitObject.transform.IsChildOf(transform) || 
                transform.IsChildOf(hitObject.transform))
            {
                Debug.Log($"[CampfirePickup] Raycast hit campfire: {hitObject.name}");
                PickupCampfire();
            }
            else
            {
                Debug.Log($"[CampfirePickup] Raycast hit something else: {hitObject.name}");
            }
        }
        else
        {
            Debug.Log("[CampfirePickup] Raycast didn't hit anything within range.");
        }
    }

    void PickupCampfire()
    {
        Debug.Log("CampfirePickup: Picking up campfire - restoring to original state.");

        GameObject campfireToPickup = this.gameObject;

        // Find an empty slot in hotbar first, then inventory
        bool pickedUp = false;

        // Try hotbar first
        if (hotbarManager != null)
        {
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
                // Restore campfire to original state (no cloning - use the same campfire)
                RestoreCampfireToOriginalState(campfireToPickup);
                
                // Verify name is cleaned before adding to hotbar
                if (campfireToPickup.name.Contains("_Placed") || campfireToPickup.name.Contains("_placed"))
                {
                    Debug.LogWarning($"CampfirePickup: Campfire name still contains '_Placed' after restoration! Name: '{campfireToPickup.name}'. Cleaning again...");
                    campfireToPickup.name = campfireToPickup.name.Replace("_Placed", "").Replace("_placed", "").Trim();
                }
                
                // Deactivate for hotbar storage
                campfireToPickup.SetActive(false);
                
                // Pickup into hotbar
                hotbarManager.PickupItem(campfireToPickup, emptySlot);
                pickedUp = true;
                Debug.Log($"CampfirePickup: Campfire '{campfireToPickup.name}' restored and picked up into hotbar slot {emptySlot}. Final name check: '{campfireToPickup.name}'");
            }
        }

        // Try inventory if hotbar is full
        if (!pickedUp && inventoryManager != null)
        {
            // Restore campfire to original state
            RestoreCampfireToOriginalState(campfireToPickup);
            
            // Deactivate for inventory storage
            campfireToPickup.SetActive(false);

            if (inventoryManager.AddItem(campfireToPickup))
            {
                pickedUp = true;
                Debug.Log("CampfirePickup: Campfire restored and picked up into inventory.");
            }
        }

        if (!pickedUp)
        {
            Debug.Log("CampfirePickup: Cannot pick up campfire - inventory and hotbar are full.");
        }
    }
    
    void RestoreCampfireToOriginalState(GameObject campfire)
    {
        // Clean up the name - remove "_Placed" suffix if present (case-insensitive)
        string originalName = campfire.name;
        string cleanName = originalName;
        
        // Remove "_Placed" suffix (case-insensitive)
        if (cleanName.Contains("_Placed"))
        {
            cleanName = cleanName.Replace("_Placed", "");
        }
        else if (cleanName.Contains("_placed"))
        {
            cleanName = cleanName.Replace("_placed", "");
        }
        
        // Remove "(Clone)" suffix if present
        if (cleanName.Contains("(Clone)"))
        {
            cleanName = cleanName.Replace("(Clone)", "");
        }
        
        // Trim any extra spaces
        cleanName = cleanName.Trim();
        
        // Set the cleaned name
        campfire.name = cleanName;
        
        // Verify the name was actually changed
        if (campfire.name != cleanName)
        {
            Debug.LogWarning($"CampfirePickup: Name change failed! Expected '{cleanName}', got '{campfire.name}'. Trying again...");
            campfire.name = cleanName; // Try again
        }
        
        Debug.Log($"CampfirePickup: Restoring campfire '{originalName}' -> '{campfire.name}' (verified)");
        
        // Reset physics state (campfire might have been placed with kinematic)
        Rigidbody rb = campfire.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Keep kinematic for inventory/hotbar
            rb.useGravity = false;
        }
        
        // Disable colliders (will be re-enabled when placed)
        Collider[] colliders = campfire.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Remove any existing CampfirePlacement (shouldn't be there, but just in case)
        CampfirePlacement existingPlacement = campfire.GetComponent<CampfirePlacement>();
        if (existingPlacement != null)
        {
            Destroy(existingPlacement);
            Debug.Log("CampfirePickup: Removed existing CampfirePlacement component.");
        }
        
        // Ensure campfire is active temporarily so CampfirePlacement can initialize properly
        campfire.SetActive(true);
        
        // Verify campfire is active before adding component
        if (!campfire.activeSelf)
        {
            Debug.LogError($"CampfirePickup: Campfire '{campfire.name}' failed to activate! Cannot add CampfirePlacement.");
        }
        
        // Add CampfirePlacement script so it can be placed again
        CampfirePlacement placement = campfire.AddComponent<CampfirePlacement>();
        
        if (placement == null)
        {
            Debug.LogError($"CampfirePickup: Failed to add CampfirePlacement component to '{campfire.name}'!");
        }
        else
        {
            // Initialize references immediately so placement works when campfire is selected
            placement.InitializeReferences();
            placement.enabled = true;
            
            // Verify component was added correctly
            CampfirePlacement verify = campfire.GetComponent<CampfirePlacement>();
            if (verify == null)
            {
                Debug.LogError($"CampfirePickup: CampfirePlacement component not found after adding to '{campfire.name}'!");
            }
            else
            {
                Debug.Log($"CampfirePickup: Added CampfirePlacement component to '{campfire.name}' - campfire restored to original state. Component verified: {verify != null}");
            }
        }
        
        // Remove this CampfirePickup component (since campfire is being picked up, it no longer needs CampfirePickup)
        // We'll destroy it after the frame ends to avoid issues
        StartCoroutine(DestroyCampfirePickupComponent());
    }
    
    System.Collections.IEnumerator DestroyCampfirePickupComponent()
    {
        // Wait until end of frame to safely destroy this component
        yield return new WaitForEndOfFrame();
        Destroy(this);
    }

    // Visualize interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

