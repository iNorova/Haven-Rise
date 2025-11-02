using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BedInteraction : MonoBehaviour
{
    [Header("Sleep Settings")]
    public KeyCode sleepKey = KeyCode.G;       // Key to toggle day/night
    public float interactionRange = 3f;        // How close player needs to be to interact
    
    [Header("Light References")]
    [Tooltip("The main sun light (will be disabled during night). If not assigned, will search for 'Directional Light'.")]
    public Light sunLight;
    [Tooltip("The NIGHT light (black color, will be enabled during night). Must be named 'NIGHT' in the scene.")]
    public Light nightLight;
    
    [Header("Fade Settings")]
    public Image fadeOverlay;                  // Optional: UI Image for fade effect (create one if needed)
    public float fadeDuration = 2f;            // Duration of fade transition in seconds
    
    private Camera playerCamera;
    private GameObject player;
    private CharController_Motor playerMotor;
    public bool isTransitioning = false; // Made public so BedPickup can check it
    private static bool isNight = false; // Static: shared state across all beds

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("BedInteraction: No main camera found!");
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMotor = player.GetComponent<CharController_Motor>();
        }
        
        // Find sun light if not assigned
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
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional && light.name != "NIGHT")
                    {
                        sunLight = light;
                        break;
                    }
                }
            }
            
            if (sunLight == null)
            {
                Debug.LogWarning("BedInteraction: Sun light not found. Please assign it in the inspector or name your directional light 'Directional Light'.");
            }
        }
        
        // Find NIGHT light if not assigned
        if (nightLight == null)
        {
            GameObject nightObj = GameObject.Find("NIGHT");
            if (nightObj != null)
            {
                nightLight = nightObj.GetComponent<Light>();
            }
            
            if (nightLight == null)
            {
                Debug.LogWarning("BedInteraction: NIGHT light not found. Please assign it in the inspector or name your night light 'NIGHT'.");
            }
        }
        
        // Initialize isNight based on actual light states (check what the lights currently are)
        // This ensures newly placed beds inherit the correct day/night state
        // CRITICAL: Only READ the state, NEVER change it! This preserves day/night when placing beds
        UpdateNightStateFromLights();
        
        // DO NOT change lights here - only read them!
        // The lights should only be changed when the player actually sleeps on the bed
        if (sunLight != null && nightLight != null)
        {
            bool sunActive = sunLight.gameObject.activeSelf;
            bool nightActive = nightLight.gameObject.activeSelf;
            Debug.Log($"BedInteraction: Initialized. Reading light state: Sun active={sunActive}, NIGHT active={nightActive}, isNight={isNight}. NOT changing lights.");
        }
        
        // Create fade overlay if not assigned
        if (fadeOverlay == null)
        {
            CreateFadeOverlay();
        }
        
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f); // Start transparent
            fadeOverlay.gameObject.SetActive(true);
        }
    }
    
    void CreateFadeOverlay()
    {
        // Try to find existing fade overlay in scene
        GameObject existingOverlay = GameObject.Find("FadeOverlay");
        if (existingOverlay != null)
        {
            fadeOverlay = existingOverlay.GetComponent<Image>();
            if (fadeOverlay != null) return;
        }
        
        // Create new fade overlay
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            // Create canvas if it doesn't exist
            GameObject canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();
            canvasObj = canvas;
        }
        
        GameObject overlayObj = new GameObject("FadeOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        
        fadeOverlay = overlayObj.AddComponent<Image>();
        fadeOverlay.color = Color.black;
        
        RectTransform rectTransform = fadeOverlay.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        fadeOverlay.raycastTarget = false; // Don't block clicks
        overlayObj.SetActive(true);
    }

    void Update()
    {
        // Don't process input if transitioning
        if (isTransitioning)
        {
            return;
        }

        // Check if player is looking at this bed and pressing G
        if (playerCamera != null && Input.GetKeyDown(sleepKey))
        {
            TryToggleDayNight();
        }
    }
    
    // Static method to check if any bed is currently transitioning
    public static bool IsAnyBedActive()
    {
        BedInteraction[] beds = FindObjectsByType<BedInteraction>(FindObjectsSortMode.None);
        foreach (BedInteraction bed in beds)
        {
            if (bed != null && bed.isTransitioning)
            {
                return true;
            }
        }
        return false;
    }
    
    // Static method for other scripts to check if bed was just exited via ESC (kept for compatibility)
    public static bool WasBedJustExitedViaEsc()
    {
        return false; // No longer used, but kept for compatibility
    }
    
    // Update isNight state based on actual light states
    void UpdateNightStateFromLights()
    {
        // Re-find lights if not already found (for newly placed beds)
        if (sunLight == null)
        {
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj != null) sunLight = sunObj.GetComponent<Light>();
        }
        if (nightLight == null)
        {
            GameObject nightObj = GameObject.Find("NIGHT");
            if (nightObj != null) nightLight = nightObj.GetComponent<Light>();
        }
        
        if (sunLight != null && nightLight != null)
        {
            // Check actual light states to determine if it's night
            bool sunActive = sunLight.gameObject.activeSelf;
            bool nightActive = nightLight.gameObject.activeSelf;
            
            // If night light is active and sun is not, it's night
            if (nightActive && !sunActive)
            {
                isNight = true;
                Debug.Log($"BedInteraction: Updated night state from lights. isNight={isNight}, Sun active={sunActive}, NIGHT active={nightActive}");
                return; // Don't change lights, just update state
            }
            // If sun is active and night is not, it's day
            else if (sunActive && !nightActive)
            {
                isNight = false;
                Debug.Log($"BedInteraction: Updated night state from lights. isNight={isNight}, Sun active={sunActive}, NIGHT active={nightActive}");
                return; // Don't change lights, just update state
            }
            // If states are inconsistent, prefer night if NIGHT is active
            else if (nightActive)
            {
                isNight = true;
                Debug.Log($"BedInteraction: Inconsistent light state detected. NIGHT is active, setting isNight=true");
            }
            else
            {
                // Default to day only if we can't determine
                isNight = false;
                Debug.Log($"BedInteraction: Could not determine state, defaulting to day");
            }
        }
        else
        {
            Debug.LogWarning($"BedInteraction: Cannot update night state - lights not found. Sun: {sunLight != null}, NIGHT: {nightLight != null}");
        }
    }

    void TryToggleDayNight()
    {
        // Check if player is close enough and looking at the bed
        if (player == null || playerCamera == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > interactionRange)
        {
            return;
        }

        // Raycast to check if player is looking at this bed
        RaycastHit hit;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        
        if (Physics.Raycast(ray, out hit, interactionRange))
        {
            if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(transform))
            {
                StartToggleDayNight();
            }
        }
    }

    void StartToggleDayNight()
    {
        if (isTransitioning)
            return;

        // Check if we have lights to toggle
        if (sunLight == null && nightLight == null)
        {
            Debug.LogWarning("BedInteraction: No lights found! Cannot toggle day/night.");
            return;
        }

        // Re-check actual light states BEFORE toggling (in case they were changed externally)
        // This ensures we always toggle from the actual current state
        UpdateNightStateFromLights();
        
        // Now toggle based on ACTUAL current state
        bool currentIsNight = isNight;
        isNight = !isNight; // Toggle for next time
        string transitionType = isNight ? "night" : "day";
        
        Debug.Log($"BedInteraction: Toggling from {(currentIsNight ? "night" : "day")} to {transitionType}. Current: Sun={sunLight?.gameObject.activeSelf}, NIGHT={nightLight?.gameObject.activeSelf}");
        isTransitioning = true;

        // Disable player movement during transition
        if (playerMotor != null)
        {
            playerMotor.SetInputActive(false);
        }

        // Start fade and light toggle coroutine (pass the new state we're transitioning TO)
        StartCoroutine(ToggleDayNightCoroutine(isNight));
    }
    
    IEnumerator ToggleDayNightCoroutine(bool transitionToNight)
    {
        // Fade to black
        if (fadeOverlay != null)
        {
            float fadeTimer = 0f;
            while (fadeTimer < fadeDuration * 0.5f) // Fade in (first half)
            {
                fadeTimer += Time.deltaTime;
                float alpha = Mathf.Clamp01(fadeTimer / (fadeDuration * 0.5f));
                fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            
            // Toggle lights while screen is black based on transition target
            if (transitionToNight)
            {
                // Switch to night: disable sun, enable NIGHT
                if (sunLight != null)
                {
                    sunLight.gameObject.SetActive(false);
                }
                if (nightLight != null)
                {
                    nightLight.gameObject.SetActive(true);
                }
                Debug.Log($"BedInteraction: Switching to NIGHT - Sun disabled, NIGHT enabled");
            }
            else
            {
                // Switch to day: enable sun, disable NIGHT
                if (sunLight != null)
                {
                    sunLight.gameObject.SetActive(true);
                }
                if (nightLight != null)
                {
                    nightLight.gameObject.SetActive(false);
                }
                Debug.Log($"BedInteraction: Switching to DAY - Sun enabled, NIGHT disabled");
            }
            
            Debug.Log($"BedInteraction: Lights toggled. Sun: {(sunLight != null && sunLight.gameObject.activeSelf)}, NIGHT: {(nightLight != null && nightLight.gameObject.activeSelf)}");
            
            // Wait a moment for lights to settle
            yield return new WaitForSeconds(0.1f);
            
            // Fade out (second half)
            fadeTimer = 0f;
            while (fadeTimer < fadeDuration * 0.5f)
            {
                fadeTimer += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(fadeTimer / (fadeDuration * 0.5f));
                fadeOverlay.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
            
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f); // Ensure fully transparent
        }
        else
        {
            // If no fade overlay, just toggle lights instantly
            if (transitionToNight)
            {
                if (sunLight != null) sunLight.gameObject.SetActive(false);
                if (nightLight != null) nightLight.gameObject.SetActive(true);
            }
            else
            {
                if (sunLight != null) sunLight.gameObject.SetActive(true);
                if (nightLight != null) nightLight.gameObject.SetActive(false);
            }
            yield return new WaitForSeconds(fadeDuration);
        }

        // Re-enable player movement
        if (playerMotor != null)
        {
            playerMotor.SetInputActive(true);
        }

        isTransitioning = false;
        string transitionType = transitionToNight ? "night" : "day";
        Debug.Log($"BedInteraction: Transition to {transitionType} complete. Final state: Sun={sunLight?.gameObject.activeSelf}, NIGHT={nightLight?.gameObject.activeSelf}");
    }

    // Visualize interaction range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
