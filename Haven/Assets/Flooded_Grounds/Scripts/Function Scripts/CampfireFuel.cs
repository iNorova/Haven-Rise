using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CookingRecipe
{
    [Tooltip("Name or keyword to identify the input item (e.g., 'meat', 'raw')")]
    public string inputItemName;
    
    [Tooltip("Prefab to spawn as the cooked/result item (e.g., cooked steak)")]
    public GameObject cookedItemPrefab;
    
    [Tooltip("Cooking time in seconds (how long before item transforms)")]
    public float cookingTime = 3f;
    
    [Tooltip("Does this item require the fire to be lit?")]
    public bool requiresFireLit = true;
}

public class CampfireFuel : MonoBehaviour
{
    [Header("Fuel Settings")]
    [Tooltip("Maximum fuel level (100% = fully fueled)")]
    public float maxFuel = 100f;
    [Tooltip("Current fuel level (0-100)")]
    public float currentFuel = 0f;
    [Tooltip("Fuel added per log (5% = 5 units)")]
    public float fuelPerLog = 5f;
    [Tooltip("Fuel consumption rate per second when lit")]
    public float fuelConsumptionRate = 0.5f; // 0.5% per second = 200 seconds for full burn
    
    [Header("Interaction Settings")]
    [Tooltip("Range within which wood can be added to campfire")]
    public float interactionRange = 2f;
    [Tooltip("Contact distance - wood must be within this distance to be consumed")]
    public float contactDistance = 0.5f;
    [Tooltip("Keywords to identify wood items (case-insensitive)")]
    public string[] woodKeywords = { "wood", "log", "stick" };
    [Tooltip("Check interval for wood contact detection (lower = more responsive, higher = better performance)")]
    public float contactCheckInterval = 0.1f;
    
    [Header("Visual Settings")]
    [Tooltip("Offset above campfire for fuel bar")]
    public Vector3 fuelBarOffset = new Vector3(0, 2f, 0);
    [Tooltip("Size of the fuel bar")]
    public Vector2 fuelBarSize = new Vector2(400f, 40f);
    [Tooltip("Scale multiplier for the fuel bar (higher = bigger)")]
    [Range(0.5f, 3f)]
    public float fuelBarScale = 1.5f;
    
    [Header("Fire Effects")]
    [Tooltip("GameObjects that represent the fire (will be enabled/disabled based on lit state). Leave empty if no visual effects needed.")]
    public GameObject[] fireEffects;
    [Tooltip("Particle systems for fire effects. Leave empty if no particles needed.")]
    public ParticleSystem[] fireParticles;
    [Tooltip("Light component for fire illumination. Will be created automatically if not assigned.")]
    public Light fireLight;
    [Tooltip("Light intensity when fire is lit")]
    public float lightIntensity = 2f;
    [Tooltip("Light range when fire is lit")]
    public float lightRange = 10f;
    [Tooltip("Light color (warm orange/yellow for fire)")]
    public Color lightColor = new Color(1f, 0.5f, 0.2f); // Warm orange
    
    [Header("Cooking System")]
    [Tooltip("List of cooking recipes - items that can be cooked on the campfire")]
    public CookingRecipe[] cookingRecipes = new CookingRecipe[0];
    
    [Tooltip("Sound effect to play when cooking starts")]
    public AudioClip cookingStartSfx;
    
    [Tooltip("Sound effect to play when item is cooked (transforms)")]
    public AudioClip cookingCompleteSfx;
    
    [Tooltip("Volume for cooking sound effects (0-1)")]
    [Range(0f, 1f)]
    public float cookingSfxVolume = 0.7f;
    
    [Header("Audio")]
    [Tooltip("Audio source for playing sound effects. Will be created automatically if not assigned.")]
    public AudioSource sfxSource;
    [Tooltip("Audio source for looping campfire sounds. Will be created automatically if not assigned.")]
    public AudioSource loopSource;
    [Tooltip("Sound effect to play when wood is added to the campfire")]
    public AudioClip addWoodSfx;
    [Tooltip("Volume for the add wood sound effect (0-1)")]
    [Range(0f, 1f)]
    public float addWoodSfxVolume = 0.7f;
    [Tooltip("Sound effect to play when the campfire is first lit")]
    public AudioClip fireLitSfx;
    [Tooltip("Volume for the fire lit sound effect (0-1)")]
    [Range(0f, 1f)]
    public float fireLitSfxVolume = 0.8f;
    [Tooltip("Looping campfire sound (crackling, etc.) that plays while fire is lit")]
    public AudioClip campfireLoopSfx;
    [Tooltip("Volume for the looping campfire sound (0-1)")]
    [Range(0f, 1f)]
    public float campfireLoopVolume = 0.6f;
    
