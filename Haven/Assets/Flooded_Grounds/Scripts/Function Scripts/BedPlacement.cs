using UnityEngine;

public class BedPlacement : MonoBehaviour
{
    [Header("Placement Settings")]
    public float placementRange = 5f;     // How far away you can place the bed
    public float placementOffset = 0.1f;  // Offset from ground to prevent clipping
    public LayerMask groundLayer = ~0;    // Layer mask for what counts as ground (defaults to everything)
    public KeyCode placeKey = KeyCode.E;  // Key to place the bed

    private Camera playerCamera;
    private HotbarManager hotbarManager;
    private InventoryManager inventoryManager;

    void Awake()
    {
        // Initialize in Awake so references are ready before Start/Update
        playerCamera = Camera.main;
        hotbarManager = FindObjectOfType<HotbarManager>();
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    void Start()
    {
        // Re-check references in Start in case they weren't available in Awake
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("BedPlacement: No main camera found! Ensure your main camera has the 'MainCamera' tag.");
            }
        }

        if (hotbarManager == null)
        {
            hotbarManager = FindObjectOfType<HotbarManager>();
            if (hotbarManager == null)
            {
                Debug.LogError("BedPlacement: HotbarManager not found in scene!");
            }
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
            if (inventoryManager == null)
            {
                Debug.LogError("BedPlacement: InventoryManager not found in scene!");
            }
        }
    }
    
    void OnEnable()
    {
        // Re-check references when the bed becomes active (e.g., when selected in hotbar)
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (hotbarManager == null)
        {
            hotbarManager = FindObjectOfType<HotbarManager>();
        }
        
        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
        }
    }

    void Update()
    {
        // Ensure references are set (in case they weren't initialized properly)
        if (hotbarManager == null)
        {
            hotbarManager = FindObjectOfType<HotbarManager>();
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Only process input if this bed GameObject is the one currently held
        if (hotbarManager != null && playerCamera != null)
        {
            GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
            bool isCurrentItem = (currentItem == this.gameObject);
            
            // Also check by name in case object reference comparison fails
            if (!isCurrentItem && currentItem != null && this.gameObject != null)
            {
                // Check if names match (fallback for reference comparison issues)
                isCurrentItem = (currentItem.name == this.gameObject.name || 
                                currentItem.name.Replace("(Clone)", "") == this.gameObject.name.Replace("(Clone)", ""));
            }
            
            if (Input.GetKeyDown(placeKey))
            {
                Debug.Log($"BedPlacement: E key pressed. Current item in slot {hotbarManager.selectedSlot}: {(currentItem != null ? currentItem.name : "null")}, This bed: {this.gameObject.name}, Match: {isCurrentItem}, Active: {gameObject.activeSelf}, ActiveInHierarchy: {gameObject.activeInHierarchy}");
            }
            
            if (isCurrentItem)
            {
                if (Input.GetKeyDown(placeKey))
                {
                    Debug.Log($"BedPlacement: Place key pressed. Bed {this.gameObject.name} is in slot {hotbarManager.selectedSlot}");
                    TryPlaceBed();
                }
            }
        }
        else if (Input.GetKeyDown(placeKey))
        {
            Debug.LogWarning($"BedPlacement: Cannot place - HotbarManager: {(hotbarManager != null ? "OK" : "NULL")}, Camera: {(playerCamera != null ? "OK" : "NULL")}");
        }
    }

    void TryPlaceBed()
    {
        Debug.Log("BedPlacement: TryPlaceBed called.");
        
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen

        Debug.DrawRay(ray.origin, ray.direction * placementRange, Color.blue, 1f);

        if (Physics.Raycast(ray, out hit, placementRange, groundLayer))
        {
            Debug.Log($"BedPlacement: Raycast hit {hit.collider.gameObject.name} at position {hit.point}.");
            
            // Calculate placement position (on the surface with a small offset)
            Vector3 placementPos = hit.point + hit.normal * placementOffset;
            
            // Align bed to surface normal - this ensures it lies flat on the surface
            Quaternion placementRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            // Create a new instance of the bed prefab for placement
            GameObject placedBed = Instantiate(gameObject, placementPos, placementRot);
            placedBed.name = gameObject.name + "_Placed";
            
            // Enable physics and collider for the placed bed
            Rigidbody rb = placedBed.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true; // Keep it static
                rb.useGravity = false;
            }
            
            Collider col = placedBed.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
            
            // Make sure placed bed is active
            placedBed.SetActive(true);
            
            // Add BedInteraction component if not already present
            if (placedBed.GetComponent<BedInteraction>() == null)
            {
                placedBed.AddComponent<BedInteraction>();
            }
            
            // Add BedPickup component if not already present
            if (placedBed.GetComponent<BedPickup>() == null)
            {
                placedBed.AddComponent<BedPickup>();
            }
            
            // Remove BedPlacement component from placed bed (only the held one should have it)
            BedPlacement placementScript = placedBed.GetComponent<BedPlacement>();
            if (placementScript != null)
            {
                Destroy(placementScript);
            }
            
            Debug.Log($"BedPlacement: Bed placed at {placementPos}.");
            
            // Remove bed from hotbar/inventory after placing
            if (hotbarManager != null)
            {
                hotbarManager.ClearCurrentHotbarSlot();
            }
            
            // Destroy the held bed instance
            Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("BedPlacement: Raycast hit nothing within range. Cannot place bed.");
        }
    }
}

