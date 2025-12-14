using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class EngineRepairMiniGame : MonoBehaviour
{
    [Header("UI Settings")]
    public Canvas minigameCanvas;           // Optional: If null, created automatically
    public RectTransform knotContainer;     // Container for knot UI elements
    public GameObject knotPrefab;           // Prefab for a single knot UI element
    public TextMeshProUGUI timerText;       // Timer display
    public TextMeshProUGUI instructionsText; // Instructions display
    public TextMeshProUGUI progressText;    // Progress display (e.g., "3/5 knots")
    
    [Header("Gameplay Settings")]
    [Tooltip("How many knot pairs (each pair is 2 knots of same color)")]
    public int numberOfKnotPairs = 3;
    
    [Tooltip("Available colors for knot pairs")]
    public Color[] availableColors = new Color[]
    {
        Color.blue,
        Color.green,
        Color.red,
        Color.yellow,
        Color.cyan,
        Color.magenta
    };
    
    [Header("Visual Settings")]
    public Color knotNormalColor = Color.white;
    public Color knotSelectedColor = Color.yellow;
    public Color knotMatchedColor = Color.green;
    public Color knotErrorColor = Color.red;
    
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip knotUntieSfx;
    public AudioClip knotCompleteSfx;
    public AudioClip successSfx;
    public AudioClip failSfx;
    public AudioClip timerTickSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;
    
    private ShipRepairPart currentPart;
    private System.Action<ShipRepairPart, bool> onComplete;
    private bool isActive = false;
    private float timeRemaining;
    private int pairsMatched = 0;
    private int totalPairs;
    private List<KnotUI> knots = new List<KnotUI>();
    private KnotUI firstSelectedKnot = null;  // First knot clicked (waiting for match)
    private bool isPaused = false;
    
    // Player input and cursor management
    private CharController_Motor playerMotor;
    private HotbarManager hotbarManager;
    private InventoryUIManager inventoryUIManager;
    private bool wasInputActive = false;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;
    private Coroutine cursorUnlockCoroutine;
    private bool wasInventoryUIManagerEnabled = false;
    
    // Knot UI data structure
    private class KnotUI
    {
        public GameObject gameObject;
        public Image knotImage;
        public Button knotButton;
        public RectTransform rectTransform;
        public Color knotColor;           // The color assigned to this knot
        public int pairId;                // ID of the pair (two knots with same pairId must be matched)
        public bool isSelected = false;   // Currently selected (first click)
        public bool isMatched = false;    // Successfully matched with its pair
    }
    
    void Start()
    {
        // Ensure AudioSource
        if (sfxSource == null)
        {
            GameObject audioObj = new GameObject("EngineRepairMiniGame_Audio");
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
        // This prevents InventoryUIManager's LateUpdate from locking it
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
    }
    
    void LateUpdate()
    {
        // Ensure cursor stays unlocked even after InventoryUIManager's LateUpdate
        // This runs after most other scripts' LateUpdate, including InventoryUIManager
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
    }
    
    void OnGUI()
    {
        // OnGUI runs very late in the frame, after all Updates and LateUpdates
        // This ensures we override any cursor locking that happens in LateUpdate
        if (isActive && !isPaused)
        {
            // OnGUI can be called multiple times per frame, so this is very effective
            UnlockCursorForMinigame();
        }
    }
    
    void FixedUpdate()
    {
        // Also unlock in FixedUpdate to catch any missed frames
        if (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
        }
    }
    
    public void Begin(ShipRepairPart part, System.Action<ShipRepairPart, bool> completeCallback)
    {
        if (isActive)
        {
            Debug.LogWarning("EngineRepairMiniGame: Already active! Cannot begin new minigame.");
            return;
        }
        
        currentPart = part;
        onComplete = completeCallback;
        
        // Calculate number of pairs
        // Priority: use numberOfKnotPairs from part, then component's numberOfKnotPairs, then fallback
        if (part.numberOfKnotPairs > 0)
        {
            totalPairs = part.numberOfKnotPairs;
        }
        else if (numberOfKnotPairs > 0)
        {
            totalPairs = numberOfKnotPairs;
        }
        else
        {
            // Fallback: use numberOfKnots / 2 if numberOfKnotPairs not set
            totalPairs = Mathf.Max(1, part.numberOfKnots / 2);
        }
        
        // Set time limit - use part's timeLimit if > 0, otherwise use component default (60 seconds)
        if (part.timeLimit > 0)
        {
            timeRemaining = part.timeLimit;
        }
        else
        {
            // Default to 60 seconds if not set
            timeRemaining = 60f;
            Debug.LogWarning($"EngineRepairMiniGame: Part '{part.partName}' has timeLimit = 0. Using default 60 seconds.");
        }
        
        pairsMatched = 0;
        firstSelectedKnot = null;
        isActive = true;
        isPaused = false;
        
        // Save current cursor state
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        
        // Disable player movement and input
        DisablePlayerInput();
        
        // Force unlock cursor immediately (do this before anything else)
        // Try both None and Confined to see which works
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Wait a frame to ensure it sticks
        StartCoroutine(DelayedCursorUnlock());
        
        // Start continuous cursor unlock coroutine
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
        }
        cursorUnlockCoroutine = StartCoroutine(ContinuousCursorUnlock());
        
        // Create UI
        CreateUI();
        
        // Start the minigame
        StartCoroutine(MinigameLoop());
        
        Debug.Log($"EngineRepairMiniGame: Started with {totalPairs} pairs ({totalPairs * 2} total boxes), {timeRemaining} seconds.");
    }
    
    private IEnumerator DelayedCursorUnlock()
    {
        yield return null; // Wait one frame
        UnlockCursorForMinigame();
        yield return new WaitForEndOfFrame(); // Wait until end of frame
        UnlockCursorForMinigame();
    }
    
    private IEnumerator ContinuousCursorUnlock()
    {
        while (isActive && !isPaused)
        {
            UnlockCursorForMinigame();
            // Unlock multiple times per frame to ensure it sticks
            yield return null; // Wait one frame
            UnlockCursorForMinigame();
            yield return new WaitForEndOfFrame(); // Wait until end of frame
            UnlockCursorForMinigame();
        }
    }
    
    private void UnlockCursorForMinigame()
    {
        // Force unlock cursor (InventoryUIManager will try to lock it, so we do this every frame)
        // Try None first, then Confined as fallback
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Sometimes Unity needs this set multiple times
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // If it's still locked, try confined mode (allows cursor to move but keeps it in window)
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        
        // Final attempt - force to None
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void DisablePlayerInput()
    {
        // Disable player movement
        if (playerMotor != null)
        {
            // Assume input was active (most of the time it is)
            wasInputActive = true;
            playerMotor.SetInputActive(false);
        }
        else
        {
            wasInputActive = false;
        }
        
        // Disable hotbar input
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(false);
        }
        
        // Temporarily disable InventoryUIManager to prevent it from locking cursor
        if (inventoryUIManager != null)
        {
            wasInventoryUIManagerEnabled = inventoryUIManager.enabled;
            inventoryUIManager.enabled = false;
            Debug.Log("EngineRepairMiniGame: Temporarily disabled InventoryUIManager to prevent cursor locking.");
        }
    }
    
    private void RestorePlayerInput()
    {
        // Restore InventoryUIManager first (before restoring cursor)
        if (inventoryUIManager != null && wasInventoryUIManagerEnabled)
        {
            inventoryUIManager.enabled = true;
            Debug.Log("EngineRepairMiniGame: Re-enabled InventoryUIManager.");
        }
        
        // Restore player movement
        if (playerMotor != null && wasInputActive)
        {
            playerMotor.SetInputActive(true);
        }
        
        // Restore hotbar input
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(true);
        }
        
        // Restore cursor state (this will get locked by InventoryUIManager, which is fine)
        // Don't restore immediately - let InventoryUIManager handle it
        // Cursor.lockState = previousCursorLockState;
        // Cursor.visible = previousCursorVisible;
    }
    
    private void CreateUI()
    {
        // Find or create canvas
        if (minigameCanvas == null)
        {
            // Look for existing Canvas in scene
            Canvas existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                minigameCanvas = existingCanvas;
            }
            else
            {
                // Create new canvas
                GameObject canvasObj = new GameObject("EngineRepairMiniGame_Canvas");
                minigameCanvas = canvasObj.AddComponent<Canvas>();
                minigameCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }
        
        // Create main panel
        GameObject panelObj = new GameObject("MinigamePanel");
        panelObj.transform.SetParent(minigameCanvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(800, 600);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Create timer text
        if (timerText == null)
        {
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(panelObj.transform, false);
            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 0.9f);
            timerRect.anchorMax = new Vector2(0.5f, 0.9f);
            timerRect.sizeDelta = new Vector2(400, 100);
            timerRect.anchoredPosition = Vector2.zero;
            timerText.text = $"Time: {timeRemaining:F1}s";
            timerText.fontSize = 48;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = Color.white;
        }
        
        // Create instructions text
        if (instructionsText == null)
        {
            GameObject instructionsObj = new GameObject("InstructionsText");
            instructionsObj.transform.SetParent(panelObj.transform, false);
            instructionsText = instructionsObj.AddComponent<TextMeshProUGUI>();
            RectTransform instructionsRect = instructionsObj.GetComponent<RectTransform>();
            instructionsRect.anchorMin = new Vector2(0.5f, 0.8f);
            instructionsRect.anchorMax = new Vector2(0.5f, 0.8f);
            instructionsRect.sizeDelta = new Vector2(700, 60);
            instructionsRect.anchoredPosition = Vector2.zero;
            instructionsText.text = "Match the colored boxes in pairs! Click two boxes of the same color to match them.";
            instructionsText.fontSize = 24;
            instructionsText.alignment = TextAlignmentOptions.Center;
            instructionsText.color = Color.white;
        }
        
        // Create progress text
        if (progressText == null)
        {
            GameObject progressObj = new GameObject("ProgressText");
            progressObj.transform.SetParent(panelObj.transform, false);
            progressText = progressObj.AddComponent<TextMeshProUGUI>();
            RectTransform progressRect = progressObj.GetComponent<RectTransform>();
            progressRect.anchorMin = new Vector2(0.5f, 0.7f);
            progressRect.anchorMax = new Vector2(0.5f, 0.7f);
            progressRect.sizeDelta = new Vector2(400, 60);
            progressRect.anchoredPosition = Vector2.zero;
            progressText.text = $"Pairs Matched: {pairsMatched}/{totalPairs}";
            progressText.fontSize = 32;
            progressText.alignment = TextAlignmentOptions.Center;
            progressText.color = Color.yellow;
        }
        
        // Create knot container
        if (knotContainer == null)
        {
            GameObject containerObj = new GameObject("KnotContainer");
            containerObj.transform.SetParent(panelObj.transform, false);
            knotContainer = containerObj.AddComponent<RectTransform>();
            knotContainer.anchorMin = new Vector2(0.5f, 0.5f);
            knotContainer.anchorMax = new Vector2(0.5f, 0.5f);
            knotContainer.sizeDelta = new Vector2(700, 400);
            knotContainer.anchoredPosition = Vector2.zero;
        }
        
        // Create knots (colored boxes)
        CreateKnots();
    }
    
    private void CreateKnots()
    {
        knots.Clear();
        firstSelectedKnot = null;
        
        // Create pairs: each pair has 2 knots with the same color
        int totalKnots = totalPairs * 2;
        
        // Prepare color list - each pair gets a unique color
        List<Color> colorsToUse = new List<Color>();
        for (int i = 0; i < totalPairs; i++)
        {
            Color colorToUse = availableColors[i % availableColors.Length];
            colorsToUse.Add(colorToUse);
            colorsToUse.Add(colorToUse); // Add twice for the pair
        }
        
        // Shuffle the colors so pairs are not adjacent
        for (int i = 0; i < colorsToUse.Count; i++)
        {
            Color temp = colorsToUse[i];
            int randomIndex = Random.Range(i, colorsToUse.Count);
            colorsToUse[i] = colorsToUse[randomIndex];
            colorsToUse[randomIndex] = temp;
        }
        
        // Calculate layout (grid of knots)
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalKnots));
        int rows = Mathf.CeilToInt((float)totalKnots / columns);
        float spacing = 120f;
        float startX = -(columns - 1) * spacing * 0.5f;
        float startY = (rows - 1) * spacing * 0.5f;
        
        // Track pair IDs for matching
        Dictionary<Color, int> colorToPairId = new Dictionary<Color, int>();
        int currentPairId = 0;
        
        for (int i = 0; i < totalKnots; i++)
        {
            int col = i % columns;
            int row = i / columns;
            
            GameObject knotObj;
            if (knotPrefab != null)
            {
                knotObj = Instantiate(knotPrefab, knotContainer);
            }
            else
            {
                // Create simple colored box UI element
                knotObj = new GameObject($"Knot_{i + 1}");
                knotObj.transform.SetParent(knotContainer, false);
                
                Image knotImg = knotObj.AddComponent<Image>();
                knotImg.color = knotNormalColor; // Will be set to actual color below
                
                // Create button
                Button button = knotObj.AddComponent<Button>();
            }
            
            RectTransform knotRect = knotObj.GetComponent<RectTransform>();
            knotRect.sizeDelta = new Vector2(100, 100);
            knotRect.anchoredPosition = new Vector2(startX + col * spacing, startY - row * spacing);
            
            // Get color for this knot
            Color knotColor = colorsToUse[i];
            int pairId;
            
            // Assign pair ID based on color (same color = same pair ID)
            if (!colorToPairId.ContainsKey(knotColor))
            {
                colorToPairId[knotColor] = currentPairId;
                pairId = currentPairId;
                currentPairId++;
            }
            else
            {
                pairId = colorToPairId[knotColor];
            }
            
            KnotUI knot = new KnotUI
            {
                gameObject = knotObj,
                knotImage = knotObj.GetComponent<Image>(),
                knotButton = knotObj.GetComponent<Button>(),
                rectTransform = knotRect,
                knotColor = knotColor,
                pairId = pairId,
                isSelected = false,
                isMatched = false
            };
            
            // Ensure button exists and set up click handler
            if (knot.knotButton == null)
            {
                knot.knotButton = knotObj.AddComponent<Button>();
            }
            
            int knotIndex = knots.Count; // Capture index for closure
            knot.knotButton.onClick.RemoveAllListeners();
            knot.knotButton.onClick.AddListener(() => OnKnotClicked(knotIndex));
            
            // Set initial visual (start with normal color, reveal color when clicked or matched)
            UpdateKnotVisual(knot);
            
            knots.Add(knot);
        }
        
        Debug.Log($"EngineRepairMiniGame: Created {totalKnots} knots in {totalPairs} color pairs.");
    }
    
    private void UpdateKnotVisual(KnotUI knot)
    {
        if (knot.knotImage == null) return;
        
        if (knot.isMatched)
        {
            // Matched: show green overlay or make it brighter
            knot.knotImage.color = knotMatchedColor;
            knot.knotButton.interactable = false;
        }
        else if (knot.isSelected)
        {
            // Selected: show the knot's color with a yellow/bright highlight
            Color highlightedColor = knot.knotColor;
            highlightedColor.r = Mathf.Min(1f, highlightedColor.r + 0.3f);
            highlightedColor.g = Mathf.Min(1f, highlightedColor.g + 0.3f);
            highlightedColor.b = Mathf.Min(1f, highlightedColor.b + 0.3f);
            knot.knotImage.color = highlightedColor;
        }
        else
        {
            // Normal: show the knot's color (always visible so player can match)
            knot.knotImage.color = knot.knotColor;
        }
    }
    
    private void OnKnotClicked(int knotIndex)
    {
        if (!isActive || isPaused || knotIndex < 0 || knotIndex >= knots.Count)
            return;
        
        KnotUI knot = knots[knotIndex];
        
        // Don't allow clicking already matched knots
        if (knot.isMatched)
            return;
        
        // If this is the first knot clicked, select it
        if (firstSelectedKnot == null)
        {
            firstSelectedKnot = knot;
            knot.isSelected = true;
            PlaySfx(knotUntieSfx);
            UpdateKnotVisual(knot);
            Debug.Log($"EngineRepairMiniGame: Selected first knot (Color: {knot.knotColor}, Pair ID: {knot.pairId})");
        }
        else
        {
            // Second knot clicked - check if it matches the first
            if (firstSelectedKnot == knot)
            {
                // Clicked the same knot - deselect it
                firstSelectedKnot.isSelected = false;
                firstSelectedKnot = null;
                UpdateKnotVisual(knot);
                Debug.Log("EngineRepairMiniGame: Deselected knot (clicked same knot again).");
            }
            else if (firstSelectedKnot.pairId == knot.pairId)
            {
                // Match! Both knots have the same pair ID (same color)
                firstSelectedKnot.isMatched = true;
                knot.isMatched = true;
                pairsMatched++;
                
                PlaySfx(knotCompleteSfx);
                UpdateKnotVisual(firstSelectedKnot);
                UpdateKnotVisual(knot);
                UpdateProgress();
                
                firstSelectedKnot = null;
                Debug.Log($"EngineRepairMiniGame: Matched pair! ({pairsMatched}/{totalPairs} pairs matched)");
            }
            else
            {
                // Wrong match - show error briefly, then deselect first
                PlaySfx(failSfx);
                
                // Store reference before clearing
                KnotUI firstKnot = firstSelectedKnot;
                int expectedPairId = firstKnot.pairId;
                
                knot.knotImage.color = knotErrorColor;
                firstKnot.isSelected = false;
                
                // Reset after a brief delay
                StartCoroutine(ResetErrorKnot(knot, firstKnot));
                firstSelectedKnot = null;
                Debug.Log($"EngineRepairMiniGame: Wrong match! Expected pair {expectedPairId}, got {knot.pairId}");
            }
        }
    }
    
    private IEnumerator ResetErrorKnot(KnotUI errorKnot, KnotUI firstKnot)
    {
        yield return new WaitForSeconds(0.5f);
        UpdateKnotVisual(errorKnot);
        if (firstKnot != null)
        {
            UpdateKnotVisual(firstKnot);
        }
    }
    
    private void UpdateProgress()
    {
        if (progressText != null)
        {
            progressText.text = $"Pairs Matched: {pairsMatched}/{totalPairs}";
        }
        
        // Check if all pairs are matched
        if (pairsMatched >= totalPairs)
        {
            // All pairs matched - engine repair complete!
            CompleteMinigame(true);
        }
    }
    
    private IEnumerator MinigameLoop()
    {
        float lastTickTime = 0f;
        
        while (isActive && timeRemaining > 0f)
        {
            if (!isPaused)
            {
                timeRemaining -= Time.deltaTime;
                
                // Update timer display
                if (timerText != null)
                {
                    timerText.text = $"Time: {timeRemaining:F1}s";
                    
                    // Change color as time runs out
                    if (timeRemaining < 10f)
                    {
                        timerText.color = Color.red;
                        if (timeRemaining - lastTickTime <= -1f)
                        {
                            PlaySfx(timerTickSfx);
                            lastTickTime = timeRemaining;
                        }
                    }
                    else if (timeRemaining < timeRemaining * 0.5f)
                    {
                        timerText.color = Color.yellow;
                    }
                }
                
                // Check win condition (all pairs matched)
                if (pairsMatched >= totalPairs)
                {
                    CompleteMinigame(true);
                    yield break;
                }
            }
            
            yield return null;
        }
        
        // Time ran out
        if (timeRemaining <= 0f && pairsMatched < totalPairs)
        {
            CompleteMinigame(false);
        }
    }
    
    private void CompleteMinigame(bool success)
    {
        isActive = false;
        
        // Stop cursor unlock coroutine
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
            cursorUnlockCoroutine = null;
        }
        
        if (success)
        {
            PlaySfx(successSfx);
            Debug.Log("EngineRepairMiniGame: Success! Engine repaired.");
        }
        else
        {
            PlaySfx(failSfx);
            Debug.Log("EngineRepairMiniGame: Failed! Ran out of time.");
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
            // Destroy only our UI elements, not the entire canvas (it might be shared)
            Transform panel = minigameCanvas.transform.Find("MinigamePanel");
            if (panel != null)
            {
                Destroy(panel.gameObject);
            }
        }
        
        knots.Clear();
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