    // UI Elements
    private Canvas fuelBarCanvas;
    private GameObject fuelBarUI;
    private Slider fuelBarSlider;
    private Image fuelBarFill;
    private TextMeshProUGUI fuelBarText;
    
    // State
    private bool isLit = false;
    private Camera playerCamera;
    private float contactCheckTimer = 0f;
    private System.Collections.Generic.HashSet<GameObject> processedWood = new System.Collections.Generic.HashSet<GameObject>(); // Track wood we've already processed
    
    // Cooking system
    [System.Serializable]
    public class CookingItem
    {
        public GameObject item;
        public CookingRecipe recipe;
        public float cookTimer;
    }
    private System.Collections.Generic.List<CookingItem> itemsBeingCooked = new System.Collections.Generic.List<CookingItem>();
    private System.Collections.Generic.HashSet<GameObject> processedCookableItems = new System.Collections.Generic.HashSet<GameObject>(); // Track items being cooked
    
    // Track previous values for change detection
    private Vector2 previousFuelBarSize;
    private float previousFuelBarScale;
    
    void Start()
    {
        playerCamera = Camera.main;
        
        // Create fire light if not assigned
        if (fireLight == null)
        {
            CreateFireLight();
        }
        
        // Create audio sources if not assigned
        if (sfxSource == null)
        {
            CreateAudioSource();
        }
        
        if (loopSource == null)
        {
            CreateLoopAudioSource();
        }
        
        // Initialize fuel bar UI
        CreateFuelBarUI();
        
        // Start unlit
        SetLitState(false);
        
        // Update initial fuel bar
        UpdateFuelBar();
        
        // Store initial values
        previousFuelBarSize = fuelBarSize;
        previousFuelBarScale = fuelBarScale;
    }
    
