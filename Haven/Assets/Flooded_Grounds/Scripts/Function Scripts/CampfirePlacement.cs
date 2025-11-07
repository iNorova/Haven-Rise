using UnityEngine;
using System.Collections;

public class CampfirePlacement : MonoBehaviour
{
    [Header("Placement Settings")]
    public float placementRange = 5f;     // How far away you can place the campfire
    public float placementOffset = 0.1f;  // Offset from ground to prevent clipping
    public LayerMask groundLayer = ~0;    // Layer mask for what counts as ground (defaults to everything)
    public KeyCode dropKey = KeyCode.Q;   // Q key to drop the campfire (remove from hotbar)
    
    [Header("Preview Settings")]
    [Tooltip("Show preview outline when campfire is selected.")]
    public bool showPreview = true; // Enabled by default
    [Tooltip("How often to update preview (higher = less lag but less responsive).")]
    [Range(0.1f, 1f)]
    public float previewUpdateInterval = 0.3f; // Update every 0.3 seconds for performance
    
    private Camera playerCamera;
    private HotbarManager hotbarManager;
    private InventoryManager inventoryManager;
    
    // Public method to initialize references (needed when campfire is picked up and re-added)
    public void InitializeReferences()
    {
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
    
    // Preview system
    private GameObject previewCampfire;
    private bool isValidPlacement = false;
    private bool lastValidityState = false; // Cache previous validity to avoid unnecessary material updates
    private Vector3 previewPosition;
    private Quaternion previewRotation;
    private float previewUpdateTimer = 0f;
    private bool loggedInvalidPlacement = false; // Track if we've logged invalid placement for this campfire
    
    // Cached components for performance
    private Renderer[] cachedRenderers;
    private Material[] cachedMaterials;
    
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
        }

        if (hotbarManager == null)
        {
            hotbarManager = FindObjectOfType<HotbarManager>();
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindObjectOfType<InventoryManager>();
        }
        
