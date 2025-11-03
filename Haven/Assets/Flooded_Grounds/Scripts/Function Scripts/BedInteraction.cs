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
    
    [Header("Skybox Settings")]
    [Tooltip("Skybox material to use during day. Leave empty to keep current skybox.")]
    public Material daySkybox;
    [Tooltip("Skybox material to use during night. Leave empty to keep current skybox.")]
    public Material nightSkybox;
    
    // Static references so all beds share the same skybox settings
    private static Material staticDaySkybox;
    private static Material staticNightSkybox;

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
                // Try to find any directional light (including inactive)
                Light[] lights = FindObjectsOfType<Light>(true); // Include inactive objects
                foreach (Light light in lights)
                {
                    if (light != null && light.type == LightType.Directional && light.name != "NIGHT")
                    {
                        sunLight = light;
                        Debug.Log($"BedInteraction: Found sun light via FindObjectsOfType in Start(): {light.gameObject.name}, Active: {light.gameObject.activeSelf}");
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
                // If not found on root, try children
                if (nightLight == null)
                {
                    nightLight = nightObj.GetComponentInChildren<Light>();
                }
            }
            
            // If still not found, search all lights (including inactive - NIGHT might be disabled)
            if (nightLight == null)
            {
                Light[] allLights = FindObjectsOfType<Light>(true); // Include inactive objects
                foreach (Light light in allLights)
                {
                    if (light != null && light.name == "NIGHT")
                    {
                        nightLight = light;
                        Debug.Log($"BedInteraction: Found NIGHT light by searching all lights in Start() (including inactive): {light.name}, Active: {light.gameObject.activeSelf}");
                        break;
                    }
                }
            }
            
            if (nightLight == null)
            {
                Debug.LogWarning("BedInteraction: NIGHT light not found in Start(). Please assign it in the inspector or name your night light 'NIGHT'. Will retry in UpdateNightStateFromLights.");
            }
            else
            {
                Debug.Log($"BedInteraction: Successfully found NIGHT light in Start(): {nightLight.name}");
            }
        }
        
        // Initialize isNight based on actual light states (check what the lights currently are)
        // This ensures newly placed beds inherit the correct day/night state
        // CRITICAL: Only READ the state, NEVER change it! This preserves day/night when placing beds
        UpdateNightStateFromLights();
        
        // Copy skybox materials to static references if they're assigned (so all beds share them)
        if (daySkybox != null)
        {
            staticDaySkybox = daySkybox;
        }
        if (nightSkybox != null)
        {
            staticNightSkybox = nightSkybox;
        }
        
        // If this instance doesn't have skybox assigned, try to get from static references
        if (daySkybox == null && staticDaySkybox != null)
        {
            daySkybox = staticDaySkybox;
        }
        if (nightSkybox == null && staticNightSkybox != null)
        {
            nightSkybox = staticNightSkybox;
        }
        
        // If still no skybox materials, try to find from another bed in the scene
        if ((daySkybox == null || nightSkybox == null))
        {
            BedInteraction[] allBeds = FindObjectsOfType<BedInteraction>();
            foreach (BedInteraction bed in allBeds)
            {
                if (bed != null && bed != this)
                {
                    if (daySkybox == null && bed.daySkybox != null)
                    {
                        daySkybox = bed.daySkybox;
                        staticDaySkybox = bed.daySkybox;
                    }
                    if (nightSkybox == null && bed.nightSkybox != null)
                    {
                        nightSkybox = bed.nightSkybox;
                        staticNightSkybox = bed.nightSkybox;
                    }
                    if (daySkybox != null && nightSkybox != null) break;
                }
            }
        }
        
        // Sync skybox to current state (only if skybox materials are assigned)
        // This ensures newly placed beds have the correct skybox matching the current day/night state
        if (daySkybox != null && nightSkybox != null)
        {
            // Get current skybox (RenderSettings.skybox might be null or different)
            Material currentSkybox = RenderSettings.skybox;
            
            // If it's night and we're not already using night skybox, switch it
            if (isNight)
            {
                if (currentSkybox != nightSkybox)
                {
                    RenderSettings.skybox = nightSkybox;
                    DynamicGI.UpdateEnvironment();
                    Debug.Log($"BedInteraction: Synced skybox to night skybox on initialization. Material name: {nightSkybox.name}");
                }
            }
            // If it's day and we're not already using day skybox, switch it
            else
            {
                if (currentSkybox != daySkybox)
                {
                    RenderSettings.skybox = daySkybox;
                    DynamicGI.UpdateEnvironment();
                    Debug.Log($"BedInteraction: Synced skybox to day skybox on initialization. Material name: {daySkybox.name}");
                }
            }
        }
        
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
    // Made public so BedPlacement can call it when placing beds
    public void UpdateNightStateFromLights()
    {
        // Re-find lights if not already found (for newly placed beds or re-placed beds)
        if (sunLight == null)
        {
            GameObject sunObj = GameObject.Find("Directional Light");
            if (sunObj != null) 
            {
                sunLight = sunObj.GetComponent<Light>();
            }
            else
            {
                // Try to find any directional light (including inactive)
                Light[] lights = FindObjectsOfType<Light>(true); // Include inactive objects
                foreach (Light light in lights)
                {
                    if (light != null && light.type == LightType.Directional && light.name != "NIGHT")
                    {
                        sunLight = light;
                        Debug.Log($"BedInteraction: Found sun light via FindObjectsOfType in UpdateNightStateFromLights: {light.gameObject.name}, Active: {light.gameObject.activeSelf}");
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
                    // Try to find Light component in children if not on root
                    nightLight = nightObj.GetComponentInChildren<Light>();
                }
            }
            
            // If still not found, try searching all lights (including inactive)
            if (nightLight == null)
            {
                Light[] allLights = FindObjectsOfType<Light>(true); // Include inactive objects
                foreach (Light light in allLights)
                {
                    if (light != null && light.name == "NIGHT")
                    {
                        nightLight = light;
                        Debug.Log($"BedInteraction: Found NIGHT light by searching all lights (including inactive): {light.name}, Active: {light.gameObject.activeSelf}");
                        break;
                    }
                }
            }
        }
        
        // Debug if lights aren't found
        if (sunLight == null || nightLight == null)
        {
            Debug.LogWarning($"BedInteraction: Lights status - Sun: {(sunLight != null ? sunLight.name : "NULL")}, NIGHT: {(nightLight != null ? nightLight.name : "NULL")}. Attempting to find lights...");
            
            // Final attempt to find lights
            if (sunLight == null)
            {
                GameObject sunObj = GameObject.Find("Directional Light");
                if (sunObj != null) sunLight = sunObj.GetComponent<Light>();
            }
            
            if (nightLight == null)
            {
                GameObject nightObj = GameObject.Find("NIGHT");
                if (nightObj != null) nightLight = nightObj.GetComponent<Light>();
                if (nightLight == null && nightObj != null) nightLight = nightObj.GetComponentInChildren<Light>();
            }
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
            // If we have at least one light, try to use what we have
            if (sunLight != null)
            {
                // We have sun but not NIGHT - try one more time to find NIGHT
                GameObject nightObj = GameObject.Find("NIGHT");
                if (nightObj != null)
                {
                    nightLight = nightObj.GetComponent<Light>();
                    // Try again with both lights
                    if (nightLight != null)
                    {
                        UpdateNightStateFromLights(); // Recursive call now that we have both
                        return;
                    }
                }
                else
                {
                    // Last resort: search all lights
                    Light[] allLights = FindObjectsOfType<Light>(true); // Include inactive
                    foreach (Light light in allLights)
                    {
                        if (light != null && light.name == "NIGHT")
                        {
                            nightLight = light;
                            Debug.Log($"BedInteraction: Found NIGHT light on retry: {light.gameObject.name}, Active: {light.gameObject.activeSelf}");
                            // Try again with both lights
                            UpdateNightStateFromLights(); // Recursive call now that we have both
                            return;
                        }
                    }
                }
            }
            
            Debug.LogWarning($"BedInteraction: Cannot update night state - lights not found. Sun: {sunLight != null}, NIGHT: {nightLight != null}. Will try to find lights on next interaction.");
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

        // ALWAYS re-find lights if they're null (critical for re-placed beds)
        // Do this BEFORE checking if lights exist, so we have the best chance of finding them
        if (sunLight == null || nightLight == null)
        {
            Debug.LogWarning($"BedInteraction: Lights are null before toggle! Sun: {sunLight != null}, NIGHT: {nightLight != null}. Re-finding lights...");
            
            // Force re-find lights
            if (sunLight == null)
            {
                GameObject sunObj = GameObject.Find("Directional Light");
                if (sunObj != null) 
                {
                    sunLight = sunObj.GetComponent<Light>();
                    if (sunLight == null)
                    {
                        Light[] lights = FindObjectsOfType<Light>(true); // Include inactive
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
                // Last resort: search all lights (including inactive - NIGHT might be disabled)
                if (nightLight == null)
                {
                    Light[] allLights = FindObjectsOfType<Light>(true); // Include inactive objects
                    foreach (Light light in allLights)
                    {
                        if (light != null && light.name == "NIGHT")
                        {
                            nightLight = light;
                            Debug.Log($"BedInteraction: Found NIGHT light during toggle search: {light.name}");
                            break;
                        }
                    }
                }
            }
            
            Debug.Log($"BedInteraction: After re-finding - Sun: {(sunLight != null ? sunLight.name : "NULL")}, NIGHT: {(nightLight != null ? nightLight.name : "NULL")}");
        }
        
        // Final check: if we still don't have both lights, we can't toggle
        if (sunLight == null || nightLight == null)
        {
            Debug.LogError($"BedInteraction: Cannot toggle day/night - lights not found after all attempts. Sun: {(sunLight != null ? sunLight.name : "NULL")}, NIGHT: {(nightLight != null ? nightLight.name : "NULL")}. Searching for all lights in scene...");
            
            // Final debug: list all lights in scene
            Light[] allLights = FindObjectsOfType<Light>();
            Debug.Log($"BedInteraction: All lights in scene ({allLights.Length} total):");
            foreach (Light light in allLights)
            {
                if (light != null)
                {
                    Debug.Log($"  - {light.name} (Type: {light.type}, Active: {light.gameObject.activeSelf})");
                }
            }
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
            
            // Toggle lights and skybox while screen is black based on transition target
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
                
                // Switch to night skybox (use static reference if instance is null)
                Material skyboxToUse = nightSkybox != null ? nightSkybox : staticNightSkybox;
                if (skyboxToUse != null)
                {
                    Material previousSkybox = RenderSettings.skybox;
                    RenderSettings.skybox = skyboxToUse;
                    
                    // Force Unity to update the skybox and lighting
                    DynamicGI.UpdateEnvironment();
                    
                    // Verify the change took effect
                    Material currentSkybox = RenderSettings.skybox;
                    Debug.Log($"BedInteraction: Skybox changed to night skybox. Previous: {(previousSkybox != null ? previousSkybox.name : "NULL")}, New: {skyboxToUse.name}, Current: {(currentSkybox != null ? currentSkybox.name : "NULL")}");
                    
                    if (currentSkybox != skyboxToUse)
                    {
                        Debug.LogError($"BedInteraction: FAILED to set skybox! Expected: {skyboxToUse.name}, Got: {(currentSkybox != null ? currentSkybox.name : "NULL")}");
                    }
                }
                else
                {
                    Debug.LogWarning("BedInteraction: nightSkybox is not assigned! Please assign it in the inspector on the bed prefab.");
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
                
                // Switch to day skybox (use static reference if instance is null)
                Material skyboxToUse = daySkybox != null ? daySkybox : staticDaySkybox;
                if (skyboxToUse != null)
                {
                    Material previousSkybox = RenderSettings.skybox;
                    RenderSettings.skybox = skyboxToUse;
                    
                    // Force Unity to update the skybox and lighting
                    DynamicGI.UpdateEnvironment();
                    
                    // Verify the change took effect
                    Material currentSkybox = RenderSettings.skybox;
                    Debug.Log($"BedInteraction: Skybox changed to day skybox. Previous: {(previousSkybox != null ? previousSkybox.name : "NULL")}, New: {skyboxToUse.name}, Current: {(currentSkybox != null ? currentSkybox.name : "NULL")}");
                    
                    if (currentSkybox != skyboxToUse)
                    {
                        Debug.LogError($"BedInteraction: FAILED to set skybox! Expected: {skyboxToUse.name}, Got: {(currentSkybox != null ? currentSkybox.name : "NULL")}");
                    }
                }
                else
                {
                    Debug.LogWarning("BedInteraction: daySkybox is not assigned! Please assign it in the inspector on the bed prefab.");
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
            // If no fade overlay, just toggle lights and skybox instantly
            if (transitionToNight)
            {
                if (sunLight != null) sunLight.gameObject.SetActive(false);
                if (nightLight != null) nightLight.gameObject.SetActive(true);
                Material skyboxToUse = nightSkybox != null ? nightSkybox : staticNightSkybox;
                if (skyboxToUse != null)
                {
                    RenderSettings.skybox = skyboxToUse;
                    DynamicGI.UpdateEnvironment();
                    Debug.Log($"BedInteraction: Skybox changed to night skybox (instant). Material name: {skyboxToUse.name}");
                }
                else
                {
                    Debug.LogWarning("BedInteraction: nightSkybox is not assigned! Please assign it in the inspector on the bed prefab.");
                }
            }
            else
            {
                if (sunLight != null) sunLight.gameObject.SetActive(true);
                if (nightLight != null) nightLight.gameObject.SetActive(false);
                Material skyboxToUse = daySkybox != null ? daySkybox : staticDaySkybox;
                if (skyboxToUse != null)
                {
                    RenderSettings.skybox = skyboxToUse;
                    DynamicGI.UpdateEnvironment();
                    Debug.Log($"BedInteraction: Skybox changed to day skybox (instant). Material name: {skyboxToUse.name}");
                }
                else
                {
                    Debug.LogWarning("BedInteraction: daySkybox is not assigned! Please assign it in the inspector on the bed prefab.");
                }
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
