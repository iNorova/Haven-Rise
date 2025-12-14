using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Duration of day in real-world minutes before automatically transitioning to night.")]
    public float dayDurationInMinutes = 30f;
    [Tooltip("Duration of transition to night in seconds (fade duration).")]
    public float transitionDuration = 5f;
    [Tooltip("Start in day mode (true) or night mode (false).")]
    public bool startInDay = true;

    [Header("Light References")]
    [Tooltip("The main sun/directional light (enabled during day, disabled during night).")]
    public Light sunLight;
    [Tooltip("The night light (enabled during night, disabled during day).")]
    public Light nightLight;

    [Header("Skybox Settings")]
    [Tooltip("Skybox material to use during day. Leave empty to keep current skybox.")]
    public Material daySkybox;
    [Tooltip("Skybox material to use during night. Leave empty to keep current skybox.")]
    public Material nightSkybox;

    [Header("Fade Settings")]
    [Tooltip("UI Image for fade effect during transition. Will be created automatically if not assigned.")]
    public Image fadeOverlay;
    [Tooltip("Fade to black before transitioning (true) or fade during transition (false).")]
    public bool fadeToBlackFirst = true;
    [Tooltip("Duration in seconds for fading to black (fade in).")]
    public float fadeInDuration = 0.5f;
    [Tooltip("Duration in seconds for fading from black (fade out).")]
    public float fadeOutDuration = 2f;

    [Header("Timer UI")]
    [Tooltip("Show timer on screen (top right).")]
    public bool showTimer = true;
    [Tooltip("Timer text component. Will be created automatically if not assigned.")]
    public Text timerText;
    [Tooltip("Font size for timer text.")]
    public int timerFontSize = 24;
    [Tooltip("Color of timer text (normal).")]
    public Color timerTextColor = Color.white;
    [Tooltip("Color of timer text when near night time (warning).")]
    public Color timerWarningColor = Color.red;
    [Tooltip("Minutes remaining before timer turns red (warning threshold).")]
    public float warningThresholdMinutes = 5f;

    [Header("Audio")]
    [Tooltip("Audio clip that plays when transitioning to night (zombie screech). Drag your MP3 audio file here.")]
    public AudioClip nightTransitionScreechClip;
    private AudioSource audioSource; // Internal AudioSource component for playing clips

    [Header("Night Warning Pop-up")]
    [Tooltip("Warning panel that appears when night starts. Will be created automatically if not assigned.")]
    public GameObject nightWarningPanel;
    [Tooltip("Text component for night warning message. Will be created automatically if not assigned.")]
    public TextMeshProUGUI nightWarningText;
    [Tooltip("Warning message to display when night starts.")]
    [TextArea(5, 10)]
    public string nightWarningMessage = "IT'S NIGHT TIME!\n\nThe Ghouls are faster now.\n\nBuild a CAMPFIRE to stay warm.\n\nBuild a BED to sleep through the night.\n\nPress F to continue...";
    
    // Player control references
    private CharController_Motor playerMotor;
    private HotbarManager hotbarManager;
    private bool wasPlayerInputActive = true;
    private bool wasHotbarInputActive = true;
    
    // Night warning pop-up state
    private static bool hasShownNightWarning = false; // Static to persist across scene loads but reset on new game
    private bool isShowingNightWarning = false;

    // Static instance for global access
    public static DayNightCycle Instance { get; private set; }

    // Timer state
    private float _dayTimer = 0f; // Timer counting up during day
    private bool _isDayTime = true; // Current state (day or night)
    private bool _isTransitioning = false; // Flag for transitioning to night
    private float _transitionStartTime = 0f;
    private float _fadeProgress = 0f;
    private bool _isFadingToBlack = false; // Flag for fade-to-black phase
    private bool _isFadingFromBlack = false; // Flag for fade-from-black phase
    private bool _justLoaded = false; // Flag to prevent immediate transitions after loading
    private float _loadTime = 0f; // Time when we loaded (to prevent transitions for a few seconds)
    private bool _disableAutoTransitions = false; // Flag to disable automatic transitions (when loading saved game)

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Subscribe to scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if we're in a menu scene and disable script accordingly
        string sceneName = scene.name;
        if (sceneName == "MAIN MENU FINAL" || sceneName == "Starting Cutscene")
        {
            this.enabled = false;
            // Hide all UI elements
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(false);
            }
            if (nightWarningPanel != null)
            {
                nightWarningPanel.SetActive(false);
            }
            Debug.Log($"[DayNightCycle] Script disabled on scene load: {sceneName}");
        }
        else
        {
            // Re-enable script in game scenes
            if (!this.enabled)
            {
                this.enabled = true;
                Debug.Log($"[DayNightCycle] Script re-enabled on scene load: {sceneName}");
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from scene loaded events
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Check if we are in a menu scene immediately
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "MAIN MENU FINAL" || currentSceneName == "Starting Cutscene")
        {
            // Disable the script completely in menu scenes
            this.enabled = false;
            // Hide timer UI immediately if it exists
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
            // Hide all UI elements
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(false);
            }
            if (nightWarningPanel != null)
            {
                nightWarningPanel.SetActive(false);
            }
            Debug.Log($"[DayNightCycle] Script disabled in menu scene: {currentSceneName}");
            return;
        }
        
        // Re-enable script if we're in a game scene (in case it was disabled in menu)
        if (!this.enabled)
        {
            this.enabled = true;
            Debug.Log($"[DayNightCycle] Script re-enabled in game scene: {currentSceneName}");
        }

        // Get or create AudioSource component for playing audio clips
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Make audio source ignore listener pause so it plays even when game is paused
        audioSource.ignoreListenerPause = true;

        // Check if there's saved day/night cycle data to load
        if (PlayerPrefs.HasKey(PauseMenuManager.SavedDayTimerKey))
        {
            Debug.Log("[DayNightCycle] Found saved game data. Loading saved state...");
            
            // COMPLETELY STOP AND RESET ALL TRANSITIONS - NO TRANSITIONS ALLOWED WHEN LOADING
            _isTransitioning = false;
            _isFadingToBlack = false;
            _isFadingFromBlack = false;
            _fadeProgress = 0f;
            _transitionStartTime = 0f;
            
            // Hide and reset fade overlay
            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
                fadeOverlay.gameObject.SetActive(false);
            }
            
            // Load saved state
            float savedDayTimer = PlayerPrefs.GetFloat(PauseMenuManager.SavedDayTimerKey, 0f);
            bool savedIsDayTime = PlayerPrefs.GetInt(PauseMenuManager.SavedIsDayTimeKey, 1) == 1;
            bool savedNightWarningShown = PlayerPrefs.GetInt(PauseMenuManager.SavedNightWarningShownKey, 0) == 1;
            
            _dayTimer = savedDayTimer;
            _isDayTime = savedIsDayTime;
            
            // Restore night warning state
            if (savedNightWarningShown)
            {
                hasShownNightWarning = true;
            }
            
            // Ensure lights and skybox match the saved state
            SetLightStates(_isDayTime);
            SetSkybox(_isDayTime);
            
            // Set flag to prevent immediate transitions after loading
            _justLoaded = true;
            _loadTime = Time.time;
            _disableAutoTransitions = true; // Disable auto transitions on load - NO TRANSITIONS ALLOWED

            // If timer is at or past the day duration limit, set it to 1 minute before the limit
            // This prevents an immediate transition to night right after loading a saved game that was at the end of day.
            float dayTimerMinutes = savedDayTimer / 60f;
            if (savedIsDayTime && dayTimerMinutes >= dayDurationInMinutes)
            {
                _dayTimer = Mathf.Max(0f, (dayDurationInMinutes - 1f) * 60f);
                Debug.Log($"[DayNightCycle] Timer was at or past limit ({dayTimerMinutes:F2} min), adjusted to {_dayTimer / 60f:F2} min to prevent immediate transition.");
            }
            
            Debug.Log($"[DayNightCycle] Loaded saved state successfully: DayTimer={_dayTimer / 60f:F2} min, IsDayTime={_isDayTime}, LoadTime={_loadTime}, JustLoaded={_justLoaded}, DisableAutoTransitions={_disableAutoTransitions} - ALL TRANSITIONS BLOCKED");
        }
        else
        {
            // Initialize timer mode (new game)
            _isDayTime = startInDay;
            _dayTimer = 0f;
            _isTransitioning = false;
            _isFadingToBlack = false;
            _isFadingFromBlack = false;
            _fadeProgress = 0f;
            _justLoaded = false;
            _disableAutoTransitions = false; // Enable auto transitions for new game
        }

        // Find lights if not assigned
        if (sunLight == null)
        {
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj != null)
            {
                sunLight = sunObj.GetComponent<Light>();
            }
            else
            {
                // Try to find any directional light
                Light[] lights = FindObjectsOfType<Light>(true);
                foreach (Light light in lights)
                {
                    if (light != null && light.type == LightType.Directional && light.name != "NIGHT")
                    {
                        sunLight = light;
                        break;
                    }
                }
            }
        }

        if (nightLight == null)
        {
            GameObject nightObj = GameObject.Find("NIGHT");
            if (nightObj != null)
            {
                nightLight = nightObj.GetComponent<Light>();
                if (nightLight == null)
                {
                    nightLight = nightObj.GetComponentInChildren<Light>();
                }
            }
            else
            {
                // Try to find NIGHT light
                Light[] allLights = FindObjectsOfType<Light>(true);
                foreach (Light light in allLights)
                {
                    if (light != null && light.name == "NIGHT")
                    {
                        nightLight = light;
                        break;
                    }
                }
            }
        }

        // Set initial light states
        SetLightStates(_isDayTime);
        
        // Set initial skybox
        SetSkybox(_isDayTime);

        // Create fade overlay if not assigned
        if (fadeOverlay == null)
        {
            CreateFadeOverlay();
        }

        // Create timer UI if enabled
        if (showTimer && timerText == null)
        {
            CreateTimerUI();
        }

        // Cache player references for disabling controls during transition
        playerMotor = FindFirstObjectByType<CharController_Motor>();
        hotbarManager = FindFirstObjectByType<HotbarManager>();

        Debug.Log($"[DayNightCycle] Started. Initial state: {(_isDayTime ? "DAY" : "NIGHT")}, Day duration: {dayDurationInMinutes} minutes, Scene: {currentSceneName}");
    }

    void Update()
    {
        // Double-check we're not in a menu scene (safety check)
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "MAIN MENU FINAL" || currentSceneName == "Starting Cutscene")
        {
            // Disable script if somehow we're still running in menu
            this.enabled = false;
            return;
        }
        
        // Periodically sync with actual light states (in case bed changed them)
        // But don't sync if auto transitions are disabled (when loading saved game)
        if (Time.frameCount % 60 == 0 && !_disableAutoTransitions) // Check every 60 frames (~1 second at 60fps)
        {
            SyncFromLights();
        }
        
        // Timer should ALWAYS count (even when auto transitions are disabled)
        // Only automatic transitions are blocked, not the timer itself
        float timeSinceLoad = Time.time - _loadTime;
        bool canUpdateTimer = _isDayTime && !_isTransitioning && !_isFadingToBlack && !_isFadingFromBlack;
        
        if (canUpdateTimer)
        {
            // Count up the day timer (this should always work)
            _dayTimer += Time.deltaTime;
            
            // Convert to minutes
            float dayTimerMinutes = _dayTimer / 60f;
            
            // Check if we've reached the day duration
            // Only trigger transition if auto transitions are enabled
            if (dayTimerMinutes >= dayDurationInMinutes && !_disableAutoTransitions)
            {
                // Start transition to night (only if auto transitions are enabled)
                StartTransitionToNight();
            }
        }
        
        // Clear the just loaded flag after 5 seconds (but keep auto transitions disabled)
        if (_justLoaded && timeSinceLoad > 5f)
        {
            _justLoaded = false;
            Debug.Log($"[DayNightCycle] Cleared _justLoaded flag after {timeSinceLoad:F2} seconds. Auto transitions remain disabled.");
        }
        
        // BLOCK ALL FADE TRANSITIONS WHEN LOADING SAVED GAME
        // If auto transitions are disabled (loading saved game), completely stop any fade transitions
        if (_disableAutoTransitions)
        {
            // Force stop any active fade transitions
            if (_isFadingToBlack || _isFadingFromBlack || _isTransitioning)
            {
                _isFadingToBlack = false;
                _isFadingFromBlack = false;
                _isTransitioning = false;
                _fadeProgress = 0f;
                _transitionStartTime = 0f;
                
                // Hide fade overlay
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
                    fadeOverlay.gameObject.SetActive(false);
                }
            }
            // Don't process any fade transitions when loading saved game
            // Skip to timer UI update
        }
        else
        {
            // Only process fade transitions if auto transitions are enabled (normal gameplay)
            // Handle fade to black phase
            if (_isFadingToBlack)
            {
                float elapsed = Time.time - _transitionStartTime;
                _fadeProgress = Mathf.Clamp01(elapsed / fadeInDuration); // Use fadeInDuration
                
                // Update fade overlay
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = new Color(0f, 0f, 0f, _fadeProgress);
                }
                
                // Check if fade to black is complete
                if (_fadeProgress >= 1f)
                {
                    // Switch lights and skybox while black
                    SetLightStates(false);
                    SetSkybox(false);
                    
                    // Start fading from black
                    _isFadingToBlack = false;
                    _isFadingFromBlack = true;
                    _transitionStartTime = Time.time;
                    _fadeProgress = 0f;
                }
            }
            
            // Handle fade from black phase
            if (_isFadingFromBlack)
            {
                float elapsed = Time.time - _transitionStartTime;
                _fadeProgress = Mathf.Clamp01(elapsed / fadeOutDuration); // Use fadeOutDuration
                
                // Update fade overlay (fading out)
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = new Color(0f, 0f, 0f, 1f - _fadeProgress);
                }
                
                // Check if fade from black is complete
                if (_fadeProgress >= 1f)
                {
                    _isFadingFromBlack = false; // Stop the fade phase
                    CompleteTransitionToNight();
                    
                    // Show night warning pop-up immediately if it hasn't been shown before
                    if (!hasShownNightWarning)
                    {
                        ShowNightWarningPanel();
                    }
                }
            }
        }
        
        // Update timer UI (only in game scene)
        if (showTimer && timerText != null)
        {
            // Make sure timer is visible in game scene
            if (!timerText.gameObject.activeSelf)
            {
                timerText.gameObject.SetActive(true);
            }
            UpdateTimerUI();
        }
        
        // Handle night warning pop-up input (when showing)
        if (isShowingNightWarning)
        {
            // Check for F key press to close warning panel
            if (Input.GetKeyDown(KeyCode.F))
            {
                CloseNightWarningPanel();
            }
        }
    }

    void StartTransitionToNight()
    {
        if (_isTransitioning || _isFadingToBlack || _isFadingFromBlack) return; // Already transitioning
        
        // ABSOLUTELY BLOCK transitions if auto transitions are disabled (loading saved game)
        if (_disableAutoTransitions)
        {
            Debug.LogWarning("[DayNightCycle] Transition to night BLOCKED - auto transitions disabled (loading saved game). NO TRANSITIONS ALLOWED.");
            return;
        }
        
        // Also block if we just loaded and are within the grace period
        float timeSinceLoad = Time.time - _loadTime;
        if (_justLoaded && timeSinceLoad < 5f)
        {
            Debug.LogWarning($"[DayNightCycle] Transition to night BLOCKED - game just loaded ({timeSinceLoad:F2}s since load).");
            return;
        }
        
        _isTransitioning = true;
        _transitionStartTime = Time.time;
        _fadeProgress = 0f;
        
        // Play night transition screech
        if (audioSource != null && nightTransitionScreechClip != null)
        {
            audioSource.PlayOneShot(nightTransitionScreechClip);
        }
        
        Debug.Log($"[DayNightCycle] Starting transition to NIGHT. Day timer: {(_dayTimer / 60f):F2} minutes.");
        
        if (fadeToBlackFirst)
        {
            // Start fade to black
            _isFadingToBlack = true;
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            }
        }
        else
        {
            // Direct transition (old method)
            _isFadingToBlack = false;
            _isFadingFromBlack = false;
            if (fadeOverlay != null)
            {
                fadeOverlay.gameObject.SetActive(true);
                fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            }
        }
    }

    void CompleteTransitionToNight()
    {
        _isDayTime = false;
        _isTransitioning = false;
        _isFadingToBlack = false;
        _isFadingFromBlack = false;
        _fadeProgress = 0f;
        
        // Lights and skybox already set during fade-to-black phase
        
        // Hide fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.gameObject.SetActive(false);
        }
        
        float dayTimerMinutes = _dayTimer / 60f;
        Debug.Log($"[DayNightCycle] Transitioned to NIGHT after {dayTimerMinutes:F2} minutes of day time.");
    }

    private void DisablePlayerControls()
    {
        // Disable player movement
        if (playerMotor == null)
        {
            playerMotor = FindFirstObjectByType<CharController_Motor>();
        }
        
        if (playerMotor != null)
        {
            // Check if input is currently active (assume it is if we can't check)
            wasPlayerInputActive = true; // We'll assume it was active
            playerMotor.SetInputActive(false);
        }
        
        // Disable hotbar input
        if (hotbarManager == null)
        {
            hotbarManager = FindFirstObjectByType<HotbarManager>();
        }
        
        if (hotbarManager != null)
        {
            wasHotbarInputActive = true; // We'll assume it was active
            hotbarManager.SetInputActive(false);
        }
        
        Debug.Log("[DayNightCycle] Disabled player controls during night transition.");
    }

    private void EnablePlayerControls()
    {
        // Refresh references in case they're null
        if (playerMotor == null)
        {
            playerMotor = FindFirstObjectByType<CharController_Motor>();
        }
        
        if (hotbarManager == null)
        {
            hotbarManager = FindFirstObjectByType<HotbarManager>();
        }
        
        // Re-enable player movement (always enable if motor exists, regardless of wasPlayerInputActive flag)
        if (playerMotor != null)
        {
            playerMotor.SetInputActive(true);
            Debug.Log("[DayNightCycle] Re-enabled player movement.");
        }
        else
        {
            Debug.LogWarning("[DayNightCycle] PlayerMotor not found! Cannot re-enable player movement.");
        }
        
        // Re-enable hotbar input (always enable if manager exists, regardless of wasHotbarInputActive flag)
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(true);
            Debug.Log("[DayNightCycle] Re-enabled hotbar input.");
        }
        else
        {
            Debug.LogWarning("[DayNightCycle] HotbarManager not found! Cannot re-enable hotbar input.");
        }
        
        Debug.Log("[DayNightCycle] Re-enabled player controls after night transition.");
    }

    private void ShowNightWarningPanel()
    {
        if (isShowingNightWarning) return;
        
        // Check if already shown (but allow showing if flag was reset)
        if (hasShownNightWarning)
        {
            Debug.Log("[DayNightCycle] Night warning already shown - skipping.");
            return;
        }
        
        isShowingNightWarning = true;
        hasShownNightWarning = true; // Mark as shown (static, persists)
        
        // Create warning panel if not assigned
        if (nightWarningPanel == null)
        {
            CreateNightWarningPanel();
        }
        
        // Show panel
        if (nightWarningPanel != null)
        {
            nightWarningPanel.SetActive(true);
            Debug.Log("[DayNightCycle] Night warning panel activated.");
        }
        else
        {
            Debug.LogError("[DayNightCycle] Night warning panel is null after creation attempt!");
        }
        
        // Update text
        if (nightWarningText != null)
        {
            nightWarningText.text = nightWarningMessage;
        }
        
        // Pause game (but audio will continue because we set ignoreListenerPause)
        Time.timeScale = 0f;
        
        Debug.Log("[DayNightCycle] Night warning panel shown. Press F to close.");
    }

    private void CloseNightWarningPanel()
    {
        if (!isShowingNightWarning) return;
        
        isShowingNightWarning = false;
        
        // Hide panel
        if (nightWarningPanel != null)
        {
            nightWarningPanel.SetActive(false);
        }
        
        // Resume game
        Time.timeScale = 1f;
        
        Debug.Log("[DayNightCycle] Night warning panel closed. Game resumed.");
    }

    private void CreateNightWarningPanel()
    {
        // Find or create Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create panel
        GameObject panelObj = new GameObject("NightWarningPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.8f); // Dim black
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        nightWarningPanel = panelObj;
        
        // Create text
        GameObject textObj = new GameObject("NightWarningText");
        textObj.transform.SetParent(panelObj.transform, false);
        
        nightWarningText = textObj.AddComponent<TextMeshProUGUI>();
        nightWarningText.text = nightWarningMessage;
        nightWarningText.fontSize = 28;
        nightWarningText.color = Color.white;
        nightWarningText.alignment = TextAlignmentOptions.Center;
        nightWarningText.verticalAlignment = VerticalAlignmentOptions.Middle;
        nightWarningText.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.1f);
        textRect.anchorMax = new Vector2(0.9f, 0.9f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        // Initially hide panel
        panelObj.SetActive(false);
        
        Debug.Log("[DayNightCycle] Night warning panel created.");
    }

    /// <summary>
    /// Reset the night warning flag (call this when starting a new game)
    /// </summary>
    public static void ResetNightWarning()
    {
        hasShownNightWarning = false;
    }

    /// <summary>
    /// Check if night warning has been shown
    /// </summary>
    public static bool HasShownNightWarning()
    {
        return hasShownNightWarning;
    }

    /// <summary>
    /// Set night warning shown state (for loading saved games)
    /// </summary>
    public static void SetNightWarningShown(bool shown)
    {
        hasShownNightWarning = shown;
    }

    /// <summary>
    /// Get current day timer value (for saving)
    /// </summary>
    public float GetDayTimer()
    {
        return _dayTimer;
    }

    /// <summary>
    /// Set day timer value (for loading)
    /// </summary>
    public void SetDayTimer(float timer)
    {
        _dayTimer = timer;
    }

    /// <summary>
    /// Get current day/night state (for saving)
    /// </summary>
    public bool GetIsDayTime()
    {
        return _isDayTime;
    }

    /// <summary>
    /// Set day/night state (for loading)
    /// </summary>
    public void SetIsDayTime(bool isDay)
    {
        _isDayTime = isDay;
        if (isDay)
        {
            SetLightStates(true);
            SetSkybox(true);
        }
        else
        {
            SetLightStates(false);
            SetSkybox(false);
        }
    }

    /// <summary>
    /// Stop any active transitions and ensure state is stable (call before saving)
    /// </summary>
    public void StopAllTransitions()
    {
        bool wasTransitioning = _isTransitioning || _isFadingToBlack || _isFadingFromBlack;
        
        _isTransitioning = false;
        _isFadingToBlack = false;
        _isFadingFromBlack = false;
        _fadeProgress = 0f;
        _transitionStartTime = 0f;
        
        // Ensure fade overlay is hidden
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.gameObject.SetActive(false);
        }
        
        // Ensure lights and skybox match the current state
        SetLightStates(_isDayTime);
        SetSkybox(_isDayTime);
        
        if (wasTransitioning)
        {
            Debug.Log($"[DayNightCycle] All transitions stopped. Current state: {(_isDayTime ? "DAY" : "NIGHT")}, Timer: {_dayTimer / 60f:F2} min");
        }
    }

    void SetLightStates(bool isDay)
    {
        // Enable/disable sun light
        if (sunLight != null)
        {
            sunLight.gameObject.SetActive(isDay);
        }
        
        // Enable/disable night light
        if (nightLight != null)
        {
            nightLight.gameObject.SetActive(!isDay);
        }
        
        Debug.Log($"[DayNightCycle] Light states set. Day: {isDay}, Sun active: {(sunLight != null ? sunLight.gameObject.activeSelf : false)}, Night active: {(nightLight != null ? nightLight.gameObject.activeSelf : false)}");
    }
    
    /// <summary>
    /// Sync with external light changes (called when bed changes lights)
    /// </summary>
    public void SyncFromLights()
    {
        // Don't sync if we just loaded (let bed/other systems set the state first)
        if (_justLoaded || (Time.time - _loadTime) < 3f)
        {
            return;
        }
        
        // Check actual light states to determine current state
        bool sunActive = sunLight != null && sunLight.gameObject.activeSelf;
        bool nightActive = nightLight != null && nightLight.gameObject.activeSelf;
        
        // Determine if it's day or night based on lights
        bool shouldBeDay = sunActive && !nightActive;
        
        // Only update if state changed AND we're not in a transition
        if (shouldBeDay != _isDayTime && !_isTransitioning && !_isFadingToBlack && !_isFadingFromBlack)
        {
            if (shouldBeDay)
            {
                // Lights indicate day - reset timer
                ResetDayTimer();
            }
            else
            {
                // Lights indicate night - set to night
                SetToNight();
            }
        }
    }

    void SetSkybox(bool isDay)
    {
        Material targetSkybox = isDay ? daySkybox : nightSkybox;
        
        if (targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            Debug.Log($"[DayNightCycle] Skybox set to {(isDay ? "DAY" : "NIGHT")}: {targetSkybox.name}");
        }
    }

    void CreateFadeOverlay()
    {
        // Try to find existing fade overlay
        GameObject existingOverlay = GameObject.Find("FadeOverlay");
        if (existingOverlay != null)
        {
            fadeOverlay = existingOverlay.GetComponent<Image>();
            if (fadeOverlay != null) return;
        }

        // Find or create Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create fade overlay
        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        
        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        
        RectTransform rectTransform = overlayObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        fadeOverlay.raycastTarget = false;
        overlayObj.SetActive(false);
        
        Debug.Log("[DayNightCycle] Created fade overlay automatically.");
    }

    void CreateTimerUI()
    {
        // Find or create Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create timer text
        GameObject timerObj = new GameObject("DayTimerText");
        timerObj.transform.SetParent(canvasObj.transform, false);
        
        timerText = timerObj.AddComponent<Text>();
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = timerFontSize;
        timerText.color = timerTextColor;
        timerText.alignment = TextAnchor.UpperRight;
        timerText.text = "00:00";
        
        RectTransform rectTransform = timerObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.sizeDelta = new Vector2(200f, 50f);
        rectTransform.anchoredPosition = new Vector2(-10f, -10f);
        
        timerText.raycastTarget = false;
        
        Debug.Log("[DayNightCycle] Created timer UI automatically.");
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        
        if (_isDayTime && !_isTransitioning && !_isFadingToBlack && !_isFadingFromBlack)
        {
            float remainingMinutes = GetRemainingDayTimeMinutes();
            int minutes = Mathf.FloorToInt(remainingMinutes);
            int seconds = Mathf.FloorToInt((remainingMinutes - minutes) * 60f);
            
            timerText.text = $"Day Time: {minutes:00}:{seconds:00}";
            
            // Change color to red when near night time
            if (remainingMinutes <= warningThresholdMinutes)
            {
                timerText.color = timerWarningColor;
            }
            else
            {
                timerText.color = timerTextColor;
            }
            
            timerText.gameObject.SetActive(true);
        }
        else if (_isTransitioning || _isFadingToBlack || _isFadingFromBlack)
        {
            timerText.text = "Transitioning...";
            timerText.color = timerWarningColor; // Red during transition
            timerText.gameObject.SetActive(true);
        }
        else
        {
            timerText.text = "Night Time";
            timerText.color = timerWarningColor; // Red during night
            timerText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Manually reset the day timer (call this when player sleeps in bed, etc.)
    /// </summary>
    public void ResetDayTimer()
    {
        _dayTimer = 0f;
        _isDayTime = true;
        _isTransitioning = false;
        _isFadingToBlack = false;
        _isFadingFromBlack = false;
        _fadeProgress = 0f;
        _disableAutoTransitions = false; // Re-enable auto transitions after manual reset
        
        // Set lights to day state
        SetLightStates(true);
        
        // Set skybox to day
        SetSkybox(true);
        
        // Hide fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.gameObject.SetActive(false);
        }
        
        Debug.Log("[DayNightCycle] Day timer reset. Starting new day cycle. Auto transitions re-enabled.");
    }

    /// <summary>
    /// Manually set to night time (call this when player sleeps in bed, etc.)
    /// </summary>
    public void SetToNight()
    {
        _isDayTime = false;
        _isTransitioning = false;
        _isFadingToBlack = false;
        _isFadingFromBlack = false;
        _fadeProgress = 0f;
        
        // Clear load flag so bed changes take effect immediately
        _justLoaded = false;
        _loadTime = 0f;
        _disableAutoTransitions = false; // Re-enable auto transitions after manual set to night
        
        // Set lights to night state
        SetLightStates(false);
        
        // Set skybox to night
        SetSkybox(false);
        
        // Hide fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.gameObject.SetActive(false);
        }
        
        Debug.Log("[DayNightCycle] Manually set to NIGHT. Auto transitions re-enabled.");
    }

    /// <summary>
    /// Manually set to day time
    /// </summary>
    public void SetToDay()
    {
        _dayTimer = 0f;
        _isDayTime = true;
        _isTransitioning = false;
        _isFadingToBlack = false;
        _isFadingFromBlack = false;
        _fadeProgress = 0f;
        
        // Set lights to day state
        SetLightStates(true);
        
        // Set skybox to day
        SetSkybox(true);
        
        // Hide fade overlay
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.gameObject.SetActive(false);
        }
        
        Debug.Log("[DayNightCycle] Manually set to DAY.");
    }

    /// <summary>
    /// Get remaining day time in minutes
    /// </summary>
    public float GetRemainingDayTimeMinutes()
    {
        if (!_isDayTime || _isTransitioning)
            return 0f;
        
        float elapsedMinutes = _dayTimer / 60f;
        return Mathf.Max(0f, dayDurationInMinutes - elapsedMinutes);
    }

    /// <summary>
    /// Check if currently in day time
    /// </summary>
    public bool IsDayTime()
    {
        return _isDayTime && !_isTransitioning;
    }

    /// <summary>
    /// Check if currently in night time
    /// </summary>
    public bool IsNightTime()
    {
        return !_isDayTime || _isTransitioning;
    }

    /// <summary>
    /// Legacy method for compatibility (uses default sunrise/sunset hours)
    /// </summary>
    public bool IsDayTime(float sunriseHour = 6f, float sunsetHour = 18f)
    {
        return IsDayTime();
    }

    /// <summary>
    /// Legacy method for compatibility (uses default sunrise/sunset hours)
    /// </summary>
    public bool IsNightTime(float sunriseHour = 6f, float sunsetHour = 18f)
    {
        return IsNightTime();
    }

    /// <summary>
    /// Get current time of day (for compatibility - returns 12 for day, 0 for night)
    /// </summary>
    public float GetTimeOfDay()
    {
        return _isDayTime ? 12f : 0f;
    }

    /// <summary>
    /// Check if currently transitioning (for debugging/saving)
    /// </summary>
    public bool IsTransitioning()
    {
        return _isTransitioning || _isFadingToBlack || _isFadingFromBlack;
    }
}