    void CreateFireLight()
    {
        // Create a light object for the fire
        GameObject lightObj = new GameObject("FireLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, 0.5f, 0); // Slightly above the campfire
        
        fireLight = lightObj.AddComponent<Light>();
        fireLight.type = LightType.Point;
        fireLight.intensity = lightIntensity;
        fireLight.range = lightRange;
        fireLight.color = lightColor;
        fireLight.shadows = LightShadows.Soft;
        
        // Start with light disabled (fire is unlit)
        fireLight.enabled = false;
        
        Debug.Log("CampfireFuel: Created fire light automatically.");
    }
    
    void CreateAudioSource()
    {
        // Create an audio source for sound effects
        GameObject audioObj = new GameObject("CampfireAudioSource");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
        
        sfxSource = audioObj.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 1f; // 3D sound
        sfxSource.minDistance = 5f;
        sfxSource.maxDistance = 20f;
        sfxSource.rolloffMode = AudioRolloffMode.Logarithmic;
        
        Debug.Log("CampfireFuel: Created audio source automatically.");
    }
    
    void CreateLoopAudioSource()
    {
        // Create an audio source for looping campfire sounds
        GameObject audioObj = new GameObject("CampfireLoopAudioSource");
        audioObj.transform.SetParent(transform);
        audioObj.transform.localPosition = Vector3.zero;
        
        loopSource = audioObj.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
        loopSource.loop = true; // Enable looping
        loopSource.spatialBlend = 1f; // 3D sound
        loopSource.minDistance = 5f;
        loopSource.maxDistance = 25f; // Slightly further range for ambient sound
        loopSource.rolloffMode = AudioRolloffMode.Logarithmic;
        
        Debug.Log("CampfireFuel: Created loop audio source automatically.");
    }
    
    void Update()
    {
        // Check for wood contact continuously (throttled for performance)
        contactCheckTimer += Time.deltaTime;
        if (contactCheckTimer >= contactCheckInterval)
        {
            contactCheckTimer = 0f;
            CheckForWoodContact();
            CheckForCookableItems(); // Also check for cookable items
        }
        
        // Also check on mouse release (for drag-and-drop)
        if (Input.GetMouseButtonUp(0))
        {
            CheckForWoodDrop();
            CheckForCookableItemsDrop(); // Check for dropped cookable items
        }
        
        // Update cooking items
        UpdateCookingItems();
        
        // Consume fuel if lit
        if (isLit && currentFuel > 0f)
        {
            ConsumeFuel();
            
            // Update light intensity based on fuel level
            if (fireLight != null && fireLight.enabled)
            {
                float fuelPercent = currentFuel / maxFuel;
                fireLight.intensity = lightIntensity * Mathf.Lerp(0.3f, 1f, fuelPercent);
                fireLight.range = lightRange * Mathf.Lerp(0.5f, 1f, fuelPercent);
            }
        }
        else if (isLit && currentFuel <= 0f)
        {
            // Fire went out
            SetLitState(false);
        }
        
        // Check if fuel bar size or scale changed in Inspector
        if (fuelBarSize != previousFuelBarSize || fuelBarScale != previousFuelBarScale)
        {
            UpdateFuelBarSize();
            previousFuelBarSize = fuelBarSize;
            previousFuelBarScale = fuelBarScale;
        }
        
        // Update fuel bar position to face camera
        if (fuelBarCanvas != null && playerCamera != null)
        {
            fuelBarCanvas.transform.LookAt(fuelBarCanvas.transform.position + playerCamera.transform.rotation * Vector3.forward, playerCamera.transform.rotation * Vector3.up);
        }
    }
    
    void CheckForWoodContact()
    {
        // Check for wood objects in contact with the campfire
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, contactDistance);
        
        foreach (Collider col in nearbyObjects)
        {
            GameObject obj = col.gameObject;
            
            // Skip if it's the campfire itself
            if (obj == gameObject || obj.transform.IsChildOf(transform))
                continue;
            
            // Skip if we've already processed this wood object
            if (processedWood.Contains(obj))
                continue;
            
            // Skip if it's being cooked (cooking takes priority over fuel)
            if (processedCookableItems.Contains(obj))
                continue;
            
            // Check if it's a cookable item first (cooking takes priority)
            CookingRecipe recipe = GetCookingRecipe(obj);
            if (recipe != null)
                continue; // Let CheckForCookableItems handle it
            
            // Check if it's wood
            if (IsWoodItem(obj))
            {
                float distance = Vector3.Distance(obj.transform.position, transform.position);
                
                // If wood is in contact range, add it as fuel
                if (distance <= contactDistance)
                {
                    AddFuel(obj);
                    processedWood.Add(obj); // Mark as processed
                    return; // Only process one wood per check
                }
            }
        }
        
        // Clean up destroyed objects from processed set
        processedWood.RemoveWhere(wood => wood == null);
    }
    
