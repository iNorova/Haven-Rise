using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PlywoodRepairMiniGame : MonoBehaviour
{
    [Header("UI Settings")]
    public Canvas minigameCanvas;
    public Image woodenBackground; // Background square (wooden texture)
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionsText;
    
    [Header("Gameplay Settings")]
    [Tooltip("Time limit in seconds")]
    public float timeLimit = 60f;
    
    [Tooltip("Number of screws to place and tighten")]
    public int numberOfScrews = 4;
    
    [Tooltip("Number of clicks needed to fully tighten each screw")]
    public int clicksPerScrew = 10;
    
    [Header("Screw Settings")]
    [Tooltip("Sprite for the screw (top view)")]
    public Sprite screwSprite;
    
    [Tooltip("Sprite for the wooden background square")]
    public Sprite woodenSquareSprite;
    
    [Tooltip("Size of each screw in UI")]
    public Vector2 screwSize = new Vector2(60, 60);
    
    [Tooltip("Size of the wooden background square")]
    public Vector2 squareSize = new Vector2(400, 400);
    
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip dragSfx;
    public AudioClip placeSfx;
    public AudioClip clickSfx;
    public AudioClip tightenSfx;
    public AudioClip successSfx;
    public AudioClip failSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    
    private ShipRepairPart currentPart;
    private System.Action<ShipRepairPart, bool> onComplete;
    private bool isActive = false;
    private float timeRemaining;
    private bool isPaused = false;
    private bool hasFailed = false;
    
    // Screw data
    private class ScrewData
    {
        public GameObject screwObj;
        public GameObject holeObj;
        public RectTransform screwRect;
        public RectTransform holeRect; // Reference to the hole rect transform
        public Image screwImage;
        public bool isPlaced = false;
        public bool isTightened = false;
        public int clickCount = 0;
        public float rotationAngle = 0f;
        public int holeIndex; // 0-3 for corners
    }
    
    private List<ScrewData> screws = new List<ScrewData>();
    private List<RectTransform> holeRects = new List<RectTransform>(); // Store hole rects
    private ScrewData currentDraggedScrew = null;
    private bool isDragging = false;
    private RectTransform squareRect; // Cache the square rect transform
    
    // Player input and cursor management
    private CharController_Motor playerMotor;
    private HotbarManager hotbarManager;
    private InventoryUIManager inventoryUIManager;
    private bool wasInputActive = false;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private Coroutine cursorUnlockCoroutine;
    private bool wasInventoryUIManagerEnabled = false;
    
    void Start()
    {
        // Ensure AudioSource
        if (sfxSource == null)
        {
            GameObject audioObj = new GameObject("PlywoodRepairMiniGame_Audio");
            audioObj.transform.SetParent(transform);
            sfxSource = audioObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        
        // Find player components
        playerMotor = FindObjectOfType<CharController_Motor>();
        hotbarManager = FindObjectOfType<HotbarManager>();
        inventoryUIManager = FindObjectOfType<InventoryUIManager>();
    }
    
    void Update()
    {
        // Continuously unlock cursor while minigame is active
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
        
        if (!isActive || isPaused || hasFailed) return;
        
        // Handle dragging
        if (Input.GetMouseButtonDown(0))
        {
            OnMouseDown();
        }
        else if (Input.GetMouseButton(0) && isDragging && currentDraggedScrew != null)
        {
            OnMouseDrag();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnMouseUp();
        }
    }
    
    void LateUpdate()
    {
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
    }
    
    void OnGUI()
    {
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
    }
    
    void FixedUpdate()
    {
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
    }
    
    public void Begin(ShipRepairPart part, System.Action<ShipRepairPart, bool> completeCallback)
    {
        Debug.Log($"PlywoodRepairMiniGame: Begin() called! Part: {part?.partName}, IsActive: {isActive}");
        
        if (isActive)
        {
            Debug.LogWarning("PlywoodRepairMiniGame: Already active! Cannot begin new minigame.");
            return;
        }
        
        if (part == null)
        {
            Debug.LogError("PlywoodRepairMiniGame: Part is null!");
            return;
        }
        
        currentPart = part;
        onComplete = completeCallback;
        
        // Use settings from part if available
        if (part.plywoodTimeLimit > 0)
        {
            timeRemaining = part.plywoodTimeLimit;
        }
        else if (part.timeLimit > 0)
        {
            timeRemaining = part.timeLimit;
        }
        else
        {
            timeRemaining = timeLimit;
            Debug.LogWarning($"PlywoodRepairMiniGame: Part '{part.partName}' has timeLimit = 0. Using default {timeLimit} seconds.");
        }
        
        if (part.numberOfScrews > 0) numberOfScrews = part.numberOfScrews;
        if (part.clicksPerScrew > 0) clicksPerScrew = part.clicksPerScrew;
        
        hasFailed = false;
        isActive = true;
        isPaused = false;
        isDragging = false;
        currentDraggedScrew = null;
        
        Debug.Log($"PlywoodRepairMiniGame: Settings initialized - TimeLimit: {timeRemaining}, Screws: {numberOfScrews}, ClicksPerScrew: {clicksPerScrew}");
        
        // Save current cursor state
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        
        // Disable player movement and input
        DisablePlayerInput();
        
        // Unlock cursor for minigame
        UnlockCursorForMinigame();
        
        // Start continuous cursor unlock coroutine
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
        }
        cursorUnlockCoroutine = StartCoroutine(ContinuousCursorUnlock());
        
        // Create UI
        Debug.Log("PlywoodRepairMiniGame: Creating UI...");
        CreateUI();
        
        // Verify UI was created
        if (minigameCanvas == null)
        {
            Debug.LogError("PlywoodRepairMiniGame: Failed to create UI canvas!");
            CompleteMinigame(false);
            return;
        }
        
        Debug.Log($"PlywoodRepairMiniGame: UI created. Canvas: {minigameCanvas.name}");
        
        // Start the minigame loop
        StartCoroutine(MinigameLoop());
        
        Debug.Log($"PlywoodRepairMiniGame: Started with {timeRemaining} seconds, {numberOfScrews} screws.");
    }
    
    private void CreateUI()
    {
        Debug.Log("PlywoodRepairMiniGame: CreateUI() called");
        
        // Always create a new canvas for the minigame
        if (minigameCanvas == null)
        {
            Debug.Log("PlywoodRepairMiniGame: Creating new canvas...");
            GameObject canvasObj = new GameObject("PlywoodRepairMiniGame_Canvas");
            DontDestroyOnLoad(canvasObj);
            minigameCanvas = canvasObj.AddComponent<Canvas>();
            minigameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            minigameCanvas.sortingOrder = 999;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasObj.SetActive(true);
            Debug.Log($"PlywoodRepairMiniGame: Created new canvas '{canvasObj.name}'");
        }
        
        // Clean up existing panel
        Transform existingPanel = minigameCanvas.transform.Find("MinigamePanel");
        if (existingPanel != null)
        {
            Destroy(existingPanel.gameObject);
        }
        
        // Clear lists
        screws.Clear();
        holeRects.Clear();
        
        // Create main panel
        GameObject panelObj = new GameObject("MinigamePanel");
        panelObj.transform.SetParent(minigameCanvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 600);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        panelObj.SetActive(true);
        
        // Create wooden background square
        GameObject squareObj = new GameObject("WoodenSquare");
        squareObj.transform.SetParent(panelObj.transform, false);
        woodenBackground = squareObj.AddComponent<Image>();
        if (woodenSquareSprite != null)
        {
            woodenBackground.sprite = woodenSquareSprite;
        }
        else
        {
            woodenBackground.color = new Color(0.6f, 0.4f, 0.2f); // Brown color as fallback
        }
        woodenBackground.type = Image.Type.Simple;
        
        squareRect = woodenBackground.rectTransform;
        squareRect.anchorMin = new Vector2(0.5f, 0.5f);
        squareRect.anchorMax = new Vector2(0.5f, 0.5f);
        squareRect.sizeDelta = squareSize;
        squareRect.anchoredPosition = Vector2.zero;
        
        // Create timer text
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(panelObj.transform, false);
        timerText = timerObj.AddComponent<TextMeshProUGUI>();
        RectTransform timerRect = timerText.rectTransform;
        timerRect.anchorMin = new Vector2(0.5f, 0.95f);
        timerRect.anchorMax = new Vector2(0.5f, 0.95f);
        timerRect.sizeDelta = new Vector2(400, 40);
        timerRect.anchoredPosition = Vector2.zero;
        timerText.text = $"Time: {timeRemaining:F1}s";
        timerText.fontSize = 28;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;
        
        // Create instructions text
        GameObject instructionsObj = new GameObject("InstructionsText");
        instructionsObj.transform.SetParent(panelObj.transform, false);
        instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
        RectTransform instructionsRect = instructionsText.rectTransform;
        instructionsRect.anchorMin = new Vector2(0.5f, 0.1f);
        instructionsRect.anchorMax = new Vector2(0.5f, 0.1f);
        instructionsRect.sizeDelta = new Vector2(550, 60);
        instructionsRect.anchoredPosition = Vector2.zero;
        instructionsText.text = "Drag screws to holes, then click to tighten!";
        instructionsText.fontSize = 18;
        instructionsText.alignment = TextAlignmentOptions.Center;
        instructionsText.color = Color.white;
        
        // Create screw holes in corners first
        CreateScrewHoles(panelObj);
        
        // Create draggable screws (scattered around) after holes are created
        CreateScrews(panelObj);
        
        // Link screws to their holes
        for (int i = 0; i < screws.Count && i < holeRects.Count; i++)
        {
            screws[i].holeRect = holeRects[i];
            screws[i].holeIndex = i;
        }
        
        Debug.Log($"PlywoodRepairMiniGame: UI creation complete. Created {screws.Count} screws and {holeRects.Count} holes.");
    }
    
    private void CreateScrewHoles(GameObject parent)
    {
        // Calculate corner positions (relative to square center)
        float halfSize = squareSize.x / 2f - 30f; // Offset from edge
        Vector2[] cornerPositions = new Vector2[]
        {
            new Vector2(-halfSize, halfSize),   // Top-left
            new Vector2(halfSize, halfSize),    // Top-right
            new Vector2(-halfSize, -halfSize),  // Bottom-left
            new Vector2(halfSize, -halfSize)    // Bottom-right
        };
        
        // Store hole references in screws
        for (int i = 0; i < numberOfScrews && i < 4; i++)
        {
            GameObject holeObj = new GameObject($"ScrewHole_{i}");
            holeObj.transform.SetParent(woodenBackground.transform, false);
            
            Image holeImage = holeObj.AddComponent<Image>();
            holeImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark circle for hole
            
            RectTransform holeRect = holeObj.GetComponent<RectTransform>();
            holeRect.anchorMin = new Vector2(0.5f, 0.5f);
            holeRect.anchorMax = new Vector2(0.5f, 0.5f);
            holeRect.sizeDelta = new Vector2(30, 30);
            holeRect.anchoredPosition = cornerPositions[i];
            
            // Create a circle sprite for the hole (simple dark circle)
            Texture2D holeTexture = new Texture2D(30, 30);
            Color[] colors = new Color[30 * 30];
            float centerX = 15f;
            float centerY = 15f;
            float radius = 12f;
            for (int y = 0; y < 30; y++)
            {
                for (int x = 0; x < 30; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    colors[y * 30 + x] = dist <= radius ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.clear;
                }
            }
            holeTexture.SetPixels(colors);
            holeTexture.Apply();
            Sprite holeSprite = Sprite.Create(holeTexture, new Rect(0, 0, 30, 30), new Vector2(0.5f, 0.5f));
            holeImage.sprite = holeSprite;
            
            // Store hole rect reference
            holeRects.Add(holeRect);
        }
    }
    
    private void CreateScrews(GameObject parent)
    {
        screws.Clear();
        
        // Scatter screws randomly around the square (outside the wooden background)
        for (int i = 0; i < numberOfScrews; i++)
        {
            ScrewData screw = new ScrewData();
            screw.holeIndex = i;
            
            GameObject screwObj = new GameObject($"Screw_{i}");
            screwObj.transform.SetParent(minigameCanvas.transform, false);
            
            screw.screwRect = screwObj.AddComponent<RectTransform>();
            screw.screwRect.sizeDelta = screwSize;
            
            screw.screwImage = screwObj.AddComponent<Image>();
            if (screwSprite != null)
            {
                screw.screwImage.sprite = screwSprite;
            }
            else
            {
                // Create a simple screw sprite as fallback (X shape)
                Texture2D screwTexture = new Texture2D((int)screwSize.x, (int)screwSize.y);
                Color[] colors = new Color[(int)(screwSize.x * screwSize.y)];
                float centerX = screwSize.x / 2f;
                float centerY = screwSize.y / 2f;
                for (int y = 0; y < screwSize.y; y++)
                {
                    for (int x = 0; x < screwSize.x; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        if (dist < 25f && (Mathf.Abs(x - centerX) < 3f || Mathf.Abs(y - centerY) < 3f || Mathf.Abs(x - centerX - (y - centerY)) < 2f || Mathf.Abs(x - centerX + (y - centerY)) < 2f))
                        {
                            colors[y * (int)screwSize.x + x] = Color.gray;
                        }
                        else
                        {
                            colors[y * (int)screwSize.x + x] = Color.clear;
                        }
                    }
                }
                screwTexture.SetPixels(colors);
                screwTexture.Apply();
                Sprite fallbackScrew = Sprite.Create(screwTexture, new Rect(0, 0, screwSize.x, screwSize.y), new Vector2(0.5f, 0.5f));
                screw.screwImage.sprite = fallbackScrew;
            }
            
            screw.screwImage.raycastTarget = true;
            screw.screwObj = screwObj;
            
            // Position screws scattered around (random positions outside the square)
            float angle = (360f / numberOfScrews) * i + Random.Range(-30f, 30f);
            float radius = 250f + Random.Range(-50f, 50f);
            float rad = angle * Mathf.Deg2Rad;
            screw.screwRect.anchoredPosition = new Vector2(
                Mathf.Cos(rad) * radius,
                Mathf.Sin(rad) * radius
            );
            
            screws.Add(screw);
        }
    }
    
    private void OnMouseDown()
    {
        Vector2 mousePos = Input.mousePosition;
        
        // First, check if clicking on a PLACED screw (to tighten)
        foreach (var screw in screws)
        {
            if (!screw.isPlaced || screw.isTightened || screw.screwRect == null) continue;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screw.screwRect, mousePos, null, out Vector2 screwLocalPoint);
            if (screw.screwRect.rect.Contains(screwLocalPoint))
            {
                // Clicked on a placed screw - tighten it
                TryTightenScrew();
                return;
            }
        }
        
        // If not a placed screw, check if clicking on an UNPLACED screw (can drag)
        foreach (var screw in screws)
        {
            if (screw.isPlaced || screw.isTightened || screw.screwRect == null) continue;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screw.screwRect, mousePos, null, out Vector2 screwLocalPoint);
            if (screw.screwRect.rect.Contains(screwLocalPoint))
            {
                isDragging = true;
                currentDraggedScrew = screw;
                screw.screwObj.transform.SetAsLastSibling(); // Bring to front
                PlaySfx(dragSfx);
                Debug.Log($"PlywoodRepairMiniGame: Started dragging screw {screws.IndexOf(screw)}");
                return;
            }
        }
    }
    
    private void OnMouseDrag()
    {
        if (!isDragging || currentDraggedScrew == null) return;
        
        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(minigameCanvas.transform as RectTransform, mousePos, null, out Vector2 localPoint);
        currentDraggedScrew.screwRect.anchoredPosition = localPoint;
    }
    
    private void OnMouseUp()
    {
        if (!isDragging || currentDraggedScrew == null)
        {
            isDragging = false;
            currentDraggedScrew = null;
            return;
        }
        
        // Check if dropped over a screw hole
        Vector2 mousePos = Input.mousePosition;
        float closestDist = float.MaxValue;
        int closestHoleIndex = -1;
        
        // Check distance to each hole
        for (int i = 0; i < numberOfScrews && i < screws.Count; i++)
        {
            if (IsHoleOccupied(i)) continue;
            
            ScrewData screw = screws[i];
            if (screw.holeRect == null) continue;
            
            // Convert hole position to screen space for distance calculation
            Vector2 holeScreenPos = RectTransformUtility.WorldToScreenPoint(null, screw.holeRect.position);
            float dist = Vector2.Distance(mousePos, holeScreenPos);
            
            if (dist < closestDist)
            {
                closestDist = dist;
                closestHoleIndex = i;
            }
        }
        
        // If close enough to a hole (larger tolerance - 80 pixels)
        if (closestHoleIndex >= 0 && closestDist < 80f)
        {
            ScrewData targetScrew = screws[closestHoleIndex];
            
            // Place screw in hole
            currentDraggedScrew.isPlaced = true;
            currentDraggedScrew.holeIndex = closestHoleIndex;
            currentDraggedScrew.screwRect.SetParent(squareRect, false);
            currentDraggedScrew.screwRect.anchoredPosition = targetScrew.holeRect.anchoredPosition;
            currentDraggedScrew.screwRect.sizeDelta = screwSize * 0.8f; // Slightly smaller when placed
            PlaySfx(placeSfx);
            Debug.Log($"PlywoodRepairMiniGame: Screw placed in hole {closestHoleIndex}");
        }
        else
        {
            Debug.Log($"PlywoodRepairMiniGame: Screw dropped too far from any hole. Distance: {closestDist}");
        }
        
        isDragging = false;
        currentDraggedScrew = null;
    }
    
    private bool IsHoleOccupied(int holeIndex)
    {
        foreach (var screw in screws)
        {
            if (screw.isPlaced && screw.holeIndex == holeIndex)
            {
                return true;
            }
        }
        return false;
    }
    
    private void TryTightenScrew()
    {
        if (isDragging) return; // Don't tighten while dragging
        
        Vector2 mousePos = Input.mousePosition;
        
        // Check screws - find which one was clicked
        foreach (var screw in screws)
        {
            if (!screw.isPlaced || screw.isTightened || screw.screwRect == null) continue;
            
            // Check if mouse is over this screw
            RectTransformUtility.ScreenPointToLocalPointInRectangle(screw.screwRect, mousePos, null, out Vector2 localPoint);
            
            // Get rect bounds
            Rect rect = screw.screwRect.rect;
            
            if (rect.Contains(localPoint))
            {
                int index = screws.IndexOf(screw);
                screw.clickCount++;
                screw.rotationAngle += 360f / clicksPerScrew; // Rotate by this amount
                screw.screwRect.localRotation = Quaternion.Euler(0, 0, screw.rotationAngle);
                
                PlaySfx(clickSfx);
                Debug.Log($"PlywoodRepairMiniGame: Screw {index} clicked. Count: {screw.clickCount}/{clicksPerScrew}");
                
                if (screw.clickCount >= clicksPerScrew)
                {
                    screw.isTightened = true;
                    PlaySfx(tightenSfx);
                    // Make screw slightly smaller when fully tightened
                    screw.screwRect.sizeDelta = screwSize * 0.7f;
                    Debug.Log($"PlywoodRepairMiniGame: Screw {index} fully tightened!");
                }
                
                return;
            }
        }
        
        Debug.Log("PlywoodRepairMiniGame: TryTightenScrew called but not over any placed screw.");
    }
    
    private IEnumerator MinigameLoop()
    {
        while (isActive && timeRemaining > 0f && !hasFailed)
        {
            if (!isPaused)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimer();
                
                // Check if all screws are tightened
                bool allTightened = true;
                foreach (var screw in screws)
                {
                    if (!screw.isTightened)
                    {
                        allTightened = false;
                        break;
                    }
                }
                
                if (allTightened)
                {
                    CompleteMinigame(true);
                    yield break;
                }
            }
            
            yield return null;
        }
        
        // Time ran out
        if (!hasFailed)
        {
            CompleteMinigame(false);
        }
    }
    
    private void UpdateTimer()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {timeRemaining:F1}s";
            
            if (timeRemaining < 10f)
            {
                timerText.color = Color.red;
            }
            else if (timeRemaining < timeRemaining * 0.5f)
            {
                timerText.color = Color.yellow;
            }
        }
        
        // Update instructions
        if (instructionsText != null)
        {
            int tightenedCount = 0;
            foreach (var screw in screws)
            {
                if (screw.isTightened) tightenedCount++;
            }
            instructionsText.text = $"Screws tightened: {tightenedCount}/{numberOfScrews}. Drag screws to holes, then click to tighten!";
        }
    }
    
    private void CompleteMinigame(bool success)
    {
        isActive = false;
        
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
            cursorUnlockCoroutine = null;
        }
        
        if (success)
        {
            PlaySfx(successSfx);
            Debug.Log("PlywoodRepairMiniGame: Success! All screws tightened.");
        }
        else
        {
            PlaySfx(failSfx);
            Debug.Log("PlywoodRepairMiniGame: Failed! Time ran out.");
        }
        
        // Restore player input and cursor
        RestorePlayerInput();
        
        // Clean up UI
        CleanupUI();
        
        // Call completion callback
        if (onComplete != null)
        {
            onComplete(currentPart, success);
        }
    }
    
    private void CleanupUI()
    {
        if (minigameCanvas != null)
        {
            Transform panel = minigameCanvas.transform.Find("MinigamePanel");
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
            
            // Clean up screws
            foreach (var screw in screws)
            {
                if (screw.screwObj != null)
                {
                    Destroy(screw.screwObj);
                }
            }
            screws.Clear();
        }
    }
    
    private void UnlockCursorForMinigame()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator ContinuousCursorUnlock()
    {
        while (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
            yield return null;
            UnlockCursorForMinigame();
            yield return new WaitForEndOfFrame();
            UnlockCursorForMinigame();
        }
    }
    
    private void DisablePlayerInput()
    {
        if (playerMotor != null)
        {
            wasInputActive = true;
            playerMotor.SetInputActive(false);
        }
        else
        {
            wasInputActive = false;
        }
        
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(false);
        }
        
        if (inventoryUIManager != null)
        {
            wasInventoryUIManagerEnabled = inventoryUIManager.enabled;
            inventoryUIManager.enabled = false;
            Debug.Log("PlywoodRepairMiniGame: Temporarily disabled InventoryUIManager to prevent cursor locking.");
        }
    }
    
    private void RestorePlayerInput()
    {
        if (inventoryUIManager != null && wasInventoryUIManagerEnabled)
        {
            inventoryUIManager.enabled = true;
            Debug.Log("PlywoodRepairMiniGame: Re-enabled InventoryUIManager.");
        }
        
        if (playerMotor != null && wasInputActive)
        {
            playerMotor.SetInputActive(true);
        }
        
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(true);
        }
    }
    
    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
    
    public void Pause()
    {
        isPaused = true;
    }
    
    public void Resume()
    {
        isPaused = false;
    }
    
    public bool IsActive()
    {
        return isActive;
    }
}
