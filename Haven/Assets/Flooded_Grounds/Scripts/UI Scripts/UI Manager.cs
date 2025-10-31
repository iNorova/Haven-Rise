using UnityEngine;
using UnityEngine.UI; // For UI elements
using UnityEngine.Events;
using System;

public class UIManager : MonoBehaviour
{
    [Header("Pause Menu")]
    public Button pauseButton;        // Reference to pause button
    public RawImage pauseMenuImage;   // Reference to the pause menu background
    private bool isPaused = false;    // Track pause state

    [Header("Temperature System")]
    public Slider temperatureSlider;              // Reference to temperature UI slider
    public Image temperatureFillImage;            // Reference to the slider's fill image
    public Color normalTemperatureColor = new Color(0f, 0.75f, 1f);    // Cool blue (#00BFFF)
    public Color dangerTemperatureColor = new Color(1f, 0.65f, 0f);    // Warm orange (#FFA500)
    public Color criticalTemperatureColor = new Color(1f, 0f, 0f);     // Hot red (#FF0000)
    public float maxTemperatureValue = 100f;      // Maximum temperature value
    public float minTemperatureValue = 0f;        // Minimum temperature value
    public float temperatureIncreaseRate = 5f;    // How fast temperature increases when trees are cut
    public float temperatureDecreaseRate = 2f;    // How fast temperature decreases over time
    public float temperatureIncreaseDuration = 5f;  // How long temperature increases after tree destruction
    public float permanentTemperatureIncreasePerTree = 2f;  // Temperature increase per tree cut
    public float temperatureDecreasePerTree = 2f;  // Temperature decrease per tree planted
    
    [Header("Temperature Thresholds")]
    public float dangerThreshold = 75f;       // Temperature level where danger effects start
    public float criticalThreshold = 90f;     // Temperature level where critical effects start

    [Header("Temperature Hooks")]
    public TemperatureVisualEffects[] temperatureVisualTargets; // Drag one or many visual effect controllers here
    public UnityEvent<float> onTemperatureChanged; // Designers can hook other reactions in Inspector
    private float lastNotifiedTemperature = -999f;

    [Header("Health System")]
    public Slider healthSlider;               // Reference to health UI slider
    public Image healthFillImage;             // Reference to the health slider's fill image
    public Color normalHealthColor = new Color(0f, 1f, 0f);     // Green
    public Color lowHealthColor = new Color(1f, 0.5f, 0f);      // Orange
    public Color criticalHealthColor = new Color(1f, 0f, 0f);   // Red
    public float maxHealth = 100f;
    public float currentHealth;
    public float healthDamageRate = 5f;       // How much health is lost per second in critical temperature
    public float healthDamageInterval = 1f;   // How often health damage is applied (in seconds)
    private float healthDamageTimer = 0f;

    [Header("Stamina System")]
    public Slider staminaSlider;              // Reference to stamina UI slider
    public Image staminaFillImage;            // Reference to the slider's fill image
    public Color normalStaminaColor = new Color(0f, 1f, 0f);     // Green (#00FF00)
    public Color lowStaminaColor = new Color(1f, 0.5f, 0f);      // Orange (#FF8000)
    public float maxStamina = 100f;           // Maximum stamina value
    public float staminaUseRate = 25f;        // How fast stamina depletes when sprinting
    public float normalStaminaRegenRate = 15f; // How fast stamina regenerates normally
    public float staminaRegenDelay = 1f;      // How long to wait before regenerating stamina
    public float dangerTempStaminaDebuff = 5f; // How much to reduce stamina regen in danger zone
    public float criticalTempStaminaDebuff = 5f; // How much to reduce stamina regen in critical zone
    private float currentStamina;             // Current stamina value
    private float staminaRegenTimer;          // Timer for stamina regeneration delay
    private bool isUsingStamina;              // Whether player is currently using stamina

    private float currentTemperature;
    private float permanentTemperatureIncrease = 0f;  // Tracks permanent temperature increase from deforestation
    private bool isTemperatureIncreasing = false; // Flag to control temperature increase
    private float temperatureIncreaseTimer = 0f;  // Timer for temperature increase duration

    // Static instance for global access
    public static UIManager Instance { get; private set; }
    public static event Action OnPlayerDeath;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("UI Manager Started");
        
        // Initialize temperature system
        currentTemperature = minTemperatureValue;
        if (temperatureSlider != null)
        {
            temperatureSlider.maxValue = maxTemperatureValue;
            temperatureSlider.minValue = minTemperatureValue;
            temperatureSlider.value = currentTemperature;
            
            // Set initial color
            if (temperatureFillImage != null)
            {
                temperatureFillImage.color = normalTemperatureColor;
            }
        }
        else
        {
            Debug.LogError("Temperature Slider is not assigned in the inspector!");
        }