        // Don't create preview in Start - wait until campfire is actually selected
    }
    
    void OnEnable()
    {
        // Always initialize references when enabled (critical for picked-up campfires)
        InitializeReferences();
        
        // Reset state when enabled (important for re-picking up campfires)
        isValidPlacement = false;
        lastValidityState = false;
        previewUpdateTimer = 0f;
        loggedInvalidPlacement = false; // Reset debug log flag
        
        // Clear and reset preview when enabled (will be recreated when campfire is selected)
        if (previewCampfire != null)
        {
            Destroy(previewCampfire);
            previewCampfire = null;
            cachedRenderers = null;
            cachedMaterials = null;
        }
        
        // Small delay to ensure HotbarManager has activated the campfire before we check
        StartCoroutine(DelayedInitialization());
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        // Wait one frame to ensure campfire is fully activated by HotbarManager
        yield return null;
        
        // Re-initialize references after activation
        InitializeReferences();
        
        // Verify we're the selected item (helps with picked-up campfires)
        if (hotbarManager != null)
        {
            GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
            if (currentItem == this.gameObject)
            {
                Debug.Log($"[CampfirePlacement] Campfire '{this.gameObject.name}' activated and detected as selected item");
            }
        }
    }
    
    void OnDisable()
    {
        // Hide preview when campfire is deselected
        HidePreview();
    }

    void Update()
    {
        // Cache expensive lookups - only check once per frame max
        if (hotbarManager == null)
        {
            hotbarManager = FindObjectOfType<HotbarManager>();
            if (hotbarManager == null) return; // Exit early if critical component missing
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return; // Exit early if critical component missing
        }
        
        // Only process input if this campfire GameObject is the one currently held
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        bool isCurrentItem = (currentItem == this.gameObject);
        
        // Also check by name and component (important for picked-up campfires with different clone names)
        if (!isCurrentItem && currentItem != null && this.gameObject != null)
        {
            // Direct component check - if current item has THIS CampfirePlacement component, it's the active one
            CampfirePlacement itemPlacement = currentItem.GetComponent<CampfirePlacement>();
            if (itemPlacement == this)
            {
                isCurrentItem = true;
            }
            // Fallback: name matching (for edge cases)
            else if (itemPlacement == null)
            {
                // Normalize names by removing common suffixes
                string currentName = currentItem.name.Replace("(Clone)", "").Replace("_Placed", "").Trim();
                string thisName = this.gameObject.name.Replace("(Clone)", "").Replace("_Placed", "").Trim();
                
                // Check if names match and this object has CampfirePlacement
                if ((currentName == thisName || (currentName.Contains("Campfire") && thisName.Contains("Campfire"))) 
                    && this.GetComponent<CampfirePlacement>() != null)
                {
                    isCurrentItem = true;
                }
            }
        }
        
        if (isCurrentItem)
        {
            // Debug log to verify campfire is detected (reduced frequency to avoid spam)
            if (Time.frameCount % 300 == 0) // Log every ~5 seconds
            {
                // Only log if there's an issue
                if (previewCampfire == null && showPreview)
                {
                    Debug.LogWarning($"[CampfirePlacement] Campfire '{this.gameObject.name}' selected but preview not created. ShowPreview={showPreview}");
                }
            }
            
            // Ensure preview is created if needed (only once, works for picked-up campfires too)
            if (previewCampfire == null && showPreview)
            {
                Debug.Log($"[CampfirePlacement] Creating preview for campfire '{this.gameObject.name}'");
                CreatePreview();
                
                // Force an immediate update after creating preview
                if (previewCampfire != null)
                {
                    UpdatePreview();
                }
            }
            
            // Always update placement check (needed for left-click placement)
            // This updates previewPosition, previewRotation, and isValidPlacement
            bool canPlace = CheckPlacement();
            
            // Debug placement state (only on first invalid placement to avoid spam)
            if (!isValidPlacement && !loggedInvalidPlacement)
            {
                Debug.Log($"[CampfirePlacement] Invalid placement detected. Position={previewPosition}");
                loggedInvalidPlacement = true;
            }
            else if (isValidPlacement)
            {
                loggedInvalidPlacement = false; // Reset when valid
            }
            
            // Update preview visual (throttled for performance)
            if (showPreview && previewCampfire != null)
            {
                // Update expensive operations (material changes) at intervals
                previewUpdateTimer += Time.deltaTime;
                if (previewUpdateTimer >= previewUpdateInterval)
                {
                    previewUpdateTimer = 0f;
                    UpdatePreview(); // Updates visual based on current placement state
                }
                else if (previewCampfire.activeSelf)
                {
                    // Keep position smooth between updates (already calculated by CheckPlacement)
                    previewCampfire.transform.position = previewPosition;
                    previewCampfire.transform.rotation = previewRotation;
                }
            }
            
            // Q key to drop (remove from hotbar)
            if (Input.GetKeyDown(dropKey))
            {
                Debug.Log("[CampfirePlacement] Q key pressed - dropping campfire");
                DropCampfire();
                return; // Exit after drop
            }
            
            // Left click to place - always check placement on click (double-check)
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"[CampfirePlacement] Left click detected. isValidPlacement={isValidPlacement}");
                
                // Re-check placement on click to ensure it's current
                bool clickPlacementCheck = CheckPlacement();
                Debug.Log($"[CampfirePlacement] Click placement check result: {clickPlacementCheck}, isValidPlacement={isValidPlacement}");
                
                if (isValidPlacement)
                {
                    Debug.Log("<color=green>[CampfirePlacement] ✓ CAMPFIRE CAN BE PLACED - Attempting placement...</color>");
                    PlaceCampfire();
                }
                else
                {
                    Debug.LogWarning("<color=red>[CampfirePlacement] ✗ CAMPFIRE CANNOT BE PLACED - Invalid placement location!</color>");
                    Debug.LogWarning($"[CampfirePlacement] Placement check failed. Check result: {clickPlacementCheck}, Valid state: {isValidPlacement}");
                    Debug.LogWarning($"[CampfirePlacement] Preview position: {previewPosition}, Make sure you're looking at valid ground.");
                }
            }
        }
        else
        {
            // Hide preview when campfire is not selected
            if (previewCampfire != null && previewCampfire.activeSelf)
            {
                HidePreview();
            }
        }
    }
    
    void CreatePreview()
    {
        // Only create if not already created
        if (previewCampfire != null)
        {
            return;
        }
        
        // Safety check - prevent creating preview if this is already a preview
        if (gameObject.name.Contains("Preview") || gameObject.name.Contains("preview"))
        {
            return;
        }
        
        // If preview is disabled, don't create it
        if (!showPreview)
        {
            return;
        }
        
        // Ensure this gameObject is active for instantiation
        bool wasActive = gameObject.activeSelf;
        if (!wasActive)
        {
            gameObject.SetActive(true);
        }
        
        try
        {
            // Create a preview instance of the campfire
            previewCampfire = Instantiate(gameObject, Vector3.zero, Quaternion.identity);
            previewCampfire.name = "CampfirePreview";
            
            // Remove CampfirePlacement from preview FIRST (before any other operations)
            CampfirePlacement previewPlacement = previewCampfire.GetComponent<CampfirePlacement>();
            if (previewPlacement != null)
            {
                Destroy(previewPlacement); // Destroy, don't just disable
            }
            
            // Disable all scripts on preview (except renderers)
            MonoBehaviour[] scripts = previewCampfire.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this && !(script is Renderer))
                {
                    script.enabled = false;
                }
            }
            
            // Disable all colliders on preview
            Collider[] colliders = previewCampfire.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
            
            // Disable rigidbody if present
            Rigidbody rb = previewCampfire.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Cache renderers and materials for faster updates
            cachedRenderers = previewCampfire.GetComponentsInChildren<Renderer>();
            if (cachedRenderers != null && cachedRenderers.Length > 0)
            {
                cachedMaterials = new Material[cachedRenderers.Length];
                for (int i = 0; i < cachedRenderers.Length; i++)
                {
                    if (cachedRenderers[i] != null && cachedRenderers[i].material != null)
                    {
                        cachedMaterials[i] = cachedRenderers[i].material;
                    }
                }
            }
            
            // Make preview invisible but keep outline visible (simplified)
            SetupPreviewMaterialsSimple();
            
            previewCampfire.SetActive(false); // Start hidden
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CampfirePlacement: Error creating preview: {e.Message}");
            if (previewCampfire != null)
            {
                Destroy(previewCampfire);
                previewCampfire = null;
            }
        }
        finally
        {
            // Restore original active state if it was inactive
            if (!wasActive)
            {
                gameObject.SetActive(false);
            }
        }
    }
    
    void SetupPreviewMaterialsSimple()
    {
        // Create visible outline effect for preview
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;
        
        // Set materials to be semi-transparent with emission for better visibility
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null && cachedRenderers[i].material != null)
            {
                Material mat = cachedRenderers[i].material;
                
                // Make material semi-transparent with glow effect
                if (mat.HasProperty("_Color"))
                {
                    Color col = mat.color;
                    col.a = 0.6f; // Semi-transparent
                    mat.color = col;
                }
                
                // Enable emission for outline/glow effect (makes it more visible)
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 0.2f, 1f) * 0.5f); // Green glow
                    mat.EnableKeyword("_EMISSION");
                }
                
                // Enable render queue for transparency
                if (mat.HasProperty("_Mode"))
                {
                    mat.SetFloat("_Mode", 3); // Set to transparent mode if supported
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000; // Transparent queue
                }
            }
        }
    }
    
    bool CheckPlacement()
    {
        // Optimized placement check - works with or without preview
        if (playerCamera == null)
        {
            Debug.LogWarning("[CampfirePlacement] CheckPlacement: playerCamera is null!");
            isValidPlacement = false;
            return false;
        }
        
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen
        
        // Try raycast with ground layer first (faster if layer mask is set)
        bool hitGround = Physics.Raycast(ray, out hit, placementRange, groundLayer);
        
        // Fallback if layer mask didn't hit anything
        if (!hitGround)
        {
            hitGround = Physics.Raycast(ray, out hit, placementRange);
        }
        
        // Quick filter for invalid objects (using cached name check)
        if (hitGround)
        {
            GameObject hitObj = hit.collider.gameObject;
            // Fast tag check first
            if (hitObj.CompareTag("Player"))
            {
                hitGround = false;
            }
            // Name check only if needed (slower, so check last)
            else
            {
                string objName = hitObj.name;
                if (objName.IndexOf("campfire", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                    objName.IndexOf("preview", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hitGround = false;
                }
            }
        }
        
        if (hitGround)
        {
            // Check if placement is valid
            isValidPlacement = IsValidPlacement(hit);
            
            // Calculate placement position (optimized)
            previewPosition = hit.collider is TerrainCollider 
                ? hit.point 
                : hit.point + hit.normal * placementOffset;
            
            // Calculate placement rotation (optimized)
            float angle = Vector3.Angle(hit.normal, Vector3.up);
            previewRotation = (hit.collider is TerrainCollider || angle < 5f)
                ? Quaternion.identity
                : Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            // Debug placement status (only log when state changes to avoid spam)
            if (isValidPlacement != lastValidityState)
            {
                if (isValidPlacement)
                {
                    Debug.Log($"<color=green>[CampfirePlacement] ✓ Valid placement location found at {previewPosition}</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[CampfirePlacement] ⚠ Invalid placement location at {previewPosition} (surface too steep or invalid)</color>");
                }
            }
            
            return isValidPlacement;
        }
        
        // No valid ground found
        isValidPlacement = false;
        if (lastValidityState != false) // Only log if state changed
        {
            Debug.LogWarning($"<color=red>[CampfirePlacement] ✗ No valid ground found for placement (range: {placementRange})</color>");
        }
        return false;
    }
    
    void UpdatePreview()
    {
        // Early exit checks
        if (!showPreview || previewCampfire == null || playerCamera == null)
        {
            return;
        }
        
        // Use the same placement check logic
        bool wasValid = isValidPlacement;
        CheckPlacement(); // Updates previewPosition, previewRotation, and isValidPlacement
        
        // Update preview visual
        if (previewCampfire != null)
        {
            if (isValidPlacement)
            {
                previewCampfire.transform.position = previewPosition;
                previewCampfire.transform.rotation = previewRotation;
                previewCampfire.SetActive(true);
                
                // Update preview color based on validity (only when state changes)
                if (isValidPlacement != lastValidityState)
                {
                    UpdatePreviewColor(isValidPlacement);
                    lastValidityState = isValidPlacement;
                }
            }
            else
            {
                HidePreview();
            }
        }
    }
    
    bool IsValidPlacement(RaycastHit hit)
    {
        // Check if surface is not too steep
        float angle = Vector3.Angle(hit.normal, Vector3.up);
        if (angle > 45f)
        {
            return false; // Too steep
        }
        
        // Check if there's enough space for the campfire (optional - you can add bounds checking)
        // For now, just check if it's a valid surface
        return true;
    }
    
    void UpdatePreviewColor(bool valid)
    {
        // Optimized color update using cached renderers
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;
        
        Color targetColor = valid ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f); // Green or Red with better visibility
        
        // Only update materials that support _Color property (fastest path)
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null && cachedRenderers[i].material != null)
            {
                Material mat = cachedRenderers[i].material;
                
                // Only update if material has _Color property (most common)
                if (mat.HasProperty("_Color"))
                {
                    mat.color = targetColor;
                    
                    // Enable emission for better outline visibility (optional, can disable if causes issues)
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", targetColor * 0.5f);
                        mat.EnableKeyword("_EMISSION");
                    }
                }
            }
        }
    }
    
    void HidePreview()
    {
        if (previewCampfire != null)
        {
            previewCampfire.SetActive(false);
        }
        isValidPlacement = false;
        lastValidityState = false; // Reset cache
    }
    
    void PlaceCampfire()
    {
        if (!isValidPlacement)
        {
            Debug.LogError("<color=red>[CampfirePlacement] ✗ CAMPFIRE PLACEMENT FAILED - Invalid placement location!</color>");
            Debug.LogError($"[CampfirePlacement] Cannot place campfire at {previewPosition}. isValidPlacement is false.");
            return;
        }
        
        Debug.Log("<color=green>[CampfirePlacement] ✓ PLACING CAMPFIRE...</color>");
        Debug.Log($"[CampfirePlacement] Position: {previewPosition}, Rotation: {previewRotation}");
        
        // Create the actual placed campfire
        GameObject placedCampfire = Instantiate(gameObject, previewPosition, previewRotation);
        placedCampfire.name = gameObject.name + "_Placed";
        
        // Enable physics and collider for the placed campfire
        Rigidbody rb = placedCampfire.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Keep it static
            rb.useGravity = false;
        }
        
        Collider[] colliders = placedCampfire.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        // Make sure placed campfire is on a layer that can be raycasted (not "Ignore Raycast")
        // Try to use "Pickup" layer if it exists, otherwise use "Default"
        int pickupLayer = LayerMask.NameToLayer("Pickup");
        if (pickupLayer == -1)
        {
            pickupLayer = LayerMask.NameToLayer("Default");
        }
        placedCampfire.layer = pickupLayer;
        
        // Also set all children to the same layer
        foreach (Transform child in placedCampfire.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = pickupLayer;
        }
        
        // Make sure placed campfire is active
        placedCampfire.SetActive(true);
        
        // Add CampfirePickup component if not already present
        if (placedCampfire.GetComponent<CampfirePickup>() == null)
        {
            placedCampfire.AddComponent<CampfirePickup>();
            Debug.Log("CampfirePlacement: Added CampfirePickup component to placed campfire.");
        }
        
        // Add or copy CampfireFuel component - ensure it has the same recipes as the original
        CampfireFuel originalFuel = gameObject.GetComponent<CampfireFuel>();
        CampfireFuel placedFuel = placedCampfire.GetComponent<CampfireFuel>();
        
        if (placedFuel == null)
        {
            placedFuel = placedCampfire.AddComponent<CampfireFuel>();
            Debug.Log("CampfirePlacement: Added CampfireFuel component to placed campfire.");
        }
        
        // Copy cooking recipes from original campfire if it has them
        if (originalFuel != null && originalFuel.cookingRecipes != null && originalFuel.cookingRecipes.Length > 0)
        {
            placedFuel.cookingRecipes = new CookingRecipe[originalFuel.cookingRecipes.Length];
            for (int i = 0; i < originalFuel.cookingRecipes.Length; i++)
            {
                placedFuel.cookingRecipes[i] = new CookingRecipe
                {
                    inputItemName = originalFuel.cookingRecipes[i].inputItemName,
                    cookedItemPrefab = originalFuel.cookingRecipes[i].cookedItemPrefab,
                    cookingTime = originalFuel.cookingRecipes[i].cookingTime,
                    requiresFireLit = originalFuel.cookingRecipes[i].requiresFireLit
                };
            }
            Debug.Log($"CampfirePlacement: Copied {placedFuel.cookingRecipes.Length} cooking recipes to placed campfire.");
        }
        
        // Copy other important settings from original
        if (originalFuel != null)
        {
            placedFuel.maxFuel = originalFuel.maxFuel;
            placedFuel.fuelPerLog = originalFuel.fuelPerLog;
            placedFuel.fuelConsumptionRate = originalFuel.fuelConsumptionRate;
            placedFuel.addWoodSfx = originalFuel.addWoodSfx;
            placedFuel.fireLitSfx = originalFuel.fireLitSfx;
            placedFuel.campfireLoopSfx = originalFuel.campfireLoopSfx;
            placedFuel.cookingStartSfx = originalFuel.cookingStartSfx;
            placedFuel.cookingCompleteSfx = originalFuel.cookingCompleteSfx;
            Debug.Log("CampfirePlacement: Copied CampfireFuel settings to placed campfire.");
        }
        
        // Remove CampfirePlacement component from placed campfire (only the held one should have it)
        CampfirePlacement placementScript = placedCampfire.GetComponent<CampfirePlacement>();
        if (placementScript != null)
        {
            Destroy(placementScript);
        }
        
        // Remove campfire from hotbar/inventory after placing
        if (hotbarManager != null)
        {
            hotbarManager.ClearCurrentHotbarSlot();
        }
        
        // Destroy the held campfire instance and preview
        HidePreview();
        if (previewCampfire != null)
        {
            Destroy(previewCampfire);
        }
        Destroy(this.gameObject);
        
        // SUCCESS MESSAGE
        Debug.Log($"<color=green>[CampfirePlacement] ✓✓✓ CAMPFIRE SUCCESSFULLY PLACED! ✓✓✓</color>");
        Debug.Log($"<color=green>[CampfirePlacement] Campfire '{placedCampfire.name}' placed at position: {previewPosition}</color>");
    }
    
    void DropCampfire()
    {
        Debug.Log("CampfirePlacement: Dropping campfire (removing from hotbar).");
        
        // Remove from hotbar
        if (hotbarManager != null)
        {
            hotbarManager.ClearCurrentHotbarSlot();
        }
        
        // Hide preview
        HidePreview();
        
        // Destroy preview and campfire
        if (previewCampfire != null)
        {
            Destroy(previewCampfire);
        }
        Destroy(this.gameObject);
    }
    
    void OnDestroy()
    {
        // Clean up preview when campfire is destroyed
        if (previewCampfire != null)
        {
            Destroy(previewCampfire);
        }
    }
}

