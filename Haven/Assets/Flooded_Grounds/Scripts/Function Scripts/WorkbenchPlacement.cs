using UnityEngine;
using System.Collections;

public class WorkbenchPlacement : MonoBehaviour
{
    [Header("Placement Settings")]
    public float placementRange = 5f;     // How far away you can place the workbench
    [Tooltip("Offset from ground to prevent clipping. Increase if workbench is going underground.")]
    public float placementOffset = 0.5f;  // Offset from ground to prevent clipping (increased for workbench)
    public LayerMask groundLayer = ~0;    // Layer mask for what counts as ground (defaults to everything)
    public KeyCode dropKey = KeyCode.Q;   // Q key to drop the workbench (remove from hotbar)
    [Tooltip("Rotation offset in degrees (X, Y, Z). Adjust this to change how the workbench is oriented when placed.")]
    public Vector3 rotationOffset = Vector3.zero; // Rotation offset that can be adjusted in Inspector
    [Tooltip("Use bounds-based offset to automatically calculate proper height based on workbench size")]
    public bool useBoundsOffset = true; // Automatically calculate offset based on workbench bounds
    
    [Header("Preview Settings")]
    [Tooltip("Show preview outline when workbench is selected.")]
    public bool showPreview = true; // Enabled by default
    [Tooltip("How often to update preview (higher = less lag but less responsive).")]
    [Range(0.1f, 1f)]
    public float previewUpdateInterval = 0.3f; // Update every 0.3 seconds for performance
    
    private Camera playerCamera;
    private HotbarManager hotbarManager;
    private InventoryManager inventoryManager;
    
    // Public method to initialize references (needed when workbench is picked up and re-added)
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
    private GameObject previewWorkbench;
    private bool isValidPlacement = false;
    private bool lastValidityState = false; // Cache previous validity to avoid unnecessary material updates
    private Vector3 previewPosition;
    private Quaternion previewRotation;
    private float previewUpdateTimer = 0f;
    private bool loggedInvalidPlacement = false; // Track if we've logged invalid placement for this workbench
    
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
        
