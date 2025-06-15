using UnityEngine;

public class SeedInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 3f;     // How close player needs to be to plant
    public LayerMask soilLayer;             // Layer for soil objects
    public KeyCode plantKey = KeyCode.F;    // Key to plant the seed

    private TreePlantingSystem treePlantingSystem;
    private Camera playerCamera;

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

        // The script is disabled by default and enabled by HotbarManager when held.
        // If this Update runs, it means it's held.
        Debug.Log("SeedInteraction: Script initialized.");
    }

    void Update()
    {
        // Debug.Log("SeedInteraction: Update method is running."); // Uncomment for debugging if needed

        if (Input.GetKeyDown(plantKey))
        {
            TryPlantSeed();
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

                // After successful planting, destroy this seed instance (the one in player's hand)
                // Or, if using an inventory system, remove it from inventory.
                // For now, destroy the GameObject if it's not managed by inventory.
                Destroy(this.gameObject);
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