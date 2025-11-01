using UnityEngine;

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
        Debug.Log("BedPickup: Picking up bed.");

        // Check if bed is currently being slept in
        BedInteraction bedInteraction = GetComponent<BedInteraction>();
        if (bedInteraction != null && bedInteraction.isSleeping)
        {
            Debug.Log("BedPickup: Cannot pick up bed while sleeping. Wake up first!");
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
                // Clone the bed for pickup
                GameObject bedClone = Instantiate(bedToPickup);
                
                // Clean up the name - remove "_Placed" suffix if present
                string originalName = bedToPickup.name;
                string cleanName = originalName.Replace("_Placed", "");
                if (originalName.Contains("(Clone)"))
                {
                    cleanName = cleanName.Replace("(Clone)", "");
                }
                bedClone.name = cleanName;
                
                Debug.Log($"BedPickup: Cloned bed '{originalName}' -> '{cleanName}'");
                
                // Remove placement/interaction scripts from the clone (it's going to inventory)
                BedInteraction interaction = bedClone.GetComponent<BedInteraction>();
                if (interaction != null)
                {
                    Destroy(interaction);
                    Debug.Log("BedPickup: Removed BedInteraction from clone.");
                }
                
                BedPickup pickup = bedClone.GetComponent<BedPickup>();
                if (pickup != null)
                {
                    Destroy(pickup);
                    Debug.Log("BedPickup: Removed BedPickup from clone.");
                }
                
                // Remove any existing BedPlacement (shouldn't be there, but just in case)
                BedPlacement existingPlacement = bedClone.GetComponent<BedPlacement>();
                if (existingPlacement != null)
                {
                    Destroy(existingPlacement);
                    Debug.LogWarning("BedPickup: Found existing BedPlacement on clone, removing it.");
                }
                
                // Add BedPlacement script so it can be placed again
                BedPlacement placement = bedClone.AddComponent<BedPlacement>();
                Debug.Log($"BedPickup: Added BedPlacement component to '{bedClone.name}'.");
                
                // Verify the component was added
                if (bedClone.GetComponent<BedPlacement>() == null)
                {
                    Debug.LogError("BedPickup: Failed to add BedPlacement component!");
                }

                // Ensure bed is inactive before pickup (HotbarManager expects this)
                bedClone.SetActive(false);
                
                // Pickup into hotbar
                hotbarManager.PickupItem(bedClone, emptySlot);
                pickedUp = true;
                Debug.Log($"BedPickup: Bed '{bedClone.name}' picked up into hotbar slot {emptySlot}. Active: {bedClone.activeSelf}, ActiveInHierarchy: {bedClone.activeInHierarchy}");
            }
        }

        // Try inventory if hotbar is full
        if (!pickedUp && inventoryManager != null)
        {
            GameObject bedClone = Instantiate(bedToPickup);
            
            // Clean up the name - remove "_Placed" suffix if present
            string originalName = bedToPickup.name;
            string cleanName = originalName.Replace("_Placed", "");
            if (originalName.Contains("(Clone)"))
            {
                cleanName = cleanName.Replace("(Clone)", "");
            }
            bedClone.name = cleanName;
            
            // Remove placement/interaction scripts
            BedInteraction interaction = bedClone.GetComponent<BedInteraction>();
            if (interaction != null)
            {
                Destroy(interaction);
            }
            
            BedPickup pickup = bedClone.GetComponent<BedPickup>();
            if (pickup != null)
            {
                Destroy(pickup);
            }
            
            // Remove any existing BedPlacement
            BedPlacement existingPlacement = bedClone.GetComponent<BedPlacement>();
            if (existingPlacement != null)
            {
                Destroy(existingPlacement);
            }
            
            // Add BedPlacement script so it can be placed again
            BedPlacement placement = bedClone.AddComponent<BedPlacement>();
            Debug.Log($"BedPickup: Added BedPlacement component to picked up bed '{bedClone.name}' (inventory).");

            if (inventoryManager.AddItem(bedClone))
            {
                pickedUp = true;
                Debug.Log("BedPickup: Bed picked up into inventory.");
            }
            else
            {
                Destroy(bedClone);
            }
        }

        // If successfully picked up, destroy the placed bed
        if (pickedUp)
        {
            Destroy(bedToPickup);
        }
        else
        {
            Debug.Log("BedPickup: Cannot pick up bed - inventory and hotbar are full.");
        }
    }

    // Visualize interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}

