using UnityEngine;
using System.Collections;

public class BedPlacement : MonoBehaviour
{
    [Header("Placement Settings")]
    public float placementRange = 5f;     // How far away you can place the bed
    public float placementOffset = 0.1f;  // Offset from ground to prevent clipping
    public LayerMask groundLayer = ~0;    // Layer mask for what counts as ground (defaults to everything)
    public KeyCode dropKey = KeyCode.Q;   // Q key to drop the bed (remove from hotbar)
    
    [Header("Preview Settings")]
    [Tooltip("Show preview outline when bed is selected.")]
    public bool showPreview = true; // Enabled by default
    [Tooltip("How often to update preview (higher = less lag but less responsive).")]
    [Range(0.1f, 1f)]
    public float previewUpdateInterval = 0.3f; // Update every 0.3 seconds for performance
    
    private Camera playerCamera;
    private HotbarManager hotbarManager;
    private InventoryManager inventoryManager;
    
    // Public method to initialize references (needed when bed is picked up and re-added)
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
    private GameObject previewBed;
    private bool isValidPlacement = false;
    private bool lastValidityState = false; // Cache previous validity to avoid unnecessary material updates
    private Vector3 previewPosition;
    private Quaternion previewRotation;
    private float previewUpdateTimer = 0f;
    private bool loggedInvalidPlacement = false; // Track if we've logged invalid placement for this bed
    
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
        
