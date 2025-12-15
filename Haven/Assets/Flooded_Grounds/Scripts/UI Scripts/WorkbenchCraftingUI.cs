using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Standalone crafting UI specifically for workbench - only shows boat repair items (Plywood, Engine, Propeller).
/// </summary>
public class WorkbenchCraftingUI : MonoBehaviour
{
    [Header("Workbench Reference")]
    [Tooltip("Optional: Drag the workbench prefab here for reference. This helps identify which workbenches use this crafting system.")]
    public GameObject workbenchPrefab;
    
    [Header("Recipe Settings")]
    [Tooltip("Prefab for Plywood item")]
    public GameObject plywoodPrefab;
    [Tooltip("Prefab for Engine item")]
    public GameObject enginePrefab;
    [Tooltip("Prefab for Propeller item")]
    public GameObject propellerPrefab;
    
    [Header("Plywood Recipe")]
    public string plywoodItemName = "Plywood";
    public List<ItemRequirement> plywoodRequirements = new List<ItemRequirement>();
    
    [Header("Engine Recipe")]
    public string engineItemName = "Engine";
    public List<ItemRequirement> engineRequirements = new List<ItemRequirement>();
    
    [Header("Propeller Recipe")]
    public string propellerItemName = "Propeller";
    public List<ItemRequirement> propellerRequirements = new List<ItemRequirement>();
    
    [System.Serializable]
    public class ItemRequirement
    {
        public string itemName;
        public int quantity = 1;
    }
    
    [Header("UI References")]
    private GameObject craftingPanel;
    private Transform recipeContainer;
    private WorkbenchInteraction currentWorkbench;
    private bool isOpen = false;
    private Canvas craftingCanvas;
    
    private InventoryManager inventoryManager;
    private HotbarManager hotbarManager;
    private static WorkbenchCraftingUI instance;
    private Coroutine cursorUnlockCoroutine;
    
    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
        hotbarManager = FindObjectOfType<HotbarManager>();
        
        CreateUI();
        
