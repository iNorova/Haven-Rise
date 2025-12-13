using UnityEngine;
using System.Collections;

public class WorkbenchPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.F;     // Key to pick up the workbench
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
            Debug.LogError("WorkbenchPickup: No main camera found!");
        }

        player = GameObject.FindGameObjectWithTag("Player");
        
        hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null)
        {
            Debug.LogError("WorkbenchPickup: HotbarManager not found!");
        }

        inventoryManager = FindObjectOfType<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogError("WorkbenchPickup: InventoryManager not found!");
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

        // Check if player is looking at this workbench and pressing F
        if (Input.GetKeyDown(pickupKey))
        {
            TryPickupWorkbench();
        }
    }

    void TryPickupWorkbench()
    {
        // Raycast to check if player is looking at this workbench
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(transform))
            {
                PickupWorkbench();
            }
        }
    }

    void PickupWorkbench()
    {
        Debug.Log("WorkbenchPickup: Picking up workbench - restoring to original state.");

        GameObject workbenchToPickup = this.gameObject;

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
                // Restore workbench to original state (no cloning - use the same workbench)
                RestoreWorkbenchToOriginalState(workbenchToPickup);
                
                // Verify WorkbenchPlacement component exists after restoration
                WorkbenchPlacement placementCheck = workbenchToPickup.GetComponent<WorkbenchPlacement>();
                if (placementCheck == null)
                {
                    Debug.LogError($"WorkbenchPickup: WorkbenchPlacement component missing after restoration! Adding it now...");
                    placementCheck = workbenchToPickup.AddComponent<WorkbenchPlacement>();
                    if (placementCheck != null)
                    {
                        placementCheck.InitializeReferences();
                        placementCheck.enabled = true;
                    }
                }
                else
                {
                    Debug.Log($"WorkbenchPickup: WorkbenchPlacement component verified on '{workbenchToPickup.name}'. Enabled: {placementCheck.enabled}");
                }
                
                // Verify name is cleaned before adding to hotbar
                if (workbenchToPickup.name.Contains("_Placed") || workbenchToPickup.name.Contains("_placed"))
                {
                    Debug.LogWarning($"WorkbenchPickup: Workbench name still contains '_Placed' after restoration! Name: '{workbenchToPickup.name}'. Cleaning again...");
                    workbenchToPickup.name = workbenchToPickup.name.Replace("_Placed", "").Replace("_placed", "").Trim();
                }
                
                // Deactivate for hotbar storage
                workbenchToPickup.SetActive(false);
                
                // Pickup into hotbar
                hotbarManager.PickupItem(workbenchToPickup, emptySlot);
                pickedUp = true;
                Debug.Log($"WorkbenchPickup: Workbench '{workbenchToPickup.name}' restored and picked up into hotbar slot {emptySlot}. HasPlacement: {workbenchToPickup.GetComponent<WorkbenchPlacement>() != null}");
            }
        }

        // Try inventory if hotbar is full
        if (!pickedUp && inventoryManager != null)
        {
            // Restore workbench to original state
            RestoreWorkbenchToOriginalState(workbenchToPickup);
            
            // Deactivate for inventory storage
            workbenchToPickup.SetActive(false);

            if (inventoryManager.AddItem(workbenchToPickup))
            {
                pickedUp = true;
                Debug.Log("WorkbenchPickup: Workbench restored and picked up into inventory.");
            }
        }

        if (!pickedUp)
        {
            Debug.Log("WorkbenchPickup: Cannot pick up workbench - inventory and hotbar are full.");
        }
    }
    
    void RestoreWorkbenchToOriginalState(GameObject workbench)
    {
        // Clean up the name - remove "_Placed" suffix if present (case-insensitive)
        string originalName = workbench.name;
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
        workbench.name = cleanName;
        
        // Verify the name was actually changed
        if (workbench.name != cleanName)
        {
            Debug.LogWarning($"WorkbenchPickup: Name change failed! Expected '{cleanName}', got '{workbench.name}'. Trying again...");
            workbench.name = cleanName; // Try again
        }
        
        Debug.Log($"WorkbenchPickup: Restoring workbench '{originalName}' -> '{workbench.name}' (verified)");
        
        // Reset physics state (workbench might have been placed with kinematic)
        Rigidbody rb = workbench.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Keep kinematic for inventory/hotbar
            rb.useGravity = false;
        }
        
        // Disable colliders (will be re-enabled when placed)
        Collider[] colliders = workbench.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Remove any existing WorkbenchPlacement (shouldn't be there, but just in case)
        WorkbenchPlacement existingPlacement = workbench.GetComponent<WorkbenchPlacement>();
        if (existingPlacement != null)
        {
            Destroy(existingPlacement);
            Debug.Log("WorkbenchPickup: Removed existing WorkbenchPlacement component.");
        }
        
        // Ensure workbench is active temporarily so WorkbenchPlacement can initialize properly
        bool wasActive = workbench.activeSelf;
        if (!wasActive)
        {
            workbench.SetActive(true);
        }
        
        // Verify workbench is active before adding component
        if (!workbench.activeSelf)
        {
            Debug.LogError($"WorkbenchPickup: Workbench '{workbench.name}' failed to activate! Cannot add WorkbenchPlacement.");
            return; // Exit early if we can't activate
        }
        
        // Add WorkbenchPlacement script so it can be placed again
        WorkbenchPlacement placement = workbench.AddComponent<WorkbenchPlacement>();
        
        if (placement == null)
        {
            Debug.LogError($"WorkbenchPickup: Failed to add WorkbenchPlacement component to '{workbench.name}'!");
        }
        else
        {
            // Initialize references immediately so placement works when workbench is selected
            placement.InitializeReferences();
            placement.enabled = true;
            
            // Verify component was added correctly
            WorkbenchPlacement verify = workbench.GetComponent<WorkbenchPlacement>();
            if (verify == null)
            {
                Debug.LogError($"WorkbenchPickup: WorkbenchPlacement component not found after adding to '{workbench.name}'!");
            }
            else
            {
                Debug.Log($"WorkbenchPickup: Added WorkbenchPlacement component to '{workbench.name}' - workbench restored to original state. Component verified: {verify != null}, Enabled: {verify.enabled}");
            }
        }
        
        // Don't deactivate here - let the calling code handle it after pickup is complete
        
        // Remove this WorkbenchPickup component (since workbench is being picked up, it no longer needs WorkbenchPickup)
        // We'll destroy it after the frame ends to avoid issues
        StartCoroutine(DestroyWorkbenchPickupComponent());
    }
    
    System.Collections.IEnumerator DestroyWorkbenchPickupComponent()
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