        // Don't create preview in Start - wait until bed is actually selected
    }
    
    void OnEnable()
    {
        // Always initialize references when enabled (critical for picked-up beds)
        InitializeReferences();
        
        // Reset state when enabled (important for re-picking up beds)
        isValidPlacement = false;
        lastValidityState = false;
        previewUpdateTimer = 0f;
        loggedInvalidPlacement = false; // Reset debug log flag
        
        // Clear preview when enabled (will be recreated when bed is selected)
        if (previewBed != null)
        {
            HidePreview();
            // Don't destroy preview here - it will be reused if bed is selected again
        }
        
        // Small delay to ensure HotbarManager has activated the bed before we check
        StartCoroutine(DelayedInitialization());
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        // Wait one frame to ensure bed is fully activated by HotbarManager
        yield return null;
        
        // Re-initialize references after activation
        InitializeReferences();
        
        // Verify we're the selected item (helps with picked-up beds)
        if (hotbarManager != null)
        {
            GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
            if (currentItem == this.gameObject)
            {
                Debug.Log($"[BedPlacement] Bed '{this.gameObject.name}' activated and detected as selected item");
            }
        }
    }
    
    void OnDisable()
    {
        // Hide preview when bed is deselected
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
        
        // Only process input if this bed GameObject is the one currently held
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        bool isCurrentItem = (currentItem == this.gameObject);
        
        // Also check by name and component (important for picked-up beds with different clone names)
        if (!isCurrentItem && currentItem != null && this.gameObject != null)
        {
            // Direct component check - if current item has THIS BedPlacement component, it's the active one
            BedPlacement itemPlacement = currentItem.GetComponent<BedPlacement>();
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
                
                // Check if names match and this object has BedPlacement
                if ((currentName == thisName || (currentName.Contains("Bed") && thisName.Contains("Bed"))) 
                    && this.GetComponent<BedPlacement>() != null)
                {
                    isCurrentItem = true;
                }
            }
        }
        
        if (isCurrentItem)
        {
            // Debug log to verify bed is detected (reduced frequency to avoid spam)
            if (Time.frameCount % 300 == 0) // Log every ~5 seconds
            {
                // Only log if there's an issue
                if (previewBed == null && showPreview)
                {
                    Debug.LogWarning($"[BedPlacement] Bed '{this.gameObject.name}' selected but preview not created. ShowPreview={showPreview}");
                }
            }
            
            // Ensure preview is created if needed (only once, works for picked-up beds too)
            if (previewBed == null && showPreview)
            {
                Debug.Log($"[BedPlacement] Creating preview for bed '{this.gameObject.name}'");
                CreatePreview();
            }
            
            // Always update placement check (needed for left-click placement)
            // This updates previewPosition, previewRotation, and isValidPlacement
            bool canPlace = CheckPlacement();
            
            // Debug placement state (only on first invalid placement to avoid spam)
            if (!isValidPlacement && !loggedInvalidPlacement)
            {
                Debug.Log($"[BedPlacement] Invalid placement detected. Position={previewPosition}");
                loggedInvalidPlacement = true;
            }
            else if (isValidPlacement)
            {
                loggedInvalidPlacement = false; // Reset when valid
            }
            
            // Update preview visual (throttled for performance)
            if (showPreview && previewBed != null)
            {
                // Update expensive operations (material changes) at intervals
                previewUpdateTimer += Time.deltaTime;
                if (previewUpdateTimer >= previewUpdateInterval)
                {
                    previewUpdateTimer = 0f;
                    UpdatePreview(); // Updates visual based on current placement state
                }
                else if (previewBed.activeSelf)
                {
                    // Keep position smooth between updates (already calculated by CheckPlacement)
                    previewBed.transform.position = previewPosition;
                    previewBed.transform.rotation = previewRotation;
                }
            }
            
            // Q key to drop (remove from hotbar)
            if (Input.GetKeyDown(dropKey))
            {
                Debug.Log("[BedPlacement] Q key pressed - dropping bed");
                DropBed();
                return; // Exit after drop
            }
            
            // Left click to place - always check placement on click (double-check)
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"[BedPlacement] Left click detected. isValidPlacement={isValidPlacement}");
                
                // Re-check placement on click to ensure it's current
                bool clickPlacementCheck = CheckPlacement();
                Debug.Log($"[BedPlacement] Click placement check result: {clickPlacementCheck}, isValidPlacement={isValidPlacement}");
                
                if (isValidPlacement)
                {
                    Debug.Log("[BedPlacement] Placing bed now...");
                    PlaceBed();
                }
                else
                {
                    Debug.LogWarning($"[BedPlacement] Cannot place bed - Invalid placement. Check: {clickPlacementCheck}, Valid: {isValidPlacement}");
                }
            }
        }
        else
        {
            // Hide preview when bed is not selected
            if (previewBed != null && previewBed.activeSelf)
            {
                HidePreview();
            }
        }
    }
    
    void CreatePreview()
    {
        // Only create if not already created
        if (previewBed != null)
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
            // Create a preview instance of the bed
            previewBed = Instantiate(gameObject, Vector3.zero, Quaternion.identity);
            previewBed.name = "BedPreview";
            
            // Remove BedPlacement from preview FIRST (before any other operations)
            BedPlacement previewPlacement = previewBed.GetComponent<BedPlacement>();
            if (previewPlacement != null)
            {
                Destroy(previewPlacement); // Destroy, don't just disable
            }
            
            // Disable all scripts on preview (except renderers)
            MonoBehaviour[] scripts = previewBed.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this && !(script is Renderer))
                {
                    script.enabled = false;
                }
            }
            
            // Disable all colliders on preview
            Collider[] colliders = previewBed.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
            
            // Disable rigidbody if present
            Rigidbody rb = previewBed.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Cache renderers and materials for faster updates
            cachedRenderers = previewBed.GetComponentsInChildren<Renderer>();
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
            
            previewBed.SetActive(false); // Start hidden
        }
        catch (System.Exception e)
        {
            Debug.LogError($"BedPlacement: Error creating preview: {e.Message}");
            if (previewBed != null)
            {
                Destroy(previewBed);
                previewBed = null;
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
        // Minimal material setup - just tint existing materials slightly
        // Avoids expensive material creation/modification
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;
        
        // Just set initial semi-transparent color - minimal changes
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null && cachedRenderers[i].material != null)
            {
                Material mat = cachedRenderers[i].material;
                
                // Only modify if shader supports _Color (fastest path)
                if (mat.HasProperty("_Color"))
                {
                    Color col = mat.color;
                    col.a = 0.5f; // Semi-transparent
                    mat.color = col;
                }
            }
        }
    }
    
    bool CheckPlacement()
    {
        // Optimized placement check - works with or without preview
        if (playerCamera == null)
        {
            Debug.LogWarning("[BedPlacement] CheckPlacement: playerCamera is null!");
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
                if (objName.IndexOf("bed", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
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
            
            return isValidPlacement;
        }
        
        // No valid ground found
        isValidPlacement = false;
        return false;
    }
    
    void UpdatePreview()
    {
        // Early exit checks
        if (!showPreview || previewBed == null || playerCamera == null)
        {
            return;
        }
        
        // Use the same placement check logic
        bool wasValid = isValidPlacement;
        CheckPlacement(); // Updates previewPosition, previewRotation, and isValidPlacement
        
        // Update preview visual
        if (previewBed != null)
        {
            if (isValidPlacement)
            {
                previewBed.transform.position = previewPosition;
                previewBed.transform.rotation = previewRotation;
                previewBed.SetActive(true);
                
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
        
        // Check if there's enough space for the bed (optional - you can add bounds checking)
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
    
    // Removed SetPreviewAlpha - now handled in SetupPreviewMaterialsSimple
    
    void HidePreview()
    {
        if (previewBed != null)
        {
            previewBed.SetActive(false);
        }
        isValidPlacement = false;
        lastValidityState = false; // Reset cache
    }
    
    void PlaceBed()
    {
        if (!isValidPlacement)
        {
            Debug.LogWarning("BedPlacement: Cannot place bed - invalid placement location.");
            return;
        }
        
        Debug.Log($"BedPlacement: Placing bed at {previewPosition}.");
        
        // Create the actual placed bed
        GameObject placedBed = Instantiate(gameObject, previewPosition, previewRotation);
        placedBed.name = gameObject.name + "_Placed";
        
        // Enable physics and collider for the placed bed
        Rigidbody rb = placedBed.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Keep it static
            rb.useGravity = false;
        }
        
        Collider[] colliders = placedBed.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        // Make sure placed bed is active
        placedBed.SetActive(true);
        
        // Add BedInteraction component if not already present
        BedInteraction bedInteraction = placedBed.GetComponent<BedInteraction>();
        if (bedInteraction == null)
        {
            bedInteraction = placedBed.AddComponent<BedInteraction>();
            Debug.Log("BedPlacement: Added BedInteraction component to placed bed.");
        }
        else
        {
            // If BedInteraction already exists, make sure it initializes properly
            Debug.Log("BedPlacement: Placed bed already has BedInteraction component.");
        }
        
        // Add BedPickup component if not already present
        if (placedBed.GetComponent<BedPickup>() == null)
        {
            placedBed.AddComponent<BedPickup>();
            Debug.Log("BedPlacement: Added BedPickup component to placed bed.");
        }
        
        // Remove BedPlacement component from placed bed (only the held one should have it)
        BedPlacement placementScript = placedBed.GetComponent<BedPlacement>();
        if (placementScript != null)
        {
            Destroy(placementScript);
        }
        
        // Remove bed from hotbar/inventory after placing
        if (hotbarManager != null)
        {
            hotbarManager.ClearCurrentHotbarSlot();
        }
        
        // Destroy the held bed instance and preview
        HidePreview();
        if (previewBed != null)
        {
            Destroy(previewBed);
        }
        Destroy(this.gameObject);
    }
    
    void DropBed()
    {
        Debug.Log("BedPlacement: Dropping bed (removing from hotbar).");
        
        // Remove from hotbar
        if (hotbarManager != null)
        {
            hotbarManager.ClearCurrentHotbarSlot();
        }
        
        // Hide preview
        HidePreview();
        
        // Destroy preview and bed
        if (previewBed != null)
        {
            Destroy(previewBed);
        }
        Destroy(this.gameObject);
    }
    
    void OnDestroy()
    {
        // Clean up preview when bed is destroyed
        if (previewBed != null)
        {
            Destroy(previewBed);
        }
    }
}
