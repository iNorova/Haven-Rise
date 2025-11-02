using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

/// <summary>
/// Displays tooltip text at crosshair when player hovers over interactable objects
/// </summary>
public class CrosshairTooltip : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI tooltipText; // TextMeshPro text component for tooltip
    public Text tooltipTextLegacy; // Fallback for legacy Text component
    public GameObject tooltipPanel; // Optional panel background (can be left null for text only)
    [Tooltip("If true, only text will show without panel background.")]
    public bool textOnly = true; // Show only text without panel
    
    [Header("Display Settings")]
    [Tooltip("Offset from center of screen (in pixels). Positive Y moves up, negative Y moves down.")]
    public Vector2 screenOffset = new Vector2(0f, -50f); // Slightly below center
    [Tooltip("Detection range for interactable objects.")]
    public float detectionRange = 5f;
    [Tooltip("Fade in/out speed for tooltip.")]
    public float fadeSpeed = 5f;
    
    [Header("Interaction Layer Masks")]
    [Tooltip("Layers that contain interactable objects.")]
    public LayerMask interactableLayers = ~0; // All layers by default
    
    [Header("Text Customization - General")]
    [Tooltip("Text to show for pickupable items.")]
    public string pickupableText = "Press [F] to Pick Up";
    [Tooltip("Text to show for beds.")]
    public string bedText = "Press [G] to Sleep";
    [Tooltip("Text to show for NPCs.")]
    public string npcText = "Press [E] to Interact";
    [Tooltip("Text to show for meat/food items.")]
    public string meatText = "Press [F] to Pick Up";
    [Tooltip("Text to show when looking at terrain/ground with bed selected.")]
    public string bedPlacementText = "Press [Left Click] to Place Bed";
    
    [Header("Text Customization - Destroyable Objects")]
    [Tooltip("Text to show for trees specifically.")]
    public string treeText = "Press [Left Click] to Chop Tree";
    [Tooltip("Text to show for rocks specifically.")]
    public string rockText = "Press [Left Click] to Mine Rock";
    [Tooltip("Text to show for other destroyable objects (fallback).")]
    public string destroyableText = "Press [Left Click] to Break";
    
    private Camera playerCamera;
    private CanvasGroup canvasGroup;
    private string currentTooltipText = "";
    private float targetAlpha = 0f;
    private float currentAlpha = 0f;
    
    // Cache for performance
    private RaycastHit lastHit;
    private GameObject lastHitObject;
    private HotbarManager hotbarManager;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
        
        if (playerCamera == null)
        {
            Debug.LogError("CrosshairTooltip: No camera found!");
            enabled = false;
            return;
        }
        
        // Get HotbarManager for checking if bed is selected
        hotbarManager = FindObjectOfType<HotbarManager>();
        
        // Setup UI components
        if (tooltipText == null && tooltipTextLegacy == null)
        {
            // Try to find existing text component
            tooltipText = GetComponentInChildren<TextMeshProUGUI>();
            tooltipTextLegacy = GetComponentInChildren<Text>();
            
            if (tooltipText == null && tooltipTextLegacy == null)
            {
                Debug.LogWarning("CrosshairTooltip: No text component found. Creating one...");
                CreateTooltipUI();
            }
        }
        
        // Setup canvas group for fading
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Initialize as hidden
        if (!textOnly)
        {
            canvasGroup.alpha = 0f;
        }
        canvasGroup.blocksRaycasts = false;
        
        // Initialize text color for textOnly mode
        if (textOnly)
        {
            Color transparentWhite = Color.white;
            transparentWhite.a = 0f;
            if (tooltipText != null)
            {
                tooltipText.color = transparentWhite;
            }
            if (tooltipTextLegacy != null)
            {
                tooltipTextLegacy.color = transparentWhite;
            }
        }
        
        // Position tooltip at screen center + offset (directly on canvas if textOnly, or relative to panel)
        if (tooltipText != null)
        {
            RectTransform rectTransform = tooltipText.rectTransform;
            if (textOnly)
            {
                // If text only, position directly on canvas at center + offset
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = screenOffset;
            }
            else
            {
                // If panel exists, position relative to panel
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            rectTransform.sizeDelta = new Vector2(250f, 50f); // Ensure proper width
            tooltipText.enableWordWrapping = false; // Prevent vertical wrapping
        }
        else if (tooltipTextLegacy != null)
        {
            RectTransform rectTransform = tooltipTextLegacy.rectTransform;
            if (textOnly)
            {
                // If text only, position directly on canvas at center + offset
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = screenOffset;
            }
            else
            {
                // If panel exists, position relative to panel
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            rectTransform.sizeDelta = new Vector2(250f, 50f); // Ensure proper width
            tooltipTextLegacy.horizontalOverflow = HorizontalWrapMode.Overflow; // Prevent wrapping
        }
    }
    
    void CreateTooltipUI()
    {
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        Transform textParent = canvas.transform;
        
        // Create tooltip panel only if textOnly is false
        if (!textOnly)
        {
            if (tooltipPanel == null)
            {
                GameObject panelObj = new GameObject("TooltipPanel");
                panelObj.transform.SetParent(canvas.transform, false);
                tooltipPanel = panelObj;
                
                Image panelImage = panelObj.AddComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black
                
                RectTransform panelRect = panelObj.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(250f, 50f); // Wider to prevent vertical text
                panelRect.anchoredPosition = screenOffset;
            }
            textParent = tooltipPanel.transform;
            
            // Hide panel initially if it exists
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        else
        {
            // If textOnly, ensure panel is hidden/disabled
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }
        
        // Create text component
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(textParent, false);
        
        // Try TextMeshPro first
        try
        {
            tooltipText = textObj.AddComponent<TextMeshProUGUI>();
            if (tooltipText != null)
            {
            tooltipText.text = "";
            tooltipText.fontSize = 16;
            // Initialize color based on textOnly mode
            if (textOnly)
            {
                Color transparentWhite = Color.white;
                transparentWhite.a = 0f;
                tooltipText.color = transparentWhite;
            }
            else
            {
                tooltipText.color = Color.white;
            }
            tooltipText.alignment = TextAlignmentOptions.Center;
            tooltipText.enableWordWrapping = false; // Prevent wrapping that causes vertical text
            tooltipText.overflowMode = TextOverflowModes.Overflow; // Allow horizontal overflow if needed
                
                RectTransform textRect = tooltipText.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
                textRect.sizeDelta = new Vector2(250f, 50f); // Ensure width is sufficient
            }
        }
        catch
        {
            tooltipText = null;
        }
        
        // Fallback to legacy Text if TextMeshPro failed
        if (tooltipText == null)
        {
            tooltipTextLegacy = textObj.AddComponent<Text>();
            tooltipTextLegacy.text = "";
            tooltipTextLegacy.fontSize = 16;
            // Initialize color based on textOnly mode
            if (textOnly)
            {
                Color transparentWhite = Color.white;
                transparentWhite.a = 0f;
                tooltipTextLegacy.color = transparentWhite;
            }
            else
            {
                tooltipTextLegacy.color = Color.white;
            }
            tooltipTextLegacy.alignment = TextAnchor.MiddleCenter;
            
            RectTransform textRect = tooltipTextLegacy.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
        }
    }
    
    void Update()
    {
        // Don't show tooltip if inventory is open or paused
        InventoryUIManager inventoryUI = FindObjectOfType<InventoryUIManager>();
        if (inventoryUI != null && inventoryUI.IsInventoryOpen())
        {
            HideTooltip();
            return;
        }
        
        if (PauseMenuManager.IsPauseMenuOpen())
        {
            HideTooltip();
            return;
        }
        
        // Check for interactable objects
        CheckForInteractables();
        
        // Update fade
        UpdateFade();
    }
    
    void CheckForInteractables()
    {
        if (playerCamera == null) return;
        
        // Cache hotbar manager check to avoid multiple FindObjectOfType calls
        if (hotbarManager == null)
        {
            hotbarManager = FindObjectOfType<HotbarManager>();
        }
        
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); // Center of screen
        
        // Check if bed is selected in hotbar for placement tooltip (cached check)
        bool bedSelected = IsBedSelected();
        
        // If bed is selected, prioritize showing placement tooltip
        if (bedSelected)
        {
            // Always raycast to check for ground when bed is selected
            RaycastHit groundHit;
            // Use a longer range or no layer mask restriction for ground detection
            if (Physics.Raycast(ray, out groundHit, detectionRange))
            {
                GameObject hitObject = groundHit.collider.gameObject;
                
                // Check if it's a placed bed first - if so, show sleep tooltip instead of placement tooltip
                BedInteraction placedBed = hitObject.GetComponent<BedInteraction>();
                if (placedBed == null)
                {
                    placedBed = hitObject.GetComponentInParent<BedInteraction>();
                }
                if (placedBed != null && !placedBed.isTransitioning)
                {
                    // This is a placed bed - show sleep tooltip even when holding a bed
                    ShowTooltip(bedText); // "Press [G] to Sleep"
                    lastHitObject = hitObject;
                    lastHit = groundHit;
                    return;
                }
                // Check if it's terrain, ground, or floor (or any non-interactable surface)
                else if (IsGroundSurface(hitObject))
                {
                    ShowTooltip(bedPlacementText);
                    lastHitObject = hitObject;
                    lastHit = groundHit;
                    return;
                }
                // If it hit other interactables, don't show tooltip
                else if (hitObject.CompareTag("Pickupable") || hitObject.CompareTag("Destroyable"))
                {
                    HideTooltip();
                    lastHitObject = null;
                    return;
                }
            }
            
            // If we got here and bed is selected but didn't hit ground or bed, hide tooltip
            HideTooltip();
            lastHitObject = null;
            return;
        }
        
        // Normal tooltip logic when bed is not selected
        // Use a longer range or no layer mask restriction to detect beds properly
        if (Physics.Raycast(ray, out hit, detectionRange))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if it's the same object as last frame (performance optimization)
            if (hitObject == lastHitObject && !string.IsNullOrEmpty(currentTooltipText))
            {
                return; // Already showing correct tooltip
            }
            
            lastHitObject = hitObject;
            lastHit = hit;
            
            // Determine what type of object this is and show appropriate text
            string tooltip = GetTooltipText(hitObject, false);
            
            if (!string.IsNullOrEmpty(tooltip))
            {
                ShowTooltip(tooltip);
            }
            else
            {
                HideTooltip();
            }
        }
        else
        {
            HideTooltip();
            lastHitObject = null;
        }
    }
    
    bool IsBedSelected()
    {
        if (hotbarManager == null) 
        {
            if (Time.frameCount % 300 == 0)
            {
                Debug.LogWarning("[CrosshairTooltip] IsBedSelected: hotbarManager is null!");
            }
            return false;
        }
        
        GameObject currentItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
        if (currentItem == null) return false;
        
        // Check if current item is a bed (has BedPlacement component)
        // GetComponent works even on inactive objects, but let's be thorough
        BedPlacement bedPlacement = currentItem.GetComponent<BedPlacement>();
        
        // Also check by name as fallback (for picked-up beds that might have BedPlacement but GetComponent failed)
        // Note: Even if name contains "_Placed", it might still be a bed that can be placed (name cleanup might have failed)
        if (bedPlacement == null)
        {
            string itemName = currentItem.name.ToLower();
            // Check if it's a bed (even if name cleanup failed and it still has "_placed")
            if (itemName.Contains("bed"))
            {
                // Might be a bed but BedPlacement not added yet - try to find it (including inactive children)
                bedPlacement = currentItem.GetComponentInChildren<BedPlacement>(true); // Include inactive
                
                // Also try GetComponents to be thorough
                if (bedPlacement == null)
                {
                    BedPlacement[] placements = currentItem.GetComponents<BedPlacement>();
                    if (placements != null && placements.Length > 0)
                    {
                        bedPlacement = placements[0];
                    }
                }
                
                // If still no BedPlacement found but it's clearly a bed, try to add it as fallback
                // (This shouldn't normally happen, but helps with edge cases)
                if (bedPlacement == null && !itemName.Contains("_placed"))
                {
                    if (Time.frameCount % 300 == 0)
                    {
                        Debug.LogWarning($"[CrosshairTooltip] Bed '{currentItem.name}' found but no BedPlacement component. This bed may not be placeable.");
                    }
                }
            }
        }
        
        // Debug every few frames to help troubleshoot (reduced frequency to avoid spam)
        if (Time.frameCount % 300 == 0 && currentItem != null)
        {
            bool hasPlacement = bedPlacement != null;
            Debug.Log($"[CrosshairTooltip] IsBedSelected: Item={currentItem.name}, Active={currentItem.activeSelf}, ActiveInHierarchy={currentItem.activeInHierarchy}, HasBedPlacement={hasPlacement}, Slot={hotbarManager.selectedSlot}");
            
            // If bed should be selected but BedPlacement is missing, warn
            if (currentItem.name.ToLower().Contains("bed") && !hasPlacement && !currentItem.name.Contains("_Placed"))
            {
                Debug.LogWarning($"[CrosshairTooltip] Bed detected but BedPlacement component missing! Item: {currentItem.name}");
            }
        }
        
        return bedPlacement != null;
    }
    
    bool IsGroundSurface(GameObject obj)
    {
        if (obj == null) return false;
        
        // Skip player
        if (obj.CompareTag("Player"))
        {
            return false;
        }
        
        // Check if it's terrain
        if (obj.GetComponent<Terrain>() != null || obj.GetComponent<TerrainCollider>() != null)
        {
            return true;
        }
        
        // Check if it's tagged as ground/terrain
        string objName = obj.name.ToLower();
        if (obj.CompareTag("Ground") || obj.CompareTag("Terrain") || 
            objName.Contains("terrain") || objName.Contains("ground") || objName.Contains("floor"))
        {
            return true;
        }
        
        // Check if it's an interactable object (skip these)
        if (obj.CompareTag("Pickupable") || obj.CompareTag("Destroyable") || 
            obj.CompareTag("NPC") || obj.GetComponent<BedInteraction>() != null ||
            obj.GetComponent<ItemIconProvider>() != null || obj.GetComponent<BedPickup>() != null)
        {
            return false;
        }
        
        // If it's not an interactable, check if surface is horizontal enough
        // Raycast to get surface normal
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out hit, detectionRange))
        {
            if (hit.collider.gameObject == obj)
            {
                float angle = Vector3.Angle(hit.normal, Vector3.up);
                if (angle < 45f) // Surface is somewhat horizontal
                {
                    return true;
                }
            }
        }
        
        // Default: if it's not an interactable, assume it's a valid surface for placement
        // This handles generic floor/mesh objects that don't have specific tags
        return true;
    }
    
    string GetTooltipText(GameObject obj, bool bedSelected = false)
    {
        if (obj == null) return "";
        
        // If bed is selected and this is ground, show placement text
        if (bedSelected && IsGroundSurface(obj))
        {
            return bedPlacementText;
        }
        
        // Check tags first (fastest)
        if (obj.CompareTag("Pickupable"))
        {
            // Check if it's meat/food
            string objName = obj.name.ToLower();
            if (objName.Contains("meat") || objName.Contains("food"))
            {
                return meatText;
            }
            return pickupableText;
        }
        
        if (obj.CompareTag("Destroyable"))
        {
            // Check for specific object types by name
            string objName = obj.name.ToLower();
            
            // Check for tree
            if (objName.Contains("tree") || objName.Contains("log") || objName.Contains("wood"))
            {
                return treeText;
            }
            
            // Check for rock/stone
            if (objName.Contains("rock") || objName.Contains("stone") || objName.Contains("boulder"))
            {
                return rockText;
            }
            
            // Check for DestroyableObject component and its isTree flag
            DestroyableObject destroyable = obj.GetComponent<DestroyableObject>();
            if (destroyable != null)
            {
                // Use reflection to check isTree field if it exists
                var isTreeField = typeof(DestroyableObject).GetField("isTree", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isTreeField != null && (bool)isTreeField.GetValue(destroyable))
                {
                    return treeText;
                }
            }
            
            // Fallback to generic destroyable text
            return destroyableText;
        }
        
        // Check components - placed beds have BedInteraction component
        BedInteraction bedInteraction = obj.GetComponent<BedInteraction>();
        if (bedInteraction == null)
        {
            bedInteraction = obj.GetComponentInParent<BedInteraction>();
        }
        if (bedInteraction != null)
        {
            // Only show sleep text if bed is placed (not transitioning)
            // This is a placed bed, show sleep tooltip
            if (!bedInteraction.isTransitioning)
            {
                return bedText; // "Press [G] to Sleep"
            }
            else
            {
                // Bed is transitioning, don't show tooltip
                return "";
            }
        }
        
        // Check if it's an NPC (you might need to add an NPC tag or component)
        if (obj.CompareTag("NPC") || obj.GetComponent("NPCController") != null)
        {
            return npcText;
        }
        
        // Check if it has ItemIconProvider (pickupable item)
        if (obj.GetComponent<ItemIconProvider>() != null || obj.GetComponentInParent<ItemIconProvider>() != null)
        {
            // Check if it's meat/food by name
            string objName = obj.name.ToLower();
            if (objName.Contains("meat") || objName.Contains("food"))
            {
                return meatText;
            }
            return pickupableText;
        }
        
        // Check for trees by name (backup check if not tagged)
        string objNameCheck = obj.name.ToLower();
        if (objNameCheck.Contains("tree") || objNameCheck.Contains("log") || objNameCheck.Contains("wood"))
        {
            return treeText;
        }
        
        // Check for rocks by name (backup check if not tagged)
        if (objNameCheck.Contains("rock") || objNameCheck.Contains("stone") || objNameCheck.Contains("boulder"))
        {
            return rockText;
        }
        
        return ""; // No tooltip for this object
    }
    
    void ShowTooltip(string text)
    {
        if (currentTooltipText == text && targetAlpha == 1f)
        {
            return; // Already showing this text
        }
        
        currentTooltipText = text;
        targetAlpha = 1f;
        
        // Update text component
        if (tooltipText != null)
        {
            tooltipText.text = text;
            tooltipText.gameObject.SetActive(true);
        }
        else if (tooltipTextLegacy != null)
        {
            tooltipTextLegacy.text = text;
            tooltipTextLegacy.gameObject.SetActive(true);
        }
        
        // Show panel only if textOnly is false and panel exists
        if (!textOnly && tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
        }
    }
    
    void HideTooltip()
    {
        if (targetAlpha == 0f)
        {
            return; // Already hidden
        }
        
        targetAlpha = 0f;
        currentTooltipText = "";
        
        // Clear text
        if (tooltipText != null)
        {
            tooltipText.text = "";
            tooltipText.gameObject.SetActive(false);
        }
        if (tooltipTextLegacy != null)
        {
            tooltipTextLegacy.text = "";
            tooltipTextLegacy.gameObject.SetActive(false);
        }
        
        // Hide panel if not textOnly
        if (!textOnly && tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    void UpdateFade()
    {
        // Smoothly interpolate alpha
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
        
        if (Mathf.Abs(currentAlpha - targetAlpha) < 0.01f)
        {
            currentAlpha = targetAlpha;
        }
        
        // Apply alpha based on mode
        if (textOnly)
        {
            // When textOnly, apply alpha directly to text color (not canvas group)
            Color textColor = Color.white;
            textColor.a = currentAlpha;
            
            if (tooltipText != null)
            {
                tooltipText.color = textColor;
                tooltipText.gameObject.SetActive(currentAlpha > 0.01f);
            }
            if (tooltipTextLegacy != null)
            {
                tooltipTextLegacy.color = textColor;
                tooltipTextLegacy.gameObject.SetActive(currentAlpha > 0.01f);
            }
        }
        else
        {
            // When using panel, use canvas group alpha
            canvasGroup.alpha = currentAlpha;
            
            // Handle visibility of text and panel
            if (currentAlpha <= 0.01f)
            {
                // Hide text
                if (tooltipText != null)
                {
                    tooltipText.gameObject.SetActive(false);
                }
                if (tooltipTextLegacy != null)
                {
                    tooltipTextLegacy.gameObject.SetActive(false);
                }
                
                // Hide panel
                if (tooltipPanel != null)
                {
                    tooltipPanel.SetActive(false);
                }
            }
            else
            {
                // Show text
                if (tooltipText != null)
                {
                    tooltipText.gameObject.SetActive(true);
                }
                if (tooltipTextLegacy != null)
                {
                    tooltipTextLegacy.gameObject.SetActive(true);
                }
                
                // Show panel
                if (tooltipPanel != null)
                {
                    tooltipPanel.SetActive(true);
                }
            }
        }
    }
}