        // Initially hide
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }
    }
    
    void Update()
    {
        // Close on ESC
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }
    
    public bool IsOpen()
    {
        return isOpen;
    }
    
    public void Open(WorkbenchInteraction workbench)
    {
        if (workbench == null)
        {
            Debug.LogError("WorkbenchCraftingUI: Cannot open - workbench is null!");
            return;
        }
        
        Debug.Log("WorkbenchCraftingUI: Opening crafting menu...");
        
        currentWorkbench = workbench;
        isOpen = true;
        
        // Make sure UI is created
        if (craftingPanel == null)
        {
            Debug.LogWarning("WorkbenchCraftingUI: Crafting panel is null! Creating UI...");
            CreateUI();
        }
        
        if (craftingPanel != null)
        {
            // Ensure canvas is active
            if (craftingCanvas != null)
            {
                craftingCanvas.gameObject.SetActive(true);
                craftingCanvas.enabled = true;
                Debug.Log($"WorkbenchCraftingUI: Canvas active: {craftingCanvas.gameObject.activeInHierarchy}, Enabled: {craftingCanvas.enabled}");
            }
            
            craftingPanel.SetActive(true);
            UpdateRecipeButtons();
            Debug.Log($"WorkbenchCraftingUI: Crafting panel activated! Panel active: {craftingPanel.activeInHierarchy}, Canvas active: {(craftingCanvas != null ? craftingCanvas.gameObject.activeInHierarchy.ToString() : "null")}");
        }
        else
        {
            Debug.LogError("WorkbenchCraftingUI: Failed to create crafting panel!");
        }
        
        // Disable player controls
        DisablePlayerControls();
        
        // Unlock cursor (force unlock immediately)
        UnlockCursorForCrafting();
        
        // Start continuous cursor unlock coroutine (other systems might try to lock it)
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
        }
        cursorUnlockCoroutine = StartCoroutine(ContinuousCursorUnlock());
        
        Debug.Log("WorkbenchCraftingUI: Crafting menu opened successfully!");
    }
    
    public void Close()
    {
        isOpen = false;
        currentWorkbench = null;
        
        // Stop cursor unlock coroutine
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
            cursorUnlockCoroutine = null;
        }
        
        if (craftingPanel != null)
        {
            craftingPanel.SetActive(false);
        }
        
        // Re-enable player controls
        EnablePlayerControls();
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void CreateUI()
    {
        Debug.Log("WorkbenchCraftingUI: CreateUI() called");
        
        // Always create a dedicated canvas for the workbench crafting UI (like minigames do)
        if (craftingCanvas == null)
        {
            Debug.Log("WorkbenchCraftingUI: Creating new canvas...");
            GameObject canvasObj = new GameObject("WorkbenchCraftingCanvas");
            DontDestroyOnLoad(canvasObj); // Prevent destruction during scene changes
            craftingCanvas = canvasObj.AddComponent<Canvas>();
            craftingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            craftingCanvas.sortingOrder = 998; // High sorting order to ensure it's on top (just below minigames' 999)
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Ensure canvas GameObject is active
            canvasObj.SetActive(true);
            Debug.Log($"WorkbenchCraftingUI: Created new canvas '{canvasObj.name}', Active: {canvasObj.activeInHierarchy}, Enabled: {craftingCanvas.enabled}, SortingOrder: {craftingCanvas.sortingOrder}");
        }
        else
        {
            // Canvas already exists, just ensure it's active
            craftingCanvas.gameObject.SetActive(true);
            craftingCanvas.enabled = true;
            craftingCanvas.sortingOrder = 998; // Ensure it stays on top
            Debug.Log($"WorkbenchCraftingUI: Using existing canvas '{craftingCanvas.name}', Active: {craftingCanvas.gameObject.activeInHierarchy}, Enabled: {craftingCanvas.enabled}, SortingOrder: {craftingCanvas.sortingOrder}");
        }
        
        // Clean up any existing panel first
        Transform existingPanel = craftingCanvas.transform.Find("WorkbenchCraftingPanel");
        if (existingPanel != null)
        {
            Debug.Log("WorkbenchCraftingUI: Found existing panel, destroying it...");
            DestroyImmediate(existingPanel.gameObject);
        }
        
        // Create main panel
        GameObject panelObj = new GameObject("WorkbenchCraftingPanel");
        panelObj.transform.SetParent(craftingCanvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 500);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Add background
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        // Create title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.sizeDelta = new Vector2(0, 60);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleText.text = "BOAT REPAIR CRAFTING";
        titleText.fontSize = 32;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        
        // Create close button
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        Image closeBtnImage = closeBtnObj.AddComponent<Image>();
        closeBtnImage.color = new Color(0.3f, 0.1f, 0.1f, 1f);
        RectTransform closeBtnRect = closeBtnObj.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1f, 1f);
        closeBtnRect.anchorMax = new Vector2(1f, 1f);
        closeBtnRect.sizeDelta = new Vector2(40, 40);
        closeBtnRect.anchoredPosition = new Vector2(-10, -10);
        
        GameObject closeBtnTextObj = new GameObject("Text");
        closeBtnTextObj.transform.SetParent(closeBtnObj.transform, false);
        TextMeshProUGUI closeBtnText = closeBtnTextObj.AddComponent<TextMeshProUGUI>();
        RectTransform closeBtnTextRect = closeBtnText.rectTransform;
        closeBtnTextRect.anchorMin = Vector2.zero;
        closeBtnTextRect.anchorMax = Vector2.one;
        closeBtnTextRect.sizeDelta = Vector2.zero;
        closeBtnText.text = "X";
        closeBtnText.fontSize = 24;
        closeBtnText.alignment = TextAlignmentOptions.Center;
        closeBtnText.color = Color.white;
        
        closeBtn.onClick.AddListener(Close);
        
        // Create scroll view for recipes
        GameObject scrollViewObj = new GameObject("ScrollView");
        scrollViewObj.transform.SetParent(panelObj.transform, false);
        RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.sizeDelta = new Vector2(-20, -80);
        scrollRect.anchoredPosition = new Vector2(0, 10);
        
        ScrollRect scrollRectComponent = scrollViewObj.AddComponent<ScrollRect>();
        Image scrollImage = scrollViewObj.AddComponent<Image>();
        scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        
        // Create content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);
        contentRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 10f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        
        ContentSizeFitter sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRectComponent.content = contentRect;
        scrollRectComponent.viewport = scrollRect;
        scrollRectComponent.vertical = true;
        scrollRectComponent.horizontal = false;
        
        recipeContainer = contentObj.transform;
        
        craftingPanel = panelObj;
        craftingPanel.SetActive(false);
    }
    
    private void UpdateRecipeButtons()
    {
        // Clear existing buttons
        foreach (Transform child in recipeContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create recipe buttons
        CreateRecipeButton("Plywood", plywoodItemName, plywoodRequirements, plywoodPrefab);
        CreateRecipeButton("Engine", engineItemName, engineRequirements, enginePrefab);
        CreateRecipeButton("Propeller", propellerItemName, propellerRequirements, propellerPrefab);
    }
    
    private void CreateRecipeButton(string displayName, string itemName, List<ItemRequirement> requirements, GameObject outputPrefab)
    {
        // Create button
        GameObject buttonObj = new GameObject($"{displayName}Button");
        buttonObj.transform.SetParent(recipeContainer, false);
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(0, 100);
        
        // Create layout
        HorizontalLayoutGroup layout = buttonObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        
        // Create item name text
        GameObject nameObj = new GameObject("ItemName");
        nameObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        RectTransform nameRect = nameText.rectTransform;
        nameRect.sizeDelta = new Vector2(150, 0);
        nameText.text = displayName;
        nameText.fontSize = 24;
        nameText.alignment = TextAlignmentOptions.Left;
        nameText.color = Color.white;
        nameText.fontStyle = FontStyles.Bold;
        
        // Create requirements text
        GameObject reqObj = new GameObject("Requirements");
        reqObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI reqText = reqObj.AddComponent<TextMeshProUGUI>();
        RectTransform reqRect = reqText.rectTransform;
        reqRect.sizeDelta = new Vector2(300, 0);
        reqText.fontSize = 18;
        reqText.alignment = TextAlignmentOptions.Left;
        reqText.color = Color.white;
        reqText.enableWordWrapping = true;
        
        // Build requirements string
        string reqString = "Requirements:\n";
        bool canCraft = true;
        foreach (var req in requirements)
        {
            int available = CountItem(req.itemName);
            string color = available >= req.quantity ? "<color=#00FF00>" : "<color=#FF0000>";
            reqString += $"{color}{req.itemName}: {available}/{req.quantity}</color>\n";
            if (available < req.quantity)
            {
                canCraft = false;
            }
        }
        reqText.text = reqString;
        
        // Create craft button
        GameObject craftBtnObj = new GameObject("CraftButton");
        craftBtnObj.transform.SetParent(buttonObj.transform, false);
        Button craftBtn = craftBtnObj.AddComponent<Button>();
        Image craftBtnImage = craftBtnObj.AddComponent<Image>();
        craftBtnImage.color = canCraft ? new Color(0.1f, 0.3f, 0.1f, 1f) : new Color(0.3f, 0.1f, 0.1f, 1f);
        RectTransform craftBtnRect = craftBtnObj.GetComponent<RectTransform>();
        craftBtnRect.sizeDelta = new Vector2(100, 0);
        
        GameObject craftBtnTextObj = new GameObject("Text");
        craftBtnTextObj.transform.SetParent(craftBtnObj.transform, false);
        TextMeshProUGUI craftBtnText = craftBtnTextObj.AddComponent<TextMeshProUGUI>();
        RectTransform craftBtnTextRect = craftBtnText.rectTransform;
        craftBtnTextRect.anchorMin = Vector2.zero;
        craftBtnTextRect.anchorMax = Vector2.one;
        craftBtnTextRect.sizeDelta = Vector2.zero;
        craftBtnText.text = "CRAFT";
        craftBtnText.fontSize = 20;
        craftBtnText.alignment = TextAlignmentOptions.Center;
        craftBtnText.color = Color.white;
        craftBtnText.fontStyle = FontStyles.Bold;
        
        craftBtn.interactable = canCraft;
        craftBtn.onClick.AddListener(() => CraftItem(itemName, requirements, outputPrefab));
        
        // Update button color based on craftability
        ColorBlock colors = button.colors;
        colors.normalColor = canCraft ? new Color(0.2f, 0.2f, 0.2f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
        button.colors = colors;
    }
    
    private void CraftItem(string itemName, List<ItemRequirement> requirements, GameObject outputPrefab)
    {
        if (outputPrefab == null)
        {
            Debug.LogError($"WorkbenchCraftingUI: Output prefab is null for {itemName}!");
            return;
        }
        
        // Check if we can craft
        foreach (var req in requirements)
        {
            if (CountItem(req.itemName) < req.quantity)
            {
                Debug.LogWarning($"WorkbenchCraftingUI: Cannot craft {itemName} - need {req.quantity} {req.itemName}");
                return;
            }
        }
        
        // Consume items
        foreach (var req in requirements)
        {
            ConsumeItem(req.itemName, req.quantity);
        }
        
        // Create output item
        GameObject craftedItem = Instantiate(outputPrefab);
        ItemIconProvider iconProvider = craftedItem.GetComponent<ItemIconProvider>();
        if (iconProvider != null)
        {
            craftedItem.name = iconProvider.itemName;
        }
        else
        {
            craftedItem.name = itemName;
        }
        
        // Add to inventory or hotbar
        bool added = false;
        if (inventoryManager != null)
        {
            added = inventoryManager.AddItem(craftedItem);
        }
        
        if (!added && hotbarManager != null)
        {
            // Try hotbar
            for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
            {
                if (hotbarManager.GetItem(i) == null)
                {
                    hotbarManager.SetItem(i, craftedItem);
                    craftedItem.transform.SetParent(hotbarManager.handHolder);
                    craftedItem.transform.localPosition = Vector3.zero;
                    craftedItem.transform.localRotation = Quaternion.identity;
                    craftedItem.SetActive(false);
                    added = true;
                    break;
                }
            }
        }
        
        if (!added)
        {
            Debug.LogWarning($"WorkbenchCraftingUI: No space for crafted {itemName}!");
            Destroy(craftedItem);
        }
        else
        {
            Debug.Log($"WorkbenchCraftingUI: Successfully crafted {itemName}!");
            
            // Update UI
            if (inventoryManager != null)
            {
                inventoryManager.UpdateInventoryUI();
            }
            if (hotbarManager != null)
            {
                hotbarManager.UpdateHotbarUI();
            }
            
            UpdateRecipeButtons();
        }
    }
    
    private int CountItem(string itemName)
    {
        int count = 0;
        
        // Count from inventory
        if (inventoryManager != null)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length; i++)
            {
                GameObject item = inventoryManager.GetItem(i);
                if (item != null && MatchesItemName(item, itemName))
                {
                    InventorySlot slot = inventoryManager.inventorySlots[i];
                    count += slot.GetStackCount();
                }
            }
        }
        
        // Count from hotbar
        if (hotbarManager != null)
        {
            for (int i = 0; i < hotbarManager.hotbarSlots.Length; i++)
            {
                GameObject item = hotbarManager.GetItem(i);
                if (item != null && MatchesItemName(item, itemName))
                {
                    InventorySlot slot = hotbarManager.hotbarSlots[i];
                    count += slot.GetStackCount();
                }
            }
        }
        
        return count;
    }
    
    private void ConsumeItem(string itemName, int quantity)
    {
        int remaining = quantity;
        
        // Consume from hotbar first
        if (hotbarManager != null && remaining > 0)
        {
            for (int i = 0; i < hotbarManager.hotbarSlots.Length && remaining > 0; i++)
            {
                GameObject item = hotbarManager.GetItem(i);
                if (item != null && MatchesItemName(item, itemName))
                {
                    InventorySlot slot = hotbarManager.hotbarSlots[i];
                    int stackCount = slot.GetStackCount();
                    int toConsume = Mathf.Min(stackCount, remaining);
                    
                    if (toConsume >= stackCount)
                    {
                        item.SetActive(false);
                        item.transform.SetParent(null);
                        hotbarManager.SetItem(i, null);
                        Destroy(item);
                        remaining -= stackCount;
                    }
                    else
                    {
                        slot.SetStackCount(stackCount - toConsume);
                        remaining -= toConsume;
                    }
                }
            }
            hotbarManager.UpdateHotbarUI();
        }
        
        // Consume from inventory
        if (inventoryManager != null && remaining > 0)
        {
            for (int i = 0; i < inventoryManager.inventorySlots.Length && remaining > 0; i++)
            {
                GameObject item = inventoryManager.GetItem(i);
                if (item != null && MatchesItemName(item, itemName))
                {
                    InventorySlot slot = inventoryManager.inventorySlots[i];
                    int stackCount = slot.GetStackCount();
                    int toConsume = Mathf.Min(stackCount, remaining);
                    
                    if (toConsume >= stackCount)
                    {
                        inventoryManager.RemoveItem(i);
                        Destroy(item);
                        remaining -= stackCount;
                    }
                    else
                    {
                        slot.SetStackCount(stackCount - toConsume);
                        remaining -= toConsume;
                    }
                }
            }
            inventoryManager.UpdateInventoryUI();
        }
    }
    
    private bool MatchesItemName(GameObject item, string targetName)
    {
        if (item == null || string.IsNullOrEmpty(targetName)) return false;
        
        ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
        if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
        {
            return iconProvider.itemName == targetName;
        }
        
        string itemName = item.name.Replace("(Clone)", "").Trim();
        return itemName.Contains(targetName) || targetName.Contains(itemName);
    }
    
    private void DisablePlayerControls()
    {
        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null)
        {
            motor.SetInputActive(false);
        }
        
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(false);
        }
    }
    
    private void EnablePlayerControls()
    {
        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null)
        {
            motor.SetInputActive(true);
        }
        
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(true);
        }
    }
    
    private void UnlockCursorForCrafting()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // If it's still locked, try Confined mode
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        
        // Force unlock again
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator ContinuousCursorUnlock()
    {
        while (isOpen)
        {
            UnlockCursorForCrafting();
            yield return null; // Wait one frame
            UnlockCursorForCrafting();
            yield return new WaitForEndOfFrame(); // Wait until end of frame
            UnlockCursorForCrafting();
        }
    }
}

