using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [Tooltip("Duration of a full day cycle in real-world minutes.")]
    public float dayDurationInMinutes = 10f;
    [Range(0, 24)]
    [Tooltip("Current time of day in hours (0 = midnight, 12 = noon, 24 = next midnight).")]
    public float timeOfDay = 12f; // Start at noon

    [Header("Lighting References")]
    [Tooltip("Assign your main Directional Light that acts as the sun/moon.")]
    public Light sunLight;

    [Header("Sky & Light Properties")]
    [Tooltip("Color of the sun light over the 24-hour cycle.")]
    public Gradient sunColor;
    [Tooltip("Intensity of the sun light over the 24-hour cycle.")]
    public AnimationCurve sunIntensity;
    [Tooltip("Color of the ambient light over the 24-hour cycle.")]
    public Gradient ambientColor;
    [Tooltip("Color of the fog over the 24-hour cycle.")]
    public Gradient fogColor;

    [Header("Smoothing Settings")]
    [Tooltip("Smooth factor for light transitions (higher = smoother, prevents flickering).")]
    [Range(0.1f, 10f)]
    public float smoothingSpeed = 5f;
    [Tooltip("Update frequency - higher values reduce flicker but may look less responsive.")]
    [Range(0.01f, 0.1f)]
    public float updateInterval = 0.02f; // Update every 0.02 seconds instead of every frame
    [Tooltip("Shadow update interval - shadows update less frequently to prevent flickering (higher = more stable shadows).")]
    [Range(0.05f, 0.5f)]
    public float shadowUpdateInterval = 0.1f; // Update shadows every 0.1 seconds

    private float _timeScale;
    private float _updateTimer = 0f;
    
    // Cached values for smooth interpolation
    private Color _targetSunColor;
    private float _targetSunIntensity;
    private Color _currentSunColor;
    private float _currentSunIntensity;
    private Color _targetAmbientColor;
    private Color _currentAmbientColor;
    private Color _targetFogColor;
    private Color _currentFogColor;
    private float _targetXRotation;
    private float _currentXRotation;
    private float _shadowXRotation; // Separate rotation for shadows (updates less frequently)
    
    // Transition state
    private bool isTransitioning = false;
    private float targetTransitionTime = 0f;
    private float transitionDuration = 0f;
    private float transitionStartTime = 0f;
    private float transitionStartValue = 0f;

    void Start()
    {
        // Calculate time scale: (24 hours / dayDurationInMinutes) * minutes_in_hour (60) for a full day
        _timeScale = 24f / dayDurationInMinutes; // Now represents hours per minute
        
        // Initialize cached values
        float normalizedTime = timeOfDay / 24f;
        _targetSunColor = sunColor.Evaluate(normalizedTime);
        _currentSunColor = _targetSunColor;
        _targetSunIntensity = sunIntensity.Evaluate(normalizedTime);
        _currentSunIntensity = _targetSunIntensity;
        _targetAmbientColor = ambientColor.Evaluate(normalizedTime);
        _currentAmbientColor = _targetAmbientColor;
        _targetFogColor = fogColor.Evaluate(normalizedTime);
        _currentFogColor = _targetFogColor;
        _targetXRotation = Mathf.Lerp(-90f, 270f, normalizedTime);
        _currentXRotation = _targetXRotation;
        _shadowXRotation = _targetXRotation;
        
        UpdateEnvironment(); // Initial update
        
        // Configure light for stable shadows
        if (sunLight != null)
        {
            // Use StableFit shadow projection for smoother transitions
            sunLight.shadows = LightShadows.Soft;
            // Reduce shadow flicker by using stable shadow settings
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
        }
    }

    void Update()
    {
        // Only advance time if not transitioning
        if (!isTransitioning)
        {
            // Advance time of day
            timeOfDay += Time.deltaTime * _timeScale; // timeOfDay is in hours
            if (timeOfDay >= 24f) // If we pass midnight, loop back
            {
                timeOfDay -= 24f;
            }
        }

        // Update target values less frequently to reduce flicker
        _updateTimer += Time.deltaTime;
        if (_updateTimer >= updateInterval)
        {
            _updateTimer = 0f;
            UpdateTargetValues();
        }
        
        // Smoothly interpolate shadow rotation towards visual rotation every frame
        // Use slower interpolation to reduce shadow update frequency and prevent flicker
        float shadowLerpSpeed = 1f / shadowUpdateInterval; // Convert interval to speed
        float shadowLerpFactor = 1f - Mathf.Exp(-shadowLerpSpeed * Time.deltaTime);
        _shadowXRotation = Mathf.LerpAngle(_shadowXRotation, _currentXRotation, shadowLerpFactor);
        
        // Smoothly interpolate towards target values every frame
        SmoothUpdateEnvironment();
    }

    void UpdateTargetValues()
    {
        // Normalize time to a 0-1 range for gradients and curves
        float normalizedTime = timeOfDay / 24f;

        // Update target values (these change less frequently)
        _targetSunColor = sunColor.Evaluate(normalizedTime);
        _targetSunIntensity = sunIntensity.Evaluate(normalizedTime);
        _targetAmbientColor = ambientColor.Evaluate(normalizedTime);
        _targetFogColor = fogColor.Evaluate(normalizedTime);
        _targetXRotation = Mathf.Lerp(-90f, 270f, normalizedTime);
    }

    void SmoothUpdateEnvironment()
    {
        // Smooth interpolation factor based on smoothing speed
        float lerpFactor = 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);

        // Update Sun Light (Directional Light)
        if (sunLight != null)
        {
            // Smoothly interpolate rotation for visual updates
            _currentXRotation = Mathf.LerpAngle(_currentXRotation, _targetXRotation, lerpFactor);
            
            // Apply rotation to light - use shadow rotation which updates less frequently to reduce shadow flicker
            // The shadow rotation smoothly follows the visual rotation but updates at a lower frequency
            sunLight.transform.localRotation = Quaternion.Euler(_shadowXRotation, sunLight.transform.localEulerAngles.y, sunLight.transform.localEulerAngles.z);

            // Smoothly interpolate color and intensity
            _currentSunColor = Color.Lerp(_currentSunColor, _targetSunColor, lerpFactor);
            _currentSunIntensity = Mathf.Lerp(_currentSunIntensity, _targetSunIntensity, lerpFactor);
            
            sunLight.color = _currentSunColor;
            sunLight.intensity = _currentSunIntensity;
        }

        // Smoothly interpolate Ambient Light and Fog
        _currentAmbientColor = Color.Lerp(_currentAmbientColor, _targetAmbientColor, lerpFactor);
        _currentFogColor = Color.Lerp(_currentFogColor, _targetFogColor, lerpFactor);
        
        RenderSettings.ambientLight = _currentAmbientColor;
        RenderSettings.fogColor = _currentFogColor;
    }

    void UpdateEnvironment()
    {
        // Direct update (used only on Start)
        float normalizedTime = timeOfDay / 24f;

        if (sunLight != null)
        {
            float xRotation = Mathf.Lerp(-90f, 270f, normalizedTime);
            sunLight.transform.localRotation = Quaternion.Euler(xRotation, sunLight.transform.localEulerAngles.y, sunLight.transform.localEulerAngles.z);
            sunLight.color = sunColor.Evaluate(normalizedTime);
            sunLight.intensity = sunIntensity.Evaluate(normalizedTime);
        }

        RenderSettings.ambientLight = ambientColor.Evaluate(normalizedTime);
        RenderSettings.fogColor = fogColor.Evaluate(normalizedTime);
        
        // Handle time transition
        if (isTransitioning)
        {
            float elapsed = Time.time - transitionStartTime;
            if (elapsed < transitionDuration)
            {
                // Calculate progress (0 to 1)
                float progress = elapsed / transitionDuration;
                
                // Handle time wrapping (e.g., transitioning from 22 to 2 = going through midnight)
                float currentTime = Mathf.Lerp(transitionStartValue, targetTransitionTime, progress);
                
                // Ensure time wraps around 24 hours correctly
                if (Mathf.Abs(targetTransitionTime - transitionStartValue) > 12f)
                {
                    // Take shorter path (wrap around)
                    if (transitionStartValue > targetTransitionTime)
                    {
                        currentTime = Mathf.Lerp(transitionStartValue, targetTransitionTime + 24f, progress);
                        if (currentTime >= 24f) currentTime -= 24f;
                    }
                    else
                    {
                        currentTime = Mathf.Lerp(transitionStartValue - 24f, targetTransitionTime, progress);
                        if (currentTime < 0f) currentTime += 24f;
                    }
                }
                
                timeOfDay = currentTime;
                
                // Force immediate update of environment during transition
                UpdateTargetValues();
            }
            else
            {
                // Transition complete
                timeOfDay = targetTransitionTime;
                if (timeOfDay >= 24f) timeOfDay -= 24f;
                if (timeOfDay < 0f) timeOfDay += 24f;
                isTransitioning = false;
                UpdateTargetValues();
            }
        }
    }
    
    // Public method to transition to a specific time of day
    public void TransitionToTime(float targetTime, float duration)
    {
        if (duration <= 0f)
        {
            // Instant transition
            timeOfDay = targetTime;
            if (timeOfDay >= 24f) timeOfDay -= 24f;
            if (timeOfDay < 0f) timeOfDay += 24f;
            UpdateTargetValues();
            return;
        }
        
        // Normalize target time
        targetTime = targetTime % 24f;
        if (targetTime < 0f) targetTime += 24f;
        
        isTransitioning = true;
        targetTransitionTime = targetTime;
        transitionDuration = duration;
        transitionStartTime = Time.time;
        transitionStartValue = timeOfDay;
        
        Debug.Log($"[DayNightCycle] Transitioning from {transitionStartValue:F2} to {targetTransitionTime:F2} over {duration} seconds");
    }
} 