    void CreateFuelBarUI()
    {
        // Create canvas for fuel bar
        GameObject canvasObj = new GameObject("CampfireFuelBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = fuelBarOffset;
        canvasObj.transform.localRotation = Quaternion.identity;
        
        fuelBarCanvas = canvasObj.AddComponent<Canvas>();
        fuelBarCanvas.renderMode = RenderMode.WorldSpace;
        fuelBarCanvas.worldCamera = Camera.main;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Create background panel
        GameObject panelObj = new GameObject("FuelBarPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = fuelBarSize;
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f); // Semi-transparent black background
        
        // Create slider
        GameObject sliderObj = new GameObject("FuelBarSlider");
        sliderObj.transform.SetParent(panelObj.transform, false);
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.anchoredPosition = Vector2.zero;
        
        fuelBarSlider = sliderObj.AddComponent<Slider>();
        fuelBarSlider.minValue = 0f;
        fuelBarSlider.maxValue = maxFuel;
        fuelBarSlider.value = currentFuel;
        
        // Create fill area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        
        fuelBarFill = fillObj.AddComponent<Image>();
        fuelBarFill.color = Color.red; // Red for fire
        fuelBarSlider.fillRect = fillRect;
        
        // Create text
        GameObject textObj = new GameObject("FuelBarText");
        textObj.transform.SetParent(panelObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        fuelBarText = textObj.AddComponent<TextMeshProUGUI>();
        fuelBarText.text = "Fuel: 0%";
        fuelBarText.fontSize = 14;
        fuelBarText.color = Color.white;
        fuelBarText.alignment = TextAlignmentOptions.Center;
        
        // Scale canvas to world size - use fuelBarScale multiplier
        float scale = 0.001f * fuelBarScale; // Adjust this to make the UI larger/smaller
        canvasObj.transform.localScale = Vector3.one * scale;
        
        fuelBarUI = panelObj;
    }
    
    void OnValidate()
    {
        // Update fuel bar size/scale when values change in Inspector (only works in editor during play)
        // Don't call UpdateFuelBarSize here to avoid issues - it will be detected in Update()
    }
    
    void UpdateFuelBarSize()
    {
        // Safety check - only update if UI is created
        if (fuelBarUI == null || fuelBarCanvas == null || !Application.isPlaying)
            return;
        
        try
        {
            // Update panel size
            RectTransform panelRect = fuelBarUI.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.sizeDelta = fuelBarSize;
            }
            
            // Update canvas scale
            if (fuelBarCanvas.transform != null)
            {
                float scale = 0.001f * fuelBarScale;
                fuelBarCanvas.transform.localScale = Vector3.one * scale;
            }
            
            // Update text font size to match new scale
            if (fuelBarText != null)
            {
                // Scale font size proportionally
                fuelBarText.fontSize = 14 * fuelBarScale;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"CampfireFuel: Error updating fuel bar size: {e.Message}");
        }
    }
    
    void CheckForWoodDrop()
    {
        // Check for wood objects near the campfire (for drag-and-drop)
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        
        GameObject closestWood = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider col in nearbyObjects)
        {
            GameObject obj = col.gameObject;
            
            // Skip if it's the campfire itself
            if (obj == gameObject || obj.transform.IsChildOf(transform))
                continue;
            
            // Skip if already processed
            if (processedWood.Contains(obj))
                continue;
            
            // Skip if it's being cooked (cooking takes priority over fuel)
            if (processedCookableItems.Contains(obj))
                continue;
            
            // Check if it's a cookable item first (cooking takes priority)
            CookingRecipe recipe = GetCookingRecipe(obj);
            if (recipe != null)
                continue; // Let CheckForCookableItemsDrop handle it
            
            if (IsWoodItem(obj))
            {
                float distance = Vector3.Distance(obj.transform.position, transform.position);
                
                // Find the closest wood item within drop range
                if (distance < interactionRange * 0.5f && distance < closestDistance)
                {
                    closestWood = obj;
                    closestDistance = distance;
                }
            }
        }
        
        // If we found wood close enough, add it as fuel
        if (closestWood != null)
        {
            AddFuel(closestWood);
            processedWood.Add(closestWood); // Mark as processed
        }
    }
    
    bool IsWoodItem(GameObject obj)
    {
        // Check by name (case-insensitive)
        string objName = obj.name.ToLower();
        foreach (string keyword in woodKeywords)
        {
            if (objName.Contains(keyword.ToLower()))
            {
                return true;
            }
        }
        
        // Check by ItemIconProvider
        ItemIconProvider iconProvider = obj.GetComponent<ItemIconProvider>();
        if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
        {
            string itemName = iconProvider.itemName.ToLower();
            foreach (string keyword in woodKeywords)
            {
                if (itemName.Contains(keyword.ToLower()))
                {
                    return true;
                }
            }
        }
        
        // Check parent objects
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            foreach (string keyword in woodKeywords)
            {
                if (parentName.Contains(keyword.ToLower()))
                {
                    return true;
                }
            }
            parent = parent.parent;
        }
        
