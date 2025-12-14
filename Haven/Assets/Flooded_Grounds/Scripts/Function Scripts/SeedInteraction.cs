using UnityEngine;

public class SeedInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;     // How close player needs to be to plant
    public LayerMask soilLayer;             // Layer for soil objects
    public KeyCode plantKey = KeyCode.F;    // Key to plant the seed

    private TreePlantingSystem treePlantingSystem;
    private Camera playerCamera;
    private HotbarManager hotbarManager;

    void Start()
    {
        treePlantingSystem = FindObjectOfType<TreePlantingSystem>();
        if (treePlantingSystem == null)
        {
            Debug.LogError("SeedInteraction: TreePlantingSystem not found in scene! Make sure it's present.");
        }

        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("SeedInteraction: No main camera found! Ensure your main camera has the 'MainCamera' tag.");
        }

        hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager == null)
        {
            Debug.LogError("SeedInteraction: HotbarManager not found in scene! Hotbar updates will not work.");
        }

        // The script is disabled by default and enabled by HotbarManager when held.
        // If this Update runs, it means it's held.
        Debug.Log("SeedInteraction: Script initialized.");
    }

    void Update()
    {
        // Only process input if this seed GameObject is the one currently held in the active hotbar slot.
        // This prevents planting when the seed is not equipped or has been consumed/destroyed.
        if (hotbarManager != null && hotbarManager.GetItem(hotbarManager.selectedSlot) == this.gameObject)
        {
            if (Input.GetKeyDown(plantKey))
            {
                TryPlantSeed();
            }
        }
    }

    void TryPlantSeed()
    {
        Debug.Log("SeedInteraction: TryPlantSeed called.");
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.green, 1f);

        if (Physics.Raycast(ray, out hit, interactionRange, soilLayer))
        {
            Debug.Log($"SeedInteraction: Raycast hit {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}.");
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Soil"))
            {
                Debug.Log("SeedInteraction: Hit object is tagged as Soil. Requesting TreePlantingSystem to plant.");
                treePlantingSystem.PlantSeedOnSoil(hitObject);

                // Consume one seed from the stack (similar to CampfireFuel)
                if (hotbarManager != null)
                {
                    InventorySlot slot = hotbarManager.hotbarSlots[hotbarManager.selectedSlot];
                    int stackCount = slot.GetStackCount();
                    
                    if (stackCount > 1)
                    {
                        // Decrement stack count - only consume one seed
                        slot.SetStackCount(stackCount - 1);
                        hotbarManager.UpdateHotbarUI();
                        Debug.Log($"SeedInteraction: Consumed 1 seed from stack. Remaining: {stackCount - 1}");
                    }
                    else
                    {
                        // Stack count is 1, so remove the item entirely
                        hotbarManager.ClearCurrentHotbarSlot();
                        Destroy(this.gameObject);
                        Debug.Log("SeedInteraction: Last seed consumed, slot cleared.");
                    }
                }
                else
                {
                    // Fallback if hotbarManager is null
                    Destroy(this.gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"SeedInteraction: Hit object {hitObject.name} is on soil layer but NOT tagged as \"Soil\". Its tag is: {hitObject.tag}");
            }
        }
        else
        {
            Debug.Log("SeedInteraction: Raycast hit nothing on the soil layer within range.");
        }
    }
} 