        // Don't create preview in Start - wait until workbench is actually selected
    }
    
    void OnEnable()
    {
        // Always initialize references when enabled (critical for picked-up workbenches)
        InitializeReferences();
        
        // Ensure component is enabled
        this.enabled = true;
        
        // Reset state when enabled (important for re-picking up workbenches)
        isValidPlacement = false;
        lastValidityState = false;
        previewUpdateTimer = 0f;
        loggedInvalidPlacement = false; // Reset debug log flag
        
        // Clear preview when enabled (will be recreated when workbench is selected)
        if (previewWorkbench != null)
        {
            HidePreview();
            // Don't destroy preview here - it will be reused if workbench is selected again
        }
        
        Debug.Log($"[WorkbenchPlacement] OnEnable called for '{this.gameObject.name}'. Active: {this.gameObject.activeSelf}, Enabled: {this.enabled}");
        
        // Small delay to ensure HotbarManager has activated the workbench before we check
        StartCoroutine(DelayedInitialization());
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        // Wait one frame to ensure workbench is fully activated by HotbarManager
        yield return null;
        
        // Re-initialize references after activation
        InitializeReferences();
        
        // Ensure component is enabled
        this.enabled = true;
        
        // Verify we're the selected item (helps with picked-up workbenches)
        if (hotbarManager != null)
        {
            GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
            if (currentItem == this.gameObject)
            {
                Debug.Log($"[WorkbenchPlacement] Workbench '{this.gameObject.name}' activated and detected as selected item. Component enabled: {this.enabled}");
            }
            else if (currentItem != null)
            {
                // Check if it's the same workbench by name (for picked-up workbenches)
                string currentName = currentItem.name.Replace("(Clone)", "").Replace("_Placed", "").Replace("_placed", "").Trim();
                string thisName = this.gameObject.name.Replace("(Clone)", "").Replace("_Placed", "").Replace("_placed", "").Trim();
                if (currentName == thisName || (currentName.ToLower().Contains("workbench") && thisName.ToLower().Contains("workbench")))
                {
                    Debug.Log($"[WorkbenchPlacement] Workbench '{this.gameObject.name}' matched by name to selected item '{currentItem.name}'. Component enabled: {this.enabled}");
                }
            }
        }
    }
    
    void OnDisable()
    {
        // Hide preview when workbench is deselected
        HidePreview();
    }

    void Update()
    {
        // Don't run if this component is disabled or GameObject is inactive
        if (!this.enabled || !this.gameObject.activeSelf) return;
        
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
        
        // Only process input if this workbench GameObject is the one currently held
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        bool isCurrentItem = (currentItem == this.gameObject);
        
        // Also check by name and component (important for picked-up workbenches with different clone names)
        if (!isCurrentItem && currentItem != null && this.gameObject != null)
        {
            // Direct component check - if current item has THIS WorkbenchPlacement component, it's the active one
            WorkbenchPlacement itemPlacement = currentItem.GetComponent<WorkbenchPlacement>();
            if (itemPlacement == this)
            {
                isCurrentItem = true;
            }
            // Fallback: name matching (for edge cases)
            else if (itemPlacement == null)
            {
                // Normalize names by removing common suffixes
                string currentName = currentItem.name.Replace("(Clone)", "").Replace("_Placed", "").Replace("_placed", "").Trim();
                string thisName = this.gameObject.name.Replace("(Clone)", "").Replace("_Placed", "").Replace("_placed", "").Trim();
                
                // Check if names match and this object has WorkbenchPlacement
                if ((currentName == thisName || (currentName.ToLower().Contains("workbench") && thisName.ToLower().Contains("workbench"))) 
                    && this.GetComponent<WorkbenchPlacement>() != null)
                {
                    isCurrentItem = true;
                }
            }
        }
        
        if (isCurrentItem)
        {
            // Ensure this component is enabled (critical for picked-up workbenches)
            if (!this.enabled)
            {
                this.enabled = true;
                Debug.Log($"[WorkbenchPlacement] Re-enabled WorkbenchPlacement for '{this.gameObject.name}'");
            }
            
            // Debug log to verify workbench is detected (reduced frequency to avoid spam)
            if (Time.frameCount % 300 == 0) // Log every ~5 seconds
            {
                // Only log if there's an issue
                if (previewWorkbench == null && showPreview)
                {
                    Debug.LogWarning($"[WorkbenchPlacement] Workbench '{this.gameObject.name}' selected but preview not created. ShowPreview={showPreview}, Enabled={this.enabled}");
                }
            }
            
            // Ensure workbench GameObject is active (should be, but double-check)
            if (!this.gameObject.activeSelf)
            {
                this.gameObject.SetActive(true);
                Debug.Log($"[WorkbenchPlacement] Activated workbench GameObject '{this.gameObject.name}'");
            }
            
            // Ensure preview is created if needed (only once, works for picked-up workbenches too)
            if (previewWorkbench == null && showPreview)
            {
                Debug.Log($"[WorkbenchPlacement] Creating preview for workbench '{this.gameObject.name}' (Active: {this.gameObject.activeSelf}, Enabled: {this.enabled})");
                CreatePreview();
                
                // Force an immediate update after creating preview
                if (previewWorkbench != null)
                {
                    UpdatePreview();
                }
                else
                {
                    Debug.LogError($"[WorkbenchPlacement] Failed to create preview for '{this.gameObject.name}'!");
                }
            }
            
            // Always update placement check (needed for left-click placement)
            // This updates previewPosition, previewRotation, and isValidPlacement
            bool canPlace = CheckPlacement();
            
            // Debug placement state (only on first invalid placement to avoid spam)
            if (!isValidPlacement && !loggedInvalidPlacement)
            {
                Debug.Log($"[WorkbenchPlacement] Invalid placement detected. Position={previewPosition}");
                loggedInvalidPlacement = true;
            }
            else if (isValidPlacement)
            {
                loggedInvalidPlacement = false; // Reset when valid
            }
            
            // Update preview visual (throttled for performance)
            if (showPreview && previewWorkbench != null)
            {
                // Update expensive operations (material changes) at intervals
                previewUpdateTimer += Time.deltaTime;
                if (previewUpdateTimer >= previewUpdateInterval)
                {
                    previewUpdateTimer = 0f;
                    UpdatePreview(); // Updates visual based on current placement state
                }
                else if (previewWorkbench.activeSelf)
                {
                    // Keep position smooth between updates (already calculated by CheckPlacement)
                    previewWorkbench.transform.position = previewPosition;
                    previewWorkbench.transform.rotation = previewRotation;
                }
            }
            
            // Q key to drop (remove from hotbar)
            if (Input.GetKeyDown(dropKey))
            {
                Debug.Log("[WorkbenchPlacement] Q key pressed - dropping workbench");
                DropWorkbench();
                return; // Exit after drop
            }
            
            // Left click to place - always check placement on click (double-check)
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log($"[WorkbenchPlacement] Left click detected. isValidPlacement={isValidPlacement}");
                
                // Re-check placement on click to ensure it's current
                bool clickPlacementCheck = CheckPlacement();
                Debug.Log($"[WorkbenchPlacement] Click placement check result: {clickPlacementCheck}, isValidPlacement={isValidPlacement}");
                
                if (isValidPlacement)
                {
                    Debug.Log("<color=green>[WorkbenchPlacement] ✓ WORKBENCH CAN BE PLACED - Attempting placement...</color>");
                    PlaceWorkbench();
                }
                else
                {
                    Debug.LogWarning("<color=red>[WorkbenchPlacement] ✗ WORKBENCH CANNOT BE PLACED - Invalid placement location!</color>");
                    Debug.LogWarning($"[WorkbenchPlacement] Placement check failed. Check result: {clickPlacementCheck}, Valid state: {isValidPlacement}");
                    Debug.LogWarning($"[WorkbenchPlacement] Preview position: {previewPosition}, Make sure you're looking at valid ground.");
                }
            }
        }
        else
        {
            // Hide preview when workbench is not selected
            if (previewWorkbench != null && previewWorkbench.activeSelf)
            {
                HidePreview();
            }
        }
    }
    
    void CreatePreview()
    {
        // Only create if not already created
        if (previewWorkbench != null)
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
            // Create a preview instance of the workbench
            previewWorkbench = Instantiate(gameObject, Vector3.zero, Quaternion.identity);
            previewWorkbench.name = "WorkbenchPreview";
            
            // Remove WorkbenchPlacement from preview FIRST (before any other operations)
            WorkbenchPlacement previewPlacement = previewWorkbench.GetComponent<WorkbenchPlacement>();
            if (previewPlacement != null)
            {
                Destroy(previewPlacement); // Destroy, don't just disable
            }
            
            // Disable all scripts on preview (except renderers)
            MonoBehaviour[] scripts = previewWorkbench.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script != this && !(script is Renderer))
                {
                    script.enabled = false;
                }
            }
            
            // Disable all colliders on preview
            Collider[] colliders = previewWorkbench.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                }
            }
            
            // Disable rigidbody if present
            Rigidbody rb = previewWorkbench.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            
            // Cache renderers and materials for faster updates
            cachedRenderers = previewWorkbench.GetComponentsInChildren<Renderer>();
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
            
            // Set initial preview color (will be updated based on placement validity)
            if (cachedRenderers != null && cachedRenderers.Length > 0)
            {
                UpdatePreviewColor(false); // Start with red, will turn green when valid
            }
            
            previewWorkbench.SetActive(false); // Start hidden
        }
        catch (System.Exception e)
        {
            Debug.LogError($"WorkbenchPlacement: Error creating preview: {e.Message}");
            if (previewWorkbench != null)
            {
                Destroy(previewWorkbench);
                previewWorkbench = null;
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
            if (cachedRenderers[i] != null)
            {
                // Get material - create instance to avoid modifying original
                Material mat = cachedRenderers[i].material;
                if (mat == null) continue;
                
                // Make material semi-transparent
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
                
                // Try to enable transparency if shader supports it
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
            Debug.LogWarning("[WorkbenchPlacement] CheckPlacement: playerCamera is null!");
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
                if (objName.IndexOf("workbench", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
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
            
            // Calculate placement position with proper offset
            float finalOffset = placementOffset;
            
            // If using bounds-based offset, calculate based on workbench size
            if (useBoundsOffset)
            {
                Bounds bounds = GetWorkbenchBounds();
                // Use half the height of the bounds to place bottom of workbench on ground
                finalOffset = bounds.extents.y;
            }
            
            // Calculate placement position
            previewPosition = hit.point + Vector3.up * finalOffset;
            
            // Calculate placement rotation - workbench should always face upward (upright)
            // Apply rotation offset from Inspector if set
            previewRotation = Quaternion.Euler(rotationOffset);
            
            // Debug placement status (only log when state changes to avoid spam)
            if (isValidPlacement != lastValidityState)
            {
                if (isValidPlacement)
                {
                    Debug.Log($"<color=green>[WorkbenchPlacement] ✓ Valid placement location found at {previewPosition}</color>");
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[WorkbenchPlacement] ⚠ Invalid placement location at {previewPosition} (surface too steep or invalid)</color>");
                }
            }
            
            return isValidPlacement;
        }
        
        // No valid ground found
        isValidPlacement = false;
        if (lastValidityState != false) // Only log if state changed
        {
            Debug.LogWarning($"<color=red>[WorkbenchPlacement] ✗ No valid ground found for placement (range: {placementRange})</color>");
        }
        return false;
    }
    
    void UpdatePreview()
    {
        // Early exit checks
        if (!showPreview || previewWorkbench == null || playerCamera == null)
        {
            return;
        }
        
        // Use the same placement check logic
        bool wasValid = isValidPlacement;
        CheckPlacement(); // Updates previewPosition, previewRotation, and isValidPlacement
        
        // Update preview visual
        if (previewWorkbench != null)
        {
            previewWorkbench.transform.position = previewPosition;
            previewWorkbench.transform.rotation = previewRotation;
            previewWorkbench.SetActive(true);
            
            // Update preview color based on validity (when state changes)
            if (isValidPlacement != lastValidityState)
            {
                UpdatePreviewColor(isValidPlacement);
                lastValidityState = isValidPlacement;
            }
            // Also update on first show if preview was just created
            else if (!lastValidityState && isValidPlacement)
            {
                UpdatePreviewColor(true);
                lastValidityState = true;
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
        
        // Check if there's enough space for the workbench (optional - you can add bounds checking)
        // For now, just check if it's a valid surface
        return true;
    }
    
    void UpdatePreviewColor(bool valid)
    {
        // Optimized color update using cached renderers
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;
        
        Color targetColor = valid ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f); // Green or Red with better visibility
        
        // Update all renderers
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
            {
                Material mat = cachedRenderers[i].material;
                if (mat == null) continue;
                
                // Update color if material supports _Color property
                if (mat.HasProperty("_Color"))
                {
                    mat.color = targetColor;
                }
                
                // Update emission for better visibility
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", targetColor * 0.8f); // Brighter emission
                    mat.EnableKeyword("_EMISSION");
                }
                
                // Also try _BaseColor for URP materials
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", targetColor);
                }
            }
        }
    }
    
    Bounds GetWorkbenchBounds()
    {
        // Get bounds from renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }
            return bounds;
        }
        
        // Fallback: use collider bounds if available
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            return col.bounds;
        }
        
        // Default bounds if nothing found
        return new Bounds(Vector3.zero, Vector3.one);
    }
    
    void HidePreview()
    {
        if (previewWorkbench != null)
        {
            previewWorkbench.SetActive(false);
        }
        isValidPlacement = false;
        lastValidityState = false; // Reset cache
    }
    
    void PlaceWorkbench()
    {
        if (!isValidPlacement)
        {
            Debug.LogError("<color=red>[WorkbenchPlacement] ✗ WORKBENCH PLACEMENT FAILED - Invalid placement location!</color>");
            Debug.LogError($"[WorkbenchPlacement] Cannot place workbench at {previewPosition}. isValidPlacement is false.");
            return;
        }
        
        Debug.Log("<color=green>[WorkbenchPlacement] ✓ PLACING WORKBENCH...</color>");
        Debug.Log($"[WorkbenchPlacement] Position: {previewPosition}, Rotation: {previewRotation}");
        
        // Create the actual placed workbench
        GameObject placedWorkbench = Instantiate(gameObject, previewPosition, previewRotation);
        placedWorkbench.name = gameObject.name + "_Placed";
        
        // Enable physics and collider for the placed workbench
        Rigidbody rb = placedWorkbench.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Keep it static
            rb.useGravity = false;
        }
        
        Collider[] colliders = placedWorkbench.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
        
        // Make sure placed workbench is on a layer that can be raycasted (not "Ignore Raycast")
        // Try to use "Pickup" layer if it exists, otherwise use "Default"
        int pickupLayer = LayerMask.NameToLayer("Pickup");
        if (pickupLayer == -1)
        {
            pickupLayer = LayerMask.NameToLayer("Default");
        }
        placedWorkbench.layer = pickupLayer;
        
        // Also set all children to the same layer
        foreach (Transform child in placedWorkbench.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = pickupLayer;
        }
        
        // Make sure placed workbench is active
        placedWorkbench.SetActive(true);
        
        // Add WorkbenchPickup component if not already present
        if (placedWorkbench.GetComponent<WorkbenchPickup>() == null)
        {
            placedWorkbench.AddComponent<WorkbenchPickup>();
            Debug.Log("WorkbenchPlacement: Added WorkbenchPickup component to placed workbench.");
        }
        
        // Remove WorkbenchPlacement component from placed workbench (only the held one should have it)
        WorkbenchPlacement placementScript = placedWorkbench.GetComponent<WorkbenchPlacement>();
        if (placementScript != null)
        {
            Destroy(placementScript);
        }
        
        // Remove workbench from hotbar/inventory after placing
        if (hotbarManager != null)
        {
            hotbarManager.ClearCurrentHotbarSlot();
        }
        
        // Destroy the held workbench instance and preview
        HidePreview();
        if (previewWorkbench != null)
        {
            Destroy(previewWorkbench);
        }
        Destroy(this.gameObject);
        
        // SUCCESS MESSAGE
        Debug.Log($"<color=green>[WorkbenchPlacement] ✓✓✓ WORKBENCH SUCCESSFULLY PLACED! ✓✓✓</color>");
        Debug.Log($"<color=green>[WorkbenchPlacement] Workbench '{placedWorkbench.name}' placed at position: {previewPosition}</color>");
    }
    
    void DropWorkbench()
    {
        Debug.Log("WorkbenchPlacement: Dropping workbench (removing from hotbar).");
        
        // Remove from hotbar
        if (hotbarManager != null)
        {
            hotbarManager.ClearCurrentHotbarSlot();
        }
        
        // Hide preview
        HidePreview();
        
        // Destroy preview and workbench
        if (previewWorkbench != null)
        {
            Destroy(previewWorkbench);
        }
        Destroy(this.gameObject);
    }
    
    void OnDestroy()
    {
        // Clean up preview when workbench is destroyed
        if (previewWorkbench != null)
        {
            Destroy(previewWorkbench);
        }
    }
}