        return false;
    }
    
    void AddFuel(GameObject woodObject)
    {
        if (woodObject == null) return;
        
        // Add fuel
        currentFuel = Mathf.Min(currentFuel + fuelPerLog, maxFuel);
        
        // Play sound effect when wood is added
        PlayAddWoodSfx();
        
        // Light the fire if it's the first log
        if (!isLit && currentFuel > 0f)
        {
            SetLitState(true);
        }
        
        // Mark as processed before destroying
        processedWood.Add(woodObject);
        
        // Destroy the wood object
        Destroy(woodObject);
        
        // Update fuel bar
        UpdateFuelBar();
        
        Debug.Log($"CampfireFuel: Added {fuelPerLog} fuel. Current fuel: {currentFuel}%");
    }
    
    void PlayAddWoodSfx()
    {
        if (sfxSource != null && addWoodSfx != null)
        {
            sfxSource.PlayOneShot(addWoodSfx, addWoodSfxVolume);
        }
    }
    
    void ConsumeFuel()
    {
        currentFuel = Mathf.Max(currentFuel - fuelConsumptionRate * Time.deltaTime, 0f);
        UpdateFuelBar();
    }
    
    void CheckForCookableItems()
    {
        // Check for cookable items in contact with the campfire
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, contactDistance);
        
        foreach (Collider col in nearbyObjects)
        {
            GameObject obj = col.gameObject;
            
            // Skip if it's the campfire itself
            if (obj == gameObject || obj.transform.IsChildOf(transform))
                continue;
            
            // Skip if already being cooked
            if (processedCookableItems.Contains(obj))
                continue;
            
            // Skip if already processed as wood
            if (processedWood.Contains(obj))
                continue;
            
            // Check if it's a cookable item
            CookingRecipe recipe = GetCookingRecipe(obj);
            if (recipe != null)
            {
                // Check if fire needs to be lit
                if (recipe.requiresFireLit && !isLit)
                    continue;
                
                float distance = Vector3.Distance(obj.transform.position, transform.position);
                
                // If item is in contact range, start cooking it
                if (distance <= contactDistance)
                {
                    StartCookingItem(obj, recipe);
                    return; // Only process one item per check
                }
            }
        }
        
        // Clean up destroyed objects from processed set
        processedCookableItems.RemoveWhere(item => item == null);
    }
    
    void CheckForCookableItemsDrop()
    {
        // Check for cookable items near the campfire (for drag-and-drop)
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRange);
        
        GameObject closestCookable = null;
        CookingRecipe closestRecipe = null;
        float closestDistance = float.MaxValue;
        
        foreach (Collider col in nearbyObjects)
        {
            GameObject obj = col.gameObject;
            
            // Skip if it's the campfire itself
            if (obj == gameObject || obj.transform.IsChildOf(transform))
                continue;
            
            // Skip if already being processed
            if (processedCookableItems.Contains(obj) || processedWood.Contains(obj))
                continue;
            
            CookingRecipe recipe = GetCookingRecipe(obj);
            if (recipe != null)
            {
                // Check if fire needs to be lit
                if (recipe.requiresFireLit && !isLit)
                    continue;
                
                float distance = Vector3.Distance(obj.transform.position, transform.position);
                
                // Find the closest cookable item within drop range
                if (distance < interactionRange * 0.5f && distance < closestDistance)
                {
                    closestCookable = obj;
                    closestRecipe = recipe;
                    closestDistance = distance;
                }
            }
        }
        
        // If we found a cookable item close enough, start cooking it
        if (closestCookable != null && closestRecipe != null)
        {
            StartCookingItem(closestCookable, closestRecipe);
        }
    }
    
    CookingRecipe GetCookingRecipe(GameObject obj)
    {
        if (cookingRecipes == null || cookingRecipes.Length == 0)
            return null;
        
        string objName = obj.name.ToLower();
        string itemName = "";
        
        // Check ItemIconProvider first
        ItemIconProvider iconProvider = obj.GetComponent<ItemIconProvider>();
        if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
        {
            itemName = iconProvider.itemName.ToLower();
        }
        
        // Check all recipes
        foreach (CookingRecipe recipe in cookingRecipes)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.inputItemName))
                continue;
            
            string recipeName = recipe.inputItemName.ToLower();
            
            // Check if object name matches recipe
            if (objName.Contains(recipeName))
                return recipe;
            
            // Check if item name matches recipe
            if (!string.IsNullOrEmpty(itemName) && itemName.Contains(recipeName))
                return recipe;
            
            // Check parent objects
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                string parentName = parent.name.ToLower();
                if (parentName.Contains(recipeName))
                    return recipe;
                parent = parent.parent;
            }
        }
        
        return null;
    }
    
    void StartCookingItem(GameObject item, CookingRecipe recipe)
    {
        if (item == null)
        {
            Debug.LogWarning("CampfireFuel: StartCookingItem - item is null!");
            return;
        }
        
        if (recipe == null)
        {
            Debug.LogWarning($"CampfireFuel: StartCookingItem - recipe is null for item '{item.name}'!");
            return;
        }
        
        if (recipe.cookedItemPrefab == null)
        {
            Debug.LogError($"CampfireFuel: StartCookingItem - cookedItemPrefab is null for recipe '{recipe.inputItemName}'! Please assign a prefab in the Inspector.");
            return;
        }
        
        // Mark as being cooked
        processedCookableItems.Add(item);
        
        // Create cooking item entry
        CookingItem cookingItem = new CookingItem
        {
            item = item,
            recipe = recipe,
            cookTimer = 0f
        };
        itemsBeingCooked.Add(cookingItem);
        
        // Play cooking start sound
        if (sfxSource != null && cookingStartSfx != null)
        {
            sfxSource.PlayOneShot(cookingStartSfx, cookingSfxVolume);
        }
        
        Debug.Log($"<color=yellow>CampfireFuel: Started cooking '{item.name}' -> '{recipe.cookedItemPrefab.name}' (cooking time: {recipe.cookingTime}s)</color>");
    }
    
    void UpdateCookingItems()
    {
        // Update all cooking items
        for (int i = itemsBeingCooked.Count - 1; i >= 0; i--)
        {
            CookingItem cookingItem = itemsBeingCooked[i];
            
            // Check if item still exists
            if (cookingItem.item == null)
            {
                itemsBeingCooked.RemoveAt(i);
                continue;
            }
            
            // Check if fire is still lit (if required)
            if (cookingItem.recipe.requiresFireLit && !isLit)
            {
                // Fire went out - stop cooking
                processedCookableItems.Remove(cookingItem.item);
                itemsBeingCooked.RemoveAt(i);
                Debug.Log($"CampfireFuel: Cooking stopped - fire went out for '{cookingItem.item.name}'");
                continue;
            }
            
            // Update cooking timer (modify the item in the list directly)
            cookingItem.cookTimer += Time.deltaTime;
            itemsBeingCooked[i] = cookingItem; // Update the list with modified timer
            
            // Debug progress
            float progress = cookingItem.cookTimer / cookingItem.recipe.cookingTime;
            if (progress >= 0.5f && progress < 0.51f) // Log once when halfway
            {
                Debug.Log($"CampfireFuel: Cooking '{cookingItem.item.name}' - {progress * 100f:F0}% complete");
            }
            
            // Check if cooking is complete
            if (cookingItem.cookTimer >= cookingItem.recipe.cookingTime)
            {
                Debug.Log($"CampfireFuel: Cooking complete! Timer: {cookingItem.cookTimer:F2}s / {cookingItem.recipe.cookingTime:F2}s");
                CompleteCooking(cookingItem);
                itemsBeingCooked.RemoveAt(i);
            }
        }
    }
    
    void CompleteCooking(CookingItem cookingItem)
    {
        if (cookingItem.item == null)
        {
            Debug.LogWarning("CampfireFuel: CompleteCooking - item is null!");
            return;
        }
        
        if (cookingItem.recipe == null)
        {
            Debug.LogWarning($"CampfireFuel: CompleteCooking - recipe is null for item '{cookingItem.item.name}'!");
            return;
        }
        
        if (cookingItem.recipe.cookedItemPrefab == null)
        {
            Debug.LogError($"CampfireFuel: CompleteCooking - cookedItemPrefab is null for recipe '{cookingItem.recipe.inputItemName}'! Please assign a prefab in the Inspector.");
            return;
        }
        
        // Get position and rotation of the item being cooked
        Vector3 cookedPosition = cookingItem.item.transform.position;
        Quaternion cookedRotation = cookingItem.item.transform.rotation;
        
        Debug.Log($"CampfireFuel: Spawning cooked item '{cookingItem.recipe.cookedItemPrefab.name}' at position {cookedPosition}");
        
        // Spawn the cooked item
        GameObject cookedItem = Instantiate(cookingItem.recipe.cookedItemPrefab, cookedPosition, cookedRotation);
        
        // Make sure the cooked item is active
        cookedItem.SetActive(true);
        
        // Copy physics properties if both have rigidbodies
        Rigidbody originalRb = cookingItem.item.GetComponent<Rigidbody>();
        Rigidbody cookedRb = cookedItem.GetComponent<Rigidbody>();
        if (originalRb != null && cookedRb != null)
        {
            cookedRb.linearVelocity = originalRb.linearVelocity;
            cookedRb.angularVelocity = originalRb.angularVelocity;
            cookedRb.isKinematic = originalRb.isKinematic;
            cookedRb.useGravity = originalRb.useGravity;
        }
        else if (originalRb != null && cookedRb == null)
        {
            // If original had physics but cooked doesn't, add a rigidbody
            cookedRb = cookedItem.AddComponent<Rigidbody>();
            cookedRb.linearVelocity = originalRb.linearVelocity;
            cookedRb.angularVelocity = originalRb.angularVelocity;
            cookedRb.isKinematic = originalRb.isKinematic;
            cookedRb.useGravity = originalRb.useGravity;
        }
        
        // Ensure cooked item has colliders enabled
        Collider[] cookedColliders = cookedItem.GetComponentsInChildren<Collider>();
        foreach (Collider col in cookedColliders)
        {
            if (col != null)
                col.enabled = true;
        }
        
        // Play cooking complete sound
        if (sfxSource != null && cookingCompleteSfx != null)
        {
            sfxSource.PlayOneShot(cookingCompleteSfx, cookingSfxVolume);
        }
        
        // Store reference to item for cleanup
        GameObject itemToDestroy = cookingItem.item;
        
        // Destroy the original item
        Destroy(itemToDestroy);
        
        // Remove from processed set
        processedCookableItems.Remove(itemToDestroy);
        
        Debug.Log($"<color=green>CampfireFuel: ✓ Completed cooking! Created '{cookedItem.name}' at {cookedPosition}</color>");
    }
    
    void SetLitState(bool lit)
    {
        isLit = lit;
        
        // Enable/disable fire effects (only if array is assigned)
        if (fireEffects != null && fireEffects.Length > 0)
        {
            foreach (GameObject fireEffect in fireEffects)
            {
                if (fireEffect != null)
                {
                    fireEffect.SetActive(lit);
                }
            }
        }
        
        // Enable/disable particle systems (only if array is assigned)
        if (fireParticles != null && fireParticles.Length > 0)
        {
            foreach (ParticleSystem particles in fireParticles)
            {
                if (particles != null)
                {
                    if (lit)
                    {
                        particles.Play();
                    }
                    else
                    {
                        particles.Stop();
                    }
                }
            }
        }
        
        // Enable/disable fire light
        if (fireLight != null)
        {
            fireLight.enabled = lit;
            
            // Adjust light intensity based on fuel level (dimmer when low fuel)
            if (lit)
            {
                float fuelPercent = currentFuel / maxFuel;
                fireLight.intensity = lightIntensity * Mathf.Lerp(0.3f, 1f, fuelPercent); // Dims as fuel runs out
                fireLight.range = lightRange * Mathf.Lerp(0.5f, 1f, fuelPercent); // Smaller range when low fuel
            }
        }
        
        // Handle audio
        if (lit)
        {
            // Play fire lit sound effect
            PlayFireLitSfx();
            
            // Start looping campfire sound
            StartCampfireLoop();
        }
        else
        {
            // Stop looping campfire sound
            StopCampfireLoop();
        }
        
        // Show/hide fuel bar (only show when lit or has fuel)
        if (fuelBarUI != null)
        {
            fuelBarUI.SetActive(lit || currentFuel > 0f);
        }
        
        Debug.Log($"CampfireFuel: Fire is now {(lit ? "LIT" : "UNLIT")}");
    }
    
    void PlayFireLitSfx()
    {
        if (sfxSource != null && fireLitSfx != null)
        {
            sfxSource.PlayOneShot(fireLitSfx, fireLitSfxVolume);
        }
    }
    
    void StartCampfireLoop()
    {
        if (loopSource != null && campfireLoopSfx != null)
        {
            if (!loopSource.isPlaying)
            {
                loopSource.clip = campfireLoopSfx;
                loopSource.volume = campfireLoopVolume;
                loopSource.Play();
            }
        }
    }
    
    void StopCampfireLoop()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
        }
    }
    
    void UpdateFuelBar()
    {
        if (fuelBarSlider != null)
        {
            fuelBarSlider.value = currentFuel;
        }
        
        if (fuelBarText != null)
        {
            fuelBarText.text = $"Fuel: {currentFuel:F1}%";
        }
        
        // Update fill color based on fuel level
        if (fuelBarFill != null)
        {
            float fuelPercent = currentFuel / maxFuel;
            if (fuelPercent > 0.6f)
            {
                fuelBarFill.color = Color.red; // High fuel = bright red
            }
            else if (fuelPercent > 0.3f)
            {
                fuelBarFill.color = Color.yellow; // Medium fuel = yellow
            }
            else
            {
                fuelBarFill.color = new Color(1f, 0.5f, 0f); // Low fuel = orange (RGB)
            }
        }
    }
    
    // Visualize interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f); // Orange color
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw drop zone (smaller)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, interactionRange * 0.5f);
        
        // Draw contact distance (green - where wood gets consumed)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, contactDistance);
    }
}

