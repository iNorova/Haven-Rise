using UnityEngine;
using UnityEngine.UI;

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

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize timer mode
        _isDayTime = startInDay;
        _dayTimer = 0f;
        _isTransitioning = false;

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

        Debug.Log($"[DayNightCycle] Started. Initial state: {(_isDayTime ? "DAY" : "NIGHT")}, Day duration: {dayDurationInMinutes} minutes");
    }

    void Update()
    {
        // Periodically sync with actual light states (in case bed changed them)
        // Use time-based check instead of frame-based for consistency
        if (Time.frameCount % 60 == 0) // Check every 60 frames (~1 second at 60fps)
        {
            SyncFromLights();
        }
        
        // Only count timer during day (not during transition or night)
        if (_isDayTime && !_isTransitioning && !_isFadingToBlack && !_isFadingFromBlack)
        {
            // Count up the day timer
            _dayTimer += Time.deltaTime;
            
            // Convert to minutes
            float dayTimerMinutes = _dayTimer / 60f;
            
            // Check if we've reached the day duration
            if (dayTimerMinutes >= dayDurationInMinutes)
            {
                // Start transition to night
                StartTransitionToNight();
            }
        }
        
        // Handle fade to black phase
        if (_isFadingToBlack)
        {
            float elapsed = Time.time - _transitionStartTime;
            _fadeProgress = Mathf.Clamp01(elapsed / (transitionDuration * 0.5f)); // Fade to black takes half the duration
            
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
            _fadeProgress = Mathf.Clamp01(elapsed / (transitionDuration * 0.5f)); // Fade from black takes half the duration
            
            // Update fade overlay (fading out)
            if (fadeOverlay != null)
            {
                fadeOverlay.color = new Color(0f, 0f, 0f, 1f - _fadeProgress);
            }
            
            // Check if fade from black is complete
            if (_fadeProgress >= 1f)
            {
                CompleteTransitionToNight();
            }
        }
        
        // Update timer UI
        if (showTimer && timerText != null)
        {
            UpdateTimerUI();
        }
    }

    void StartTransitionToNight()
    {
        if (_isTransitioning || _isFadingToBlack || _isFadingFromBlack) return; // Already transitioning
        
        _isTransitioning = true;
        _transitionStartTime = Time.time;
        _fadeProgress = 0f;
        
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
        // Check actual light states to determine current state
        bool sunActive = sunLight != null && sunLight.gameObject.activeSelf;
        bool nightActive = nightLight != null && nightLight.gameObject.activeSelf;
        
        // Determine if it's day or night based on lights
        bool shouldBeDay = sunActive && !nightActive;
        
        // Only update if state changed
        if (shouldBeDay != _isDayTime)
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
        
        Debug.Log("[DayNightCycle] Day timer reset. Starting new day cycle.");
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
        
        Debug.Log("[DayNightCycle] Manually set to NIGHT.");
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
}
