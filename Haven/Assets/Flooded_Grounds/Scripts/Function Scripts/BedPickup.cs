using UnityEngine;
using System.Collections;

public class BedPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.F;     // Key to pick up the bed
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
            Debug.LogError("BedPickup: No main camera found!");
        }

        player = GameObject.FindGameObjectWithTag("Player");
        
        hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null)
        {
            Debug.LogError("BedPickup: HotbarManager not found!");
        }

        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("BedPickup: InventoryManager not found!");
        }
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

        // Check if player is looking at this bed and pressing F
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupBed();
        }
    }

    void TryPickupBed()
    {
        // Raycast to check if player is looking at this bed
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(transform))
            {
                PickupBed();
            }
        }
    }

    void PickupBed()
    {
        Debug.Log("BedPickup: Picking up bed - restoring to original state.");

        // Check if bed is currently transitioning to night
        BedInteraction bedInteraction = GetComponent<BedInteraction>();
        if (bedInteraction != null && bedInteraction.isTransitioning)
        {
            Debug.Log("BedPickup: Cannot pick up bed while transitioning to night. Wait for transition to complete!");
            return;
        }

        GameObject bedToPickup = this.gameObject;

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
                // Restore bed to original state (no cloning - use the same bed)
                RestoreBedToOriginalState(bedToPickup);
                
                // Verify name is cleaned before adding to hotbar
                if (bedToPickup.name.Contains("_Placed") || bedToPickup.name.Contains("_placed"))
                {
                    Debug.LogWarning($"BedPickup: Bed name still contains '_Placed' after restoration! Name: '{bedToPickup.name}'. Cleaning again...");
                    bedToPickup.name = bedToPickup.name.Replace("_Placed", "").Replace("_placed", "").Trim();
                }
                
                // Deactivate for hotbar storage
                bedToPickup.SetActive(false);
                
                // Pickup into hotbar
                hotbarManager.PickupItem(bedToPickup, emptySlot);
                pickedUp = true;
                Debug.Log($"BedPickup: Bed '{bedToPickup.name}' restored and picked up into hotbar slot {emptySlot}. Final name check: '{bedToPickup.name}'");
            }
        }

        // Try inventory if hotbar is full
        if (!pickedUp && inventoryManager != null)
        {
            // Restore bed to original state
            RestoreBedToOriginalState(bedToPickup);
            
            // Deactivate for inventory storage
            bedToPickup.SetActive(false);

            if (inventoryManager.AddItem(bedToPickup))
            {
                pickedUp = true;
                Debug.Log("BedPickup: Bed restored and picked up into inventory.");
            }
        }

        if (!pickedUp)
        {
            Debug.Log("BedPickup: Cannot pick up bed - inventory and hotbar are full.");
        }
    }
    
    void RestoreBedToOriginalState(GameObject bed)
    {
        // Clean up the name - remove "_Placed" suffix if present (case-insensitive)
        string originalName = bed.name;
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
        bed.name = cleanName;
        
        // Verify the name was actually changed
        if (bed.name != cleanName)
        {
            Debug.LogWarning($"BedPickup: Name change failed! Expected '{cleanName}', got '{bed.name}'. Trying again...");
            bed.name = cleanName; // Try again
        }
        
        Debug.Log($"BedPickup: Restoring bed '{originalName}' -> '{bed.name}' (verified)");
        
        // Reset physics state (bed might have been placed with kinematic)
        Rigidbody rb = bed.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Keep kinematic for inventory/hotbar
            rb.useGravity = false;
        }
        
        // Disable colliders (will be re-enabled when placed)
        Collider[] colliders = bed.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Remove placed bed components
        BedInteraction interaction = bed.GetComponent<BedInteraction>();
        if (interaction != null)
        {
            Destroy(interaction);
            Debug.Log("BedPickup: Removed BedInteraction component.");
        }
        
        // Remove any existing BedPlacement (shouldn't be there, but just in case)
        BedPlacement existingPlacement = bed.GetComponent<BedPlacement>();
        if (existingPlacement != null)
        {
            Destroy(existingPlacement);
            Debug.Log("BedPickup: Removed existing BedPlacement component.");
        }
        
        // Ensure bed is active temporarily so BedPlacement can initialize properly
        bed.SetActive(true);
        
        // Verify bed is active before adding component
        if (!bed.activeSelf)
        {
            Debug.LogError($"BedPickup: Bed '{bed.name}' failed to activate! Cannot add BedPlacement.");
        }
        
        // Add BedPlacement script so it can be placed again
        BedPlacement placement = bed.AddComponent<BedPlacement>();
        
        if (placement == null)
        {
            Debug.LogError($"BedPickup: Failed to add BedPlacement component to '{bed.name}'!");
        }
        else
        {
            // Initialize references immediately so placement works when bed is selected
            placement.InitializeReferences();
            placement.enabled = true;
            
            // Verify component was added correctly
            BedPlacement verify = bed.GetComponent<BedPlacement>();
            if (verify == null)
            {
                Debug.LogError($"BedPickup: BedPlacement component not found after adding to '{bed.name}'!");
            }
            else
            {
                Debug.Log($"BedPickup: Added BedPlacement component to '{bed.name}' - bed restored to original state. Component verified: {verify != null}");
            }
        }
        
        // Remove this BedPickup component (since bed is being picked up, it no longer needs BedPickup)
        // We'll destroy it after the frame ends to avoid issues
        StartCoroutine(DestroyBedPickupComponent());
    }
    
    System.Collections.IEnumerator DestroyBedPickupComponent()
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
