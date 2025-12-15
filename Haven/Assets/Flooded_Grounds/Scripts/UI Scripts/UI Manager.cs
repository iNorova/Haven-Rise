using UnityEngine;
using UnityEngine.UI; // For UI elements
using UnityEngine.Events;
using System;

public class UIManager : MonoBehaviour, IDamageable
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

    [Header("Hunger System")]
    public Slider hungerSlider;               // Reference to hunger UI slider
    public Image hungerFillImage;             // Reference to the hunger slider's fill image
    public Color hungerColor = new Color(1f, 0.65f, 0f);          // Orange (#FFA500)
    public float maxHunger = 100f;            // Maximum hunger value
    public float cookedSteakHungerRestore = 50f; // How much hunger is restored when eating cooked steak
    [Tooltip("Drag the cooked steak prefab/asset here. If not assigned, will use item name matching.")]
    public GameObject cookedSteakPrefab;      // Reference to the cooked steak prefab (drag from project)
    [Tooltip("Name of the cooked steak item (used as fallback if prefab is not assigned).")]
    public string cookedSteakItemName = "Cooked Steak"; // Name of the cooked steak item (fallback)
    private float currentHunger;              // Current hunger value
    private float hungerActionTimer = 0f;      // Timer for action-based hunger decrease
    private float thirstActionTimer = 0f;      // Timer for action-based thirst decrease

    [Header("Thirst System")]
    public Slider thirstSlider;               // Reference to thirst UI slider
    public Image thirstFillImage;             // Reference to the thirst slider's fill image
    public Color thirstColor = new Color(0f, 0.5f, 1f);          // Blue (#0080FF)
    public float maxThirst = 100f;            // Maximum thirst value
    public float waterDrinkRestore = 50f;     // How much thirst is restored when drinking water
    [Tooltip("Drag the water plane GameObject here. Player can drink by pressing G when near it.")]
    public GameObject waterPlane;              // Reference to the water plane (drag from scene)
    [Tooltip("How close player needs to be to water to drink.")]
    public float waterDrinkRange = 3f;        // Distance from water to allow drinking
    [Tooltip("Cooldown time in seconds before player can drink water again (5 minutes = 300 seconds).")]
    public float waterDrinkCooldown = 300f;   // 5 minutes cooldown
    private float currentThirst;              // Current thirst value
    private float lastWaterDrinkTime = -999f;  // Time when player last drank water

    [Header("Hunger/Thirst Audio")]
    [Tooltip("Audio clip that plays when pressing G to eat food. Drag your MP3 audio file here.")]
    public AudioClip hungerAudioClip; // Audio clip for hunger actions (eating)
    [Tooltip("Audio clip that plays when pressing G to drink water. Drag your MP3 audio file here.")]
    public AudioClip thirstAudioClip; // Audio clip for thirst actions (drinking)
    private AudioSource audioSource; // Internal AudioSource component for playing clips

    [Header("Action-Based Hunger/Thirst Costs")]
    [Tooltip("Hunger lost per axe swing.")]
    public float hungerCostAxeSwing = 2f;
    [Tooltip("Thirst lost per axe swing.")]
    public float thirstCostAxeSwing = 3f;
    [Tooltip("Hunger lost per rock swing.")]
    public float hungerCostRockSwing = 1.5f;
    [Tooltip("Thirst lost per rock swing.")]
    public float thirstCostRockSwing = 2f;
    [Tooltip("Hunger lost per second while sprinting.")]
    public float hungerCostSprinting = 1f;
    [Tooltip("Thirst lost per second while sprinting.")]
    public float thirstCostSprinting = 2f;
    [Tooltip("Hunger lost per second while walking.")]
    public float hungerCostWalking = 0.3f;
    [Tooltip("Thirst lost per second while walking.")]
    public float thirstCostWalking = 0.5f;

    private float currentTemperature;
    private float permanentTemperatureIncrease = 0f;  // Tracks permanent temperature increase from deforestation
    private bool isTemperatureIncreasing = false; // Flag to control temperature increase
    private float temperatureIncreaseTimer = 0f;  // Timer for temperature increase duration
    private bool isDead = false;  // Flag to prevent multiple death triggers and further damage

    // Static instance for global access
    public static UIManager Instance { get; private set; }
    public static event Action OnPlayerDeath;

    // Cached references for optimization
    private CharController_Motor cachedPlayerMotor;
    private HotbarManager cachedHotbarManager;
    private InventoryManager cachedInventoryManager;
    private GameObject cachedPlayer;
    private float playerMotorCheckTimer = 0f;
    private const float PLAYER_MOTOR_CHECK_INTERVAL = 1f; // Check every second instead of every frame

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
        
        // Get or create AudioSource component for playing audio clips
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
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

        // Initialize hunger system
        currentHunger = maxHunger;
        if (hungerSlider != null)
        {
            hungerSlider.maxValue = maxHunger;
            hungerSlider.minValue = 0f;
            hungerSlider.value = currentHunger;
            if (hungerFillImage != null)
            {
                hungerFillImage.fillAmount = 1f;
                hungerFillImage.color = hungerColor;
            }
            Debug.Log($"Hunger slider initialized - Max: {maxHunger}, Current: {currentHunger}");
        }
        else
        {
            Debug.LogError("Hunger Slider is not assigned in the inspector!");
        }

        // Initialize thirst system
        currentThirst = maxThirst;
        if (thirstSlider != null)
        {
            thirstSlider.maxValue = maxThirst;
            thirstSlider.minValue = 0f;
            thirstSlider.value = currentThirst;
            if (thirstFillImage != null)
            {
                thirstFillImage.fillAmount = 1f;
                thirstFillImage.color = thirstColor;
            }
            Debug.Log($"Thirst slider initialized - Max: {maxThirst}, Current: {currentThirst}");
        }
        else
        {
            Debug.LogError("Thirst Slider is not assigned in the inspector!");
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
            
            // Check if player is sleeping in a bed - if so, let BedInteraction handle it
            if (BedInteraction.IsAnyBedActive())
            {
                // Player is sleeping, let BedInteraction handle waking up
                // Don't open pause menu yet - wait for next ESC press
                return;
            }
            
            // Check if bed was just exited via ESC this frame - if so, don't open pause menu yet
            if (BedInteraction.WasBedJustExitedViaEsc())
            {
                // Bed was just exited, skip opening pause menu - wait for next ESC press
                return;
            }
            
            // Only toggle pause menu if inventory/bed is not active and wasn't just exited
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

            // Update action-based hunger/thirst (from movement)
            UpdateActionBasedHungerThirst();

            // Periodically refresh cached references (every second)
            playerMotorCheckTimer += Time.deltaTime;
            if (playerMotorCheckTimer >= PLAYER_MOTOR_CHECK_INTERVAL)
            {
                RefreshCachedReferences();
                playerMotorCheckTimer = 0f;
            }

            // Check for food consumption (G key)
            if (Input.GetKeyDown(KeyCode.G))
            {
                // Try to drink water first (if near water)
                // Audio will be played inside TryDrinkWater/DrinkWater if cooldown allows
                if (TryDrinkWater())
                {
                    // Audio is handled inside DrinkWater() after cooldown check
                }
                else
                {
                    // If not near water, try to consume food
                    if (TryConsumeFood())
                    {
                        // Play hunger audio sound when eating food
                        if (hungerAudioClip != null && audioSource != null)
                        {
                            audioSource.PlayOneShot(hungerAudioClip);
                        }
                    }
                }
            }
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
        // Use cached player reference
        if (cachedPlayer == null)
        {
            cachedPlayer = GameObject.FindGameObjectWithTag("Player");
        }

        GameObject player = cachedPlayer;
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
        // Use cached player reference
        if (cachedPlayer == null)
        {
            cachedPlayer = GameObject.FindGameObjectWithTag("Player");
        }

        GameObject player = cachedPlayer;
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

            // Re-enable any movement scripts (cache GetComponents result)
            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            int scriptCount = scripts.Length;
            for (int i = 0; i < scriptCount; i++)
            {
                MonoBehaviour script = scripts[i];
                if (script != null)
                {
                    string typeName = script.GetType().Name.ToLower();
                    if (typeName.Contains("movement") || typeName.Contains("controller") || typeName.Contains("input"))
                    {
                        script.enabled = true;
                    }
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
        }

        // Update stamina UI only if slider exists
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
            // Removed debug log - was looping every frame
        }
        // Check if temperature is in danger zone
        else if (currentTemp >= dangerThreshold)
        {
            regenRate -= dangerTempStaminaDebuff;
            // Removed debug log - was looping every frame
        }
        // Removed debug log for safe zone - was looping every frame
        
        // Ensure regeneration rate doesn't go below 1
        return Mathf.Max(1f, regenRate);
    }

    public void UseStamina()
    {
        isUsingStamina = true;
        currentStamina = Mathf.Max(currentStamina - staminaUseRate * Time.deltaTime, 0f);
        // Removed debug log - was looping every frame
    }

    public void StopUsingStamina()
    {
        isUsingStamina = false;
        // Removed debug log - was looping every frame
    }

    private void TakeHealthDamage()
    {
        ApplyDamage(healthDamageRate);
    }

    /// <summary>
    /// Implements IDamageable interface. Called by enemies (like Ghoul Zombie) to damage the player.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        // Don't take damage if already dead or paused
        if (isDead || isPaused)
        {
            return;
        }

        currentHealth -= Mathf.Abs(amount);
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

        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}");

        // Check if player died
        if (currentHealth <= 0f && !isDead)
        {
            HandlePlayerDeath();
        }
    }

    private void HandlePlayerDeath()
    {
        // Prevent multiple death triggers
        if (isDead)
        {
            Debug.LogWarning("UIManager: HandlePlayerDeath called but player is already dead!");
            return;
        }
        
        isDead = true;
        Debug.Log("UIManager: Player died! Triggering death event.");
        Debug.Log($"UIManager: Current health: {currentHealth}");
        
        // Check if event has subscribers
        if (OnPlayerDeath == null)
        {
            Debug.LogError("UIManager: OnPlayerDeath event has NO subscribers! Death screen will not show!");
        }
        else
        {
            int subscriberCount = OnPlayerDeath.GetInvocationList().Length;
            Debug.Log($"UIManager: OnPlayerDeath event has {subscriberCount} subscriber(s). Invoking event...");
        }
        
        // Broadcast death to listeners (e.g., RespawnManager, DeathScreenUI)
        OnPlayerDeath?.Invoke();
        
        Debug.Log("UIManager: Death event invocation completed.");
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

    private void RefreshCachedReferences()
    {
        // Refresh cached references periodically
        if (cachedPlayerMotor == null)
        {
            cachedPlayerMotor = FindObjectOfType<CharController_Motor>();
        }
        if (cachedHotbarManager == null)
        {
            cachedHotbarManager = FindFirstObjectByType<HotbarManager>();
        }
        if (cachedInventoryManager == null)
        {
            cachedInventoryManager = FindFirstObjectByType<InventoryManager>();
        }
        if (cachedPlayer == null)
        {
            cachedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void UpdateActionBasedHungerThirst()
    {
        if (isPaused) return;

        // Use cached reference
        if (cachedPlayerMotor == null)
        {
            cachedPlayerMotor = FindObjectOfType<CharController_Motor>();
            if (cachedPlayerMotor == null) return;
        }

        CharController_Motor playerMotor = cachedPlayerMotor;
        if (playerMotor != null)
        {
            // Check if player is sprinting
            if (playerMotor.IsSprinting())
            {
                // Decrease hunger and thirst while sprinting
                hungerActionTimer += Time.deltaTime;
                thirstActionTimer += Time.deltaTime;
                
                if (hungerActionTimer >= 1f) // Every second
                {
                    DecreaseHunger(hungerCostSprinting);
                    hungerActionTimer = 0f;
                }
                
                if (thirstActionTimer >= 1f) // Every second
                {
                    DecreaseThirst(thirstCostSprinting);
                    thirstActionTimer = 0f;
                }
            }
            // Check if player is walking
            else if (playerMotor.IsWalking())
            {
                hungerActionTimer += Time.deltaTime;
                thirstActionTimer += Time.deltaTime;
                
                if (hungerActionTimer >= 1f) // Every second
                {
                    DecreaseHunger(hungerCostWalking);
                    hungerActionTimer = 0f;
                }
                
                if (thirstActionTimer >= 1f) // Every second
                {
                    DecreaseThirst(thirstCostWalking);
                    thirstActionTimer = 0f;
                }
            }
        }
    }

    /// <summary>
    /// Called when player swings an axe. Decreases hunger and thirst.
    /// </summary>
    public void OnAxeSwing()
    {
        DecreaseHunger(hungerCostAxeSwing);
        DecreaseThirst(thirstCostAxeSwing);
        Debug.Log($"Axe swing! Hunger: -{hungerCostAxeSwing}, Thirst: -{thirstCostAxeSwing}");
    }

    /// <summary>
    /// Called when player swings a rock. Decreases hunger and thirst.
    /// </summary>
    public void OnRockSwing()
    {
        DecreaseHunger(hungerCostRockSwing);
        DecreaseThirst(thirstCostRockSwing);
        Debug.Log($"Rock swing! Hunger: -{hungerCostRockSwing}, Thirst: -{thirstCostRockSwing}");
    }

    private void DecreaseHunger(float amount)
    {
        currentHunger = Mathf.Max(currentHunger - amount, 0f);
        UpdateHungerUI();
    }

    private void DecreaseThirst(float amount)
    {
        currentThirst = Mathf.Max(currentThirst - amount, 0f);
        UpdateThirstUI();
    }

    private void UpdateHungerUI()
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger;
            if (hungerFillImage != null)
            {
                hungerFillImage.fillAmount = currentHunger / maxHunger;
                hungerFillImage.color = hungerColor;
            }
        }
    }

    private void UpdateThirstUI()
    {
        if (thirstSlider != null)
        {
            thirstSlider.value = currentThirst;
            if (thirstFillImage != null)
            {
                thirstFillImage.fillAmount = currentThirst / maxThirst;
                thirstFillImage.color = thirstColor;
            }
        }
    }

    private bool TryConsumeFood()
    {
        // Check if player has cooked steak in inventory/hotbar
        if (HasCookedSteak())
        {
            ConsumeCookedSteak();
            return true;
        }
        return false;
    }

    private bool HasCookedSteak()
    {
        // Use cached reference
        if (cachedHotbarManager == null)
        {
            cachedHotbarManager = FindFirstObjectByType<HotbarManager>();
        }

        HotbarManager hotbarManager = cachedHotbarManager;
        if (hotbarManager != null)
        {
            // Only check selected slot (what player is holding) - steak must be held to consume
            GameObject selectedItem = hotbarManager.GetItem(hotbarManager.selectedSlot);
            if (IsCookedSteak(selectedItem))
            {
                return true;
            }
        }

        // Player must be holding the steak to consume it, so we don't check other slots
        return false;
    }

    private bool IsCookedSteak(GameObject item)
    {
        if (item == null) return false;
        
        // First check: Compare with prefab reference (most reliable)
        if (cookedSteakPrefab != null)
        {
            // Check if item is the same prefab (by comparing names without Clone)
            string itemName = item.name.Replace("(Clone)", "").Trim();
            string prefabName = cookedSteakPrefab.name.Replace("(Clone)", "").Trim();
            
            if (itemName.Equals(prefabName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Also check ItemIconProvider if both have it
            ItemIconProvider itemProvider = item.GetComponent<ItemIconProvider>();
            ItemIconProvider prefabProvider = cookedSteakPrefab.GetComponent<ItemIconProvider>();
            
            if (itemProvider != null && prefabProvider != null)
            {
                if (!string.IsNullOrEmpty(itemProvider.itemName) && 
                    !string.IsNullOrEmpty(prefabProvider.itemName))
                {
                    if (itemProvider.itemName.Equals(prefabProvider.itemName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        
        // Fallback: Check by name (if prefab not assigned)
        if (cookedSteakPrefab == null)
        {
            ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
            if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
            {
                return iconProvider.itemName.Equals(cookedSteakItemName, System.StringComparison.OrdinalIgnoreCase);
            }
            
            string itemName = item.name.Replace("(Clone)", "").Trim();
            return itemName.Equals(cookedSteakItemName, System.StringComparison.OrdinalIgnoreCase) || 
                   itemName.Contains(cookedSteakItemName);
        }
        
        return false;
    }

    private void ConsumeCookedSteak()
    {
        // Restore hunger
        currentHunger = Mathf.Min(currentHunger + cookedSteakHungerRestore, maxHunger);
        
        // Update hunger UI immediately
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger;
            if (hungerFillImage != null)
            {
                hungerFillImage.fillAmount = currentHunger / maxHunger;
                hungerFillImage.color = hungerColor;
            }
        }

        Debug.Log($"Consumed cooked steak! Hunger restored by {cookedSteakHungerRestore}. Current hunger: {currentHunger}");

        // Remove cooked steak from inventory/hotbar
        RemoveCookedSteakFromInventory();
    }

    private void RemoveCookedSteakFromInventory()
    {
        // Use cached reference
        if (cachedHotbarManager == null)
        {
            cachedHotbarManager = FindFirstObjectByType<HotbarManager>();
        }

        HotbarManager hotbarManager = cachedHotbarManager;
        if (hotbarManager != null)
        {
            // Check selected slot first
            int selectedSlot = hotbarManager.selectedSlot;
            GameObject selectedItem = hotbarManager.GetItem(selectedSlot);
            if (IsCookedSteak(selectedItem))
            {
                InventorySlot slot = hotbarManager.hotbarSlots[selectedSlot];
                int stackCount = slot.GetStackCount();
                
                if (stackCount > 1)
                {
                    // Decrement stack
                    slot.SetStackCount(stackCount - 1);
                }
                else
                {
                    // Remove item completely
                    hotbarManager.SetItem(selectedSlot, null);
                    if (selectedItem != null)
                    {
                        Destroy(selectedItem);
                    }
                }
                hotbarManager.UpdateHotbarUI();
                Debug.Log("Consumed cooked steak from hotbar selected slot.");
                return;
            }
        }

        // If we reach here, the steak wasn't in the selected slot
        // Since steak must be held to consume, we don't check other slots
        Debug.LogWarning("Cooked steak not found in selected slot. Cannot consume.");
    }

    /// <summary>
    /// Manually restore hunger (for testing or other systems).
    /// </summary>
    public void RestoreHunger(float amount)
    {
        currentHunger = Mathf.Min(currentHunger + amount, maxHunger);
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger;
            if (hungerFillImage != null)
            {
                hungerFillImage.fillAmount = currentHunger / maxHunger;
                hungerFillImage.color = hungerColor;
            }
        }
    }

    /// <summary>
    /// Restore hunger to full.
    /// </summary>
    public void RestoreFullHunger()
    {
        currentHunger = maxHunger;
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger;
            if (hungerFillImage != null)
            {
                hungerFillImage.fillAmount = 1f;
                hungerFillImage.color = hungerColor;
            }
        }
    }

    /// <summary>
    /// Get current hunger value.
    /// </summary>
    public float GetCurrentHunger()
    {
        return currentHunger;
    }


    private bool TryDrinkWater()
    {
        if (waterPlane == null)
        {
            Debug.LogWarning("UIManager: Water plane is not assigned! Cannot drink water.");
            return false;
        }

        // Use cached player reference
        if (cachedPlayer == null)
        {
            cachedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (cachedPlayer == null)
            {
                if (cachedPlayerMotor != null)
                {
                    cachedPlayer = cachedPlayerMotor.gameObject;
                }
                else
                {
                    CharController_Motor motor = FindObjectOfType<CharController_Motor>();
                    if (motor != null)
                    {
                        cachedPlayer = motor.gameObject;
                        cachedPlayerMotor = motor;
                    }
                }
            }
        }

        GameObject player = cachedPlayer;
        if (player == null)
        {
            Debug.LogWarning("UIManager: Player not found! Cannot drink water.");
            return false;
        }

        // Calculate distance to water plane
        float distanceToWater = GetDistanceToWaterPlane(player.transform.position);
        
        Debug.Log($"UIManager: Distance to water: {distanceToWater:F2}, Drink range: {waterDrinkRange}, Water plane: {(waterPlane != null ? waterPlane.name : "NULL")}");
        
        if (distanceToWater <= waterDrinkRange)
        {
            Debug.Log("UIManager: Player is near water! Drinking...");
            DrinkWater();
            return true;
        }
        else
        {
            Debug.Log($"UIManager: Player is too far from water. Distance: {distanceToWater:F2}, Required: {waterDrinkRange}");
        }

        return false;
    }

    private float GetDistanceToWaterPlane(Vector3 position)
    {
        if (waterPlane == null)
        {
            return float.MaxValue;
        }

        // Get water plane's position
        Vector3 waterPosition = waterPlane.transform.position;
        
        // For a flat water plane, we primarily check vertical distance (Y position)
        // This works well for large flat water planes
        float verticalDistance = Mathf.Abs(position.y - waterPosition.y);
        
        // Optional: Check horizontal bounds if water plane has a collider
        Collider waterCollider = waterPlane.GetComponent<Collider>();
        if (waterCollider != null)
        {
            Bounds waterBounds = waterCollider.bounds;
            
            // Check if player is within horizontal bounds (X and Z)
            bool withinHorizontalBounds = position.x >= waterBounds.min.x && position.x <= waterBounds.max.x &&
                                         position.z >= waterBounds.min.z && position.z <= waterBounds.max.z;
            
            if (!withinHorizontalBounds)
            {
                // Player is outside water bounds - calculate distance to nearest edge
                Vector3 closestPoint = waterBounds.ClosestPoint(position);
                closestPoint.y = waterPosition.y; // Use water's Y level
                float horizontalDistance = Vector3.Distance(new Vector3(position.x, 0, position.z), 
                                                           new Vector3(closestPoint.x, 0, closestPoint.z));
                // Return combined distance (prioritize vertical, but include horizontal)
                return Mathf.Max(verticalDistance, horizontalDistance);
            }
        }
        
        // If within bounds or no collider, just return vertical distance
        return verticalDistance;
    }

    private void DrinkWater()
    {
        // Check cooldown
        float timeSinceLastDrink = Time.time - lastWaterDrinkTime;
        if (lastWaterDrinkTime > 0 && timeSinceLastDrink < waterDrinkCooldown)
        {
            float remainingCooldown = waterDrinkCooldown - timeSinceLastDrink;
            float remainingMinutes = remainingCooldown / 60f;
            Debug.Log($"Cannot drink water yet! Cooldown remaining: {remainingMinutes:F1} minutes ({remainingCooldown:F0} seconds)");
            return; // Return early if on cooldown - audio won't play
        }

        // Restore thirst
        currentThirst = Mathf.Min(currentThirst + waterDrinkRestore, maxThirst);
        
        // Update cooldown timer
        lastWaterDrinkTime = Time.time;
        
        // Update thirst UI immediately
        UpdateThirstUI();

        // Play thirst audio sound only when water is actually consumed (after cooldown check)
        if (thirstAudioClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(thirstAudioClip);
        }

        Debug.Log($"Drank water! Thirst restored by {waterDrinkRestore}. Current thirst: {currentThirst}. Cooldown: {waterDrinkCooldown} seconds.");
    }

    /// <summary>
    /// Manually restore thirst (for testing or other systems).
    /// </summary>
    public void RestoreThirst(float amount)
    {
        currentThirst = Mathf.Min(currentThirst + amount, maxThirst);
        if (thirstSlider != null)
        {
            thirstSlider.value = currentThirst;
            if (thirstFillImage != null)
            {
                thirstFillImage.fillAmount = currentThirst / maxThirst;
                thirstFillImage.color = thirstColor;
            }
        }
    }

    /// <summary>
    /// Restore thirst to full.
    /// </summary>
    public void RestoreFullThirst()
    {
        currentThirst = maxThirst;
        if (thirstSlider != null)
        {
            thirstSlider.value = currentThirst;
            if (thirstFillImage != null)
            {
                thirstFillImage.fillAmount = 1f;
                thirstFillImage.color = thirstColor;
            }
        }
    }

    /// <summary>
    /// Get current thirst value.
    /// </summary>
    public float GetCurrentThirst()
    {
        return currentThirst;
    }

    // Convenience method to reset all gameplay stats on respawn
    public void ResetAllStats()
    {
        isDead = false;  // Reset death flag so player can die again after respawn
        RestoreFullHealth();
        RestoreFullStamina();
        RestoreFullHunger();
        RestoreFullThirst();
        ResetTemperature();
    }
}
