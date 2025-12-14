using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PropellerRepairMiniGame : MonoBehaviour
{
    [Header("UI Settings")]
    public Canvas minigameCanvas;           // Optional: If null, created automatically
    public Slider progressBar;              // Progress bar slider
    public Image progressBarFill;           // Fill image of the progress bar
    public TextMeshProUGUI timerText;       // Timer display
    public TextMeshProUGUI instructionsText; // Instructions display
    public TextMeshProUGUI clickCountText;  // Shows clicks per second or instructions
    
    [Header("Gameplay Settings")]
    [Tooltip("Time limit in seconds")]
    public float timeLimit = 15f;
    
    [Tooltip("Points added per click")]
    public float pointsPerClick = 10f;
    
    [Tooltip("Points lost per second (decay rate)")]
    public float decayRate = 15f;
    
    [Tooltip("Minimum points needed to win (out of 100)")]
    public float targetPoints = 80f;
    
    [Tooltip("Critical failure threshold (if bar drops below this, instant fail)")]
    public float failureThreshold = 20f;
    
    [Header("Bar Colors")]
    [Tooltip("Color when in green/success zone")]
    public Color greenZoneColor = Color.green;
    
    [Tooltip("Color when in yellow/warning zone")]
    public Color yellowZoneColor = Color.yellow;
    
    [Tooltip("Color when in red/danger zone")]
    public Color redZoneColor = Color.red;
    
    [Tooltip("Green zone starts at this percentage (0-1)")]
    [Range(0f, 1f)]
    public float greenZoneStart = 0.7f;
    
    [Tooltip("Yellow zone starts at this percentage (0-1)")]
    [Range(0f, 1f)]
    public float yellowZoneStart = 0.4f;
    
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip clickSfx;
    public AudioClip successSfx;
    public AudioClip failSfx;
    public AudioClip warningSfx;  // Play when entering red zone
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    
    private ShipRepairPart currentPart;
    private System.Action<ShipRepairPart, bool> onComplete;
    private bool isActive = false;
    private float timeRemaining;
    private float currentPoints = 0f;  // 0-100 range
    private bool isPaused = false;
    private bool hasFailed = false;
    private int clickCount = 0;
    private float clickCountResetTimer = 0f;
    private float lastClickTime = 0f;
    private float clicksPerSecond = 0f;
    
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
            GameObject audioObj = new GameObject("PropellerRepairMiniGame_Audio");
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
        
        // Handle clicks during active minigame
        if (isActive && !isPaused && !hasFailed)
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                OnClick();
            }
            
            // Update click rate calculation
            clickCountResetTimer += Time.deltaTime;
            if (clickCountResetTimer >= 1f)
            {
                clicksPerSecond = clickCount;
                clickCount = 0;
                clickCountResetTimer = 0f;
            }
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
        Debug.Log($"PropellerRepairMiniGame: Begin() called! Part: {part?.partName}, IsActive: {isActive}");
        
        if (isActive)
        {
            Debug.LogWarning("PropellerRepairMiniGame: Already active! Cannot begin new minigame.");
            return;
        }
        
        if (part == null)
        {
            Debug.LogError("PropellerRepairMiniGame: Part is null!");
            return;
        }
        
        currentPart = part;
        onComplete = completeCallback;
        
        // Use settings from part if available, otherwise use component defaults
        if (part.timeLimit > 0)
        {
            timeRemaining = part.timeLimit;
        }
        else
        {
            timeRemaining = timeLimit; // Use component default
            Debug.LogWarning($"PropellerRepairMiniGame: Part '{part.partName}' has timeLimit = 0. Using default {timeLimit} seconds.");
        }
        
        if (part.targetPoints > 0) targetPoints = part.targetPoints;
        if (part.pointsPerClick > 0) pointsPerClick = part.pointsPerClick;
        if (part.decayRate > 0) decayRate = part.decayRate;
        if (part.failureThreshold > 0) failureThreshold = part.failureThreshold;
        
        // Start with points slightly above failure threshold, so player has time to react
        // This prevents instant failure when the minigame starts
        // Use failureThreshold + 5 as starting point (or 25 if threshold is 20)
        currentPoints = Mathf.Max(failureThreshold + 5f, 25f);
        clickCount = 0;
        clicksPerSecond = 0f;
        hasFailed = false;
        isActive = true;
        isPaused = false;
        
        Debug.Log($"PropellerRepairMiniGame: Settings initialized - TimeLimit: {timeRemaining}, Target: {targetPoints}, PointsPerClick: {pointsPerClick}");
        
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
        Debug.Log("PropellerRepairMiniGame: Creating UI...");
        CreateUI();
        
        // Verify UI was created
        if (minigameCanvas == null)
        {
            Debug.LogError("PropellerRepairMiniGame: Failed to create UI canvas!");
            CompleteMinigame(false);
            return;
        }
        
        Debug.Log($"PropellerRepairMiniGame: UI created. Canvas: {minigameCanvas.name} (Active: {minigameCanvas.gameObject.activeInHierarchy}, Enabled: {minigameCanvas.enabled}), ProgressBar: {progressBar != null}, TimerText: {timerText != null}, InstructionsText: {instructionsText != null}, ClickCountText: {clickCountText != null}");
        
        if (minigameCanvas != null)
        {
            Debug.Log($"PropellerRepairMiniGame: Canvas sortingOrder: {minigameCanvas.sortingOrder}, RenderMode: {minigameCanvas.renderMode}");
        }
        
        // Start the minigame loop
        StartCoroutine(MinigameLoop());
        
        Debug.Log($"PropellerRepairMiniGame: Started with {timeRemaining} seconds, target: {targetPoints} points.");
    }
    
    private void OnClick()
    {
        if (hasFailed || !isActive) return;
        
        // Add points per click
        currentPoints = Mathf.Clamp(currentPoints + pointsPerClick, 0f, 100f);
        clickCount++;
        lastClickTime = Time.time;
        
        // Play click sound
        PlaySfx(clickSfx);
        
        // Update UI
        UpdateProgressBar();
    }
    
    private IEnumerator MinigameLoop()
    {
        while (isActive && timeRemaining > 0f && !hasFailed)
        {
            if (!isPaused)
            {
                // Decay points over time
                currentPoints = Mathf.Max(0f, currentPoints - (decayRate * Time.deltaTime));
                
                // Update time
                timeRemaining -= Time.deltaTime;
                
                // Update UI
                UpdateProgressBar();
                UpdateTimer();
                UpdateClickCountDisplay();
                
                // Check for failure (points dropped below threshold)
                if (currentPoints < failureThreshold)
                {
                    FailMinigame();
                    yield break;
                }
                
                // Check for success (points reached target and time still remaining)
                if (currentPoints >= targetPoints)
                {
                    CompleteMinigame(true);
                    yield break;
                }
            }
            
            yield return null;
        }
        
        // Time ran out or loop ended
        if (!hasFailed)
        {
            if (currentPoints >= targetPoints)
            {
                CompleteMinigame(true);
            }
            else
            {
                FailMinigame();
            }
        }
    }
    
    private void UpdateProgressBar()
    {
        if (progressBar != null)
        {
            progressBar.value = currentPoints / 100f; // Normalize to 0-1
        }
        
        // Update bar color based on current position
        if (progressBarFill != null)
        {
            float normalizedPoints = currentPoints / 100f;
            
            if (normalizedPoints >= greenZoneStart)
            {
                progressBarFill.color = greenZoneColor;
            }
            else if (normalizedPoints >= yellowZoneStart)
            {
                progressBarFill.color = yellowZoneColor;
            }
            else
            {
                progressBarFill.color = redZoneColor;
                
                // Play warning sound when entering red zone
                if (normalizedPoints < yellowZoneStart && normalizedPoints + (pointsPerClick / 100f) >= yellowZoneStart)
                {
                    PlaySfx(warningSfx);
                }
            }
        }
    }
    
    private void UpdateTimer()
    {
        if (timerText != null)
        {
            timerText.text = $"Time: {timeRemaining:F1}s";
            
            // Change color as time runs out
            if (timeRemaining < 5f)
            {
                timerText.color = Color.red;
            }
            else if (timeRemaining < timeRemaining * 0.5f)
            {
                timerText.color = Color.yellow;
            }
        }
    }
    
    private void UpdateClickCountDisplay()
    {
        if (clickCountText != null)
        {
            clickCountText.text = $"Clicks/sec: {clicksPerSecond:F1}\nPoints: {currentPoints:F0}/{targetPoints:F0}";
        }
    }
    
    private void CreateUI()
    {
        Debug.Log("PropellerRepairMiniGame: CreateUI() called");
        
        // Always create a new canvas for the minigame to ensure it's on top
        if (minigameCanvas == null)
        {
            Debug.Log("PropellerRepairMiniGame: Creating new canvas...");
            GameObject canvasObj = new GameObject("PropellerRepairMiniGame_Canvas");
            DontDestroyOnLoad(canvasObj); // Prevent destruction during scene changes
            minigameCanvas = canvasObj.AddComponent<Canvas>();
            minigameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            minigameCanvas.sortingOrder = 999; // Very high sorting order to ensure it's on top
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Ensure canvas GameObject is active
            canvasObj.SetActive(true);
            Debug.Log($"PropellerRepairMiniGame: Created new canvas '{canvasObj.name}', Active: {canvasObj.activeInHierarchy}, Enabled: {minigameCanvas.enabled}, SortingOrder: {minigameCanvas.sortingOrder}");
        }
        else
        {
            // Canvas already exists, just ensure it's active
            minigameCanvas.gameObject.SetActive(true);
            minigameCanvas.enabled = true;
            minigameCanvas.sortingOrder = 999; // Ensure it stays on top
            Debug.Log($"PropellerRepairMiniGame: Using existing canvas '{minigameCanvas.name}', Active: {minigameCanvas.gameObject.activeInHierarchy}, Enabled: {minigameCanvas.enabled}, SortingOrder: {minigameCanvas.sortingOrder}");
        }
        
        // Clean up any existing panel first and reset references
        Transform existingPanel = minigameCanvas.transform.Find("MinigamePanel");
        if (existingPanel != null)
        {
            Debug.Log("PropellerRepairMiniGame: Cleaning up existing panel before creating new one.");
            Destroy(existingPanel.gameObject);
        }
        
        // Reset UI references to ensure they're recreated
        timerText = null;
        instructionsText = null;
        progressBar = null;
        progressBarFill = null;
        clickCountText = null;
        
        // Create main panel
        GameObject panelObj = new GameObject("MinigamePanel");
        panelObj.transform.SetParent(minigameCanvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 400);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Ensure panel is active
        panelObj.SetActive(true);
        Debug.Log($"PropellerRepairMiniGame: Created panel '{panelObj.name}', Active: {panelObj.activeInHierarchy}, Position: {panelRect.anchoredPosition}");
        
        // Create timer text
        if (timerText == null)
        {
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(panelObj.transform, false);
            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 0.9f);
            timerRect.anchorMax = new Vector2(0.5f, 0.9f);
            timerRect.sizeDelta = new Vector2(400, 60);
            timerRect.anchoredPosition = Vector2.zero;
            timerText.text = $"Time: {timeRemaining:F1}s";
            timerText.fontSize = 36;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = Color.white;
        }
        
        // Create instructions text
        if (instructionsText == null)
        {
            GameObject instructionsObj = new GameObject("InstructionsText");
            instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
            instructionsObj.transform.SetParent(panelObj.transform, false);
            RectTransform instructionsRect = instructionsObj.GetComponent<RectTransform>();
            instructionsRect.anchorMin = new Vector2(0.5f, 0.8f);
            instructionsRect.anchorMax = new Vector2(0.5f, 0.8f);
            instructionsRect.sizeDelta = new Vector2(550, 40);
            instructionsRect.anchoredPosition = Vector2.zero;
            instructionsText.text = "SPAM LEFT CLICK to raise the bar! Keep it in the GREEN ZONE!";
            instructionsText.fontSize = 20;
            instructionsText.alignment = TextAlignmentOptions.Center;
            instructionsText.color = Color.white;
        }
        
        // Create progress bar
        if (progressBar == null)
        {
            GameObject sliderObj = new GameObject("ProgressBar");
            sliderObj.transform.SetParent(panelObj.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(500, 60);
            sliderRect.anchoredPosition = Vector2.zero;
            
            progressBar = sliderObj.AddComponent<Slider>();
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            
            // Create background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Create fill area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            progressBarFill = fillObj.AddComponent<Image>();
            progressBarFill.color = redZoneColor;
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            progressBar.fillRect = fillRect;
        }
        
        // Create click count text
        if (clickCountText == null)
        {
            GameObject clickCountObj = new GameObject("ClickCountText");
            clickCountObj.transform.SetParent(panelObj.transform, false);
            clickCountText = clickCountObj.AddComponent<TextMeshProUGUI>();
            RectTransform clickCountRect = clickCountObj.GetComponent<RectTransform>();
            clickCountRect.anchorMin = new Vector2(0.5f, 0.3f);
            clickCountRect.anchorMax = new Vector2(0.5f, 0.3f);
            clickCountRect.sizeDelta = new Vector2(400, 60);
            clickCountRect.anchoredPosition = Vector2.zero;
            clickCountText.text = "Clicks/sec: 0.0\nPoints: 0/80";
            clickCountText.fontSize = 24;
            clickCountText.alignment = TextAlignmentOptions.Center;
            clickCountText.color = Color.white;
            clickCountObj.SetActive(true);
        }
        
        Debug.Log($"PropellerRepairMiniGame: UI creation complete. Panel children count: {panelObj.transform.childCount}");
        Debug.Log($"PropellerRepairMiniGame: Panel active: {panelObj.activeInHierarchy}, Canvas active: {minigameCanvas.gameObject.activeInHierarchy}");
    }
    
    private void FailMinigame()
    {
        hasFailed = true;
        isActive = false;
        
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
            cursorUnlockCoroutine = null;
        }
        
        PlaySfx(failSfx);
        Debug.Log("PropellerRepairMiniGame: Failed! Bar dropped too low.");
        
        // Restore player input and cursor
        RestorePlayerInput();
        
        // Clean up UI
        CleanupUI();
        
        // Call completion callback with failure
        if (onComplete != null)
        {
            onComplete(currentPart, false);
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
            Debug.Log("PropellerRepairMiniGame: Success! Propeller repaired.");
        }
        else
        {
            PlaySfx(failSfx);
            Debug.Log("PropellerRepairMiniGame: Failed! Time ran out.");
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
            Debug.Log("PropellerRepairMiniGame: Temporarily disabled InventoryUIManager to prevent cursor locking.");
        }
    }
    
    private void RestorePlayerInput()
    {
        if (inventoryUIManager != null && wasInventoryUIManagerEnabled)
        {
            inventoryUIManager.enabled = true;
            Debug.Log("PropellerRepairMiniGame: Re-enabled InventoryUIManager.");
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