        // Initialize health system
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.minValue = 0f;
            healthSlider.value = currentHealth;
            
            // Set initial color
            if (healthFillImage != null)
            {
                healthFillImage.color = normalHealthColor;
            }
        }
        else
        {
            Debug.LogError("Health Slider is not assigned in the inspector!");
        }

        // Initialize stamina system
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.minValue = 0f;
            staminaSlider.value = currentStamina;
            if (staminaFillImage != null)
            {
                staminaFillImage.fillAmount = 1f;
                staminaFillImage.color = normalStaminaColor;
            }
            Debug.Log($"Stamina slider initialized - Max: {maxStamina}, Current: {currentStamina}");
        }
        else
        {
            Debug.LogError("Stamina Slider is not assigned in the inspector!");
        }

        // Check if components are assigned
        if (pauseButton == null)
        {
            Debug.LogError("Pause Button is not assigned in the inspector!");
            return;
        }

        if (pauseMenuImage == null)
        {
            Debug.LogError("Pause Menu Image is not assigned in the inspector!");
            return;
        }

        // Ensure pause menu is hidden at start
        pauseMenuImage.gameObject.SetActive(false);
        ResumeGame(); // Ensure game starts in unpaused state

        // Add click listener to pause button
        pauseButton.onClick.AddListener(TogglePauseMenu);
        Debug.Log("Pause button listener added successfully");

        // Push initial temperature to hooks
        NotifyTemperatureChanged(GetCurrentTemperature());
    }

    // Update is called once per frame
    void Update()
    {
        // Handle pause menu input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Check if inventory is open first - if so, don't handle pause menu (inventory will close itself)
            InventoryUIManager inventoryUI = FindFirstObjectByType<InventoryUIManager>();
            if (inventoryUI != null && inventoryUI.IsInventoryOpen())
            {
                // Inventory is open, let InventoryUIManager handle closing it
                // Don't open pause menu yet - wait for next ESC press
                return;
            }
            
            // Check if inventory was just closed via ESC this frame - if so, don't open pause menu yet
            if (InventoryUIManager.WasInventoryJustClosedViaEsc())
            {
                // Inventory was just closed, skip opening pause menu - wait for next ESC press
                return;
            }
            
            // Only toggle pause menu if inventory is not open and wasn't just closed
            Debug.Log("ESC key pressed");
            TogglePauseMenu();
        }

        if (!isPaused)
        {
            // Update temperature system
            UpdateTemperatureSystem();

            // Update health damage in critical temperature
            if (currentTemperature >= criticalThreshold)
            {
                healthDamageTimer += Time.deltaTime;
                if (healthDamageTimer >= healthDamageInterval)
                {
                    TakeHealthDamage();
                    healthDamageTimer = 0f;
                }
            }

            // Update stamina system
            UpdateStaminaSystem();
        }
    }

    private void UpdateTemperatureSystem()
    {
        if (!isPaused)
        {
            if (isTemperatureIncreasing)
            {
                // Increase temperature
                currentTemperature += temperatureIncreaseRate * Time.deltaTime;
                
                // Update temperature increase timer
                temperatureIncreaseTimer += Time.deltaTime;
                if (temperatureIncreaseTimer >= temperatureIncreaseDuration)
                {
                    isTemperatureIncreasing = false;
                    temperatureIncreaseTimer = 0f;
                }
            }

            // Calculate final temperature including permanent increase
            float finalTemperature = currentTemperature + permanentTemperatureIncrease;

            // Clamp the value between min and max
            finalTemperature = Mathf.Clamp(finalTemperature, minTemperatureValue, maxTemperatureValue);

            // Update the slider
            if (temperatureSlider != null)
            {
                temperatureSlider.value = finalTemperature;
                
                // Update fill color based on temperature
                if (temperatureFillImage != null)
                {
                    if (finalTemperature >= criticalThreshold)
                    {
                        temperatureFillImage.color = criticalTemperatureColor;
                    }
                    else if (finalTemperature >= dangerThreshold)
                    {
                        temperatureFillImage.color = dangerTemperatureColor;
                    }
                    else
                    {
                        temperatureFillImage.color = normalTemperatureColor;
                    }
                }
            }

            // Notify listeners/visual systems if value changed
            if (Mathf.Abs(finalTemperature - lastNotifiedTemperature) > 0.001f)
            {
                NotifyTemperatureChanged(finalTemperature);
            }

            // Check for temperature thresholds and trigger effects
            CheckTemperatureEffects();
        }
    }

    private void NotifyTemperatureChanged(float value)
    {
        lastNotifiedTemperature = value;
        // Push to any assigned visual targets (Inspector array)
        if (temperatureVisualTargets != null)
        {
            for (int i = 0; i < temperatureVisualTargets.Length; i++)
            {
                var tgt = temperatureVisualTargets[i];
                if (tgt != null)
                {
                    tgt.SetTemperature(value);
                }
            }
        }
        // Fire UnityEvent for any other listeners
        if (onTemperatureChanged != null)
        {
            onTemperatureChanged.Invoke(value);
        }
    }

    private void CheckTemperatureEffects()
    {
        if (currentTemperature >= criticalThreshold)
        {
            ApplyCriticalTemperatureEffects();
        }
        else if (currentTemperature >= dangerThreshold)
        {
            ApplyDangerTemperatureEffects();
        }
    }

    private void ApplyDangerTemperatureEffects()
    {
        // Apply danger level effects
        // - Add slight screen tint
        // - Increase ambient temperature
        // - Add subtle heat distortion
        Debug.Log("Danger temperature level reached!");
    }

    private void ApplyCriticalTemperatureEffects()
    {
        // Apply critical level effects
        // - Strong screen tint
        // - Heavy heat distortion
        // - Player takes damage over time
        Debug.Log("Critical temperature level reached!");
    }

    // Public methods to control temperature state
    public void StartTemperatureIncrease()
    {
        isTemperatureIncreasing = true;
        // Add permanent temperature increase when tree is cut
        permanentTemperatureIncrease += permanentTemperatureIncreasePerTree;
        // Ensure permanent increase doesn't exceed max temperature
        permanentTemperatureIncrease = Mathf.Min(permanentTemperatureIncrease, maxTemperatureValue);
    }

    public void DecreaseTemperatureFromTreePlanting()
    {
        // Decrease permanent temperature increase when tree is planted
        permanentTemperatureIncrease = Mathf.Max(0f, permanentTemperatureIncrease - temperatureDecreasePerTree);
        // Update the current temperature to reflect the change immediately
        currentTemperature = Mathf.Max(0f, currentTemperature - temperatureDecreasePerTree);
        Debug.Log($"Temperature decreased by {temperatureDecreasePerTree}. New temperature: {GetCurrentTemperature()}");
        // Push update immediately so visuals respond right away
        NotifyTemperatureChanged(GetCurrentTemperature());
    }

    // New method to handle SproutSeed planting
    public void OnSproutSeedPlanted()
    {
        // Decrease temperature when a sprout seed is planted
        DecreaseTemperatureFromTreePlanting();
        Debug.Log("SproutSeed planted - decreasing temperature");
    }

    public float GetCurrentTemperature()
    {
        return currentTemperature + permanentTemperatureIncrease;
    }

    public bool IsInDangerZone()
    {
        return currentTemperature >= dangerThreshold;
    }

    public bool IsInCriticalZone()
    {
        return currentTemperature >= criticalThreshold;
    }

    public void TogglePauseMenu()
    {
        Debug.Log("TogglePauseMenu called");
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    private void PauseGame()
    {
        Debug.Log("Pausing game");
        isPaused = true;
        Time.timeScale = 0f;
        pauseMenuImage.gameObject.SetActive(true);
        
        // Disable player movement scripts
        DisablePlayerControls();
        
        // Optional: Pause audio
        AudioListener.pause = true;
    }

    private void ResumeGame()
    {
        Debug.Log("Resuming game");
        isPaused = false;
        Time.timeScale = 1f;
        pauseMenuImage.gameObject.SetActive(false);
        
        // Enable player movement scripts
        EnablePlayerControls();
        
        // Optional: Resume audio
        AudioListener.pause = false;
    }

    private void DisablePlayerControls()
    {
        // Find the player GameObject
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Disable all Rigidbody movement
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Disable character controller if present
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // Disable any movement scripts
            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script.GetType().Name.ToLower().Contains("movement") 
                    || script.GetType().Name.ToLower().Contains("controller")
                    || script.GetType().Name.ToLower().Contains("input"))
                {
                    script.enabled = false;
                }
            }
        }
    }

    private void EnablePlayerControls()
    {
        // Find the player GameObject
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Re-enable Rigidbody movement
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Re-enable character controller if present
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = true;
            }

            // Re-enable any movement scripts
            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && script.GetType().Name.ToLower().Contains("movement") 
                    || script.GetType().Name.ToLower().Contains("controller")
                    || script.GetType().Name.ToLower().Contains("input"))
                {
                    script.enabled = true;
                }
            }
        }
    }

    // Public method to check if game is paused
    public bool IsGamePaused()
    {
        return isPaused;
    }

    private void UpdateStaminaSystem()
    {
        // Regenerate stamina whenever not using stamina
        if (!isUsingStamina && currentStamina < maxStamina)
        {
            float regenRate = GetStaminaRegenRate();
            currentStamina = Mathf.Min(currentStamina + regenRate * Time.deltaTime, maxStamina);
            Debug.Log($"Regenerating stamina: {currentStamina} at rate {regenRate}");
        }

        // Update stamina UI
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
            if (staminaFillImage != null)
            {
                staminaFillImage.fillAmount = currentStamina / maxStamina;
                staminaFillImage.color = currentStamina < maxStamina * 0.3f ? lowStaminaColor : normalStaminaColor;
            }
        }
    }

    private float GetStaminaRegenRate()
    {
        float currentTemp = GetCurrentTemperature();
        float regenRate = normalStaminaRegenRate;
        
        // Check if temperature is in critical zone
        if (currentTemp >= criticalThreshold)
        {
            regenRate -= criticalTempStaminaDebuff;
            Debug.Log($"Temperature in critical zone ({currentTemp} >= {criticalThreshold}). Stamina regen reduced by {criticalTempStaminaDebuff}. New rate: {regenRate}");
        }
        // Check if temperature is in danger zone
        else if (currentTemp >= dangerThreshold)
        {
            regenRate -= dangerTempStaminaDebuff;
            Debug.Log($"Temperature in danger zone ({currentTemp} >= {dangerThreshold}). Stamina regen reduced by {dangerTempStaminaDebuff}. New rate: {regenRate}");
        }
        else
        {
            Debug.Log($"Temperature in safe zone ({currentTemp} < {dangerThreshold}). Stamina regen at normal rate: {regenRate}");
        }
        
        // Ensure regeneration rate doesn't go below 1
        return Mathf.Max(1f, regenRate);
    }

    public void UseStamina()
    {
        isUsingStamina = true;
        currentStamina = Mathf.Max(currentStamina - staminaUseRate * Time.deltaTime, 0f);
        Debug.Log($"Using stamina: {currentStamina}");
    }

    public void StopUsingStamina()
    {
        isUsingStamina = false;
        Debug.Log("Stopped using stamina, will start regenerating");
    }

    private void TakeHealthDamage()
    {
        currentHealth -= healthDamageRate;
        currentHealth = Mathf.Max(currentHealth, 0f);
        
        // Update health UI
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            
            // Update health color based on amount
            if (healthFillImage != null)
            {
                if (currentHealth < maxHealth * 0.3f)
                {
                    healthFillImage.color = criticalHealthColor;
                }
                else if (currentHealth < maxHealth * 0.6f)
                {
                    healthFillImage.color = lowHealthColor;
                }
                else
                {
                    healthFillImage.color = normalHealthColor;
                }
            }
        }

        Debug.Log($"Health damaged by heat. Current health: {currentHealth}");

        // Check if player died
        if (currentHealth <= 0f)
        {
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        Debug.Log("Player died from heat damage!");
        // Broadcast death to listeners (e.g., RespawnManager)
        OnPlayerDeath?.Invoke();
    }

    public bool CanSprint()
    {
        return currentStamina > 0;
    }

    // Public helper to fully restore health and update UI immediately
    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
            if (healthFillImage != null)
            {
                healthFillImage.color = normalHealthColor;
            }
        }
    }

    // Reset stamina to full and update UI
    public void RestoreFullStamina()
    {
        currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
            if (staminaFillImage != null)
            {
                staminaFillImage.fillAmount = 1f;
                staminaFillImage.color = normalStaminaColor;
            }
        }
        isUsingStamina = false;
        staminaRegenTimer = 0f;
    }

    // Reset temperature to minimum and clear permanent increase
    public void ResetTemperature()
    {
        currentTemperature = minTemperatureValue;
        permanentTemperatureIncrease = 0f;
        isTemperatureIncreasing = false;
        temperatureIncreaseTimer = 0f;
        if (temperatureSlider != null)
        {
            temperatureSlider.value = GetCurrentTemperature();
            if (temperatureFillImage != null)
            {
                temperatureFillImage.color = normalTemperatureColor;
            }
        }
        NotifyTemperatureChanged(GetCurrentTemperature());
    }

    // Convenience method to reset all gameplay stats on respawn
    public void ResetAllStats()
    {
        RestoreFullHealth();
        RestoreFullStamina();
        ResetTemperature();
    }
}
