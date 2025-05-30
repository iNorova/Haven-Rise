using UnityEngine;
using UnityEngine.UI; // For UI elements

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
    
    [Header("Temperature Thresholds")]
    public float dangerThreshold = 75f;       // Temperature level where danger effects start
    public float criticalThreshold = 90f;     // Temperature level where critical effects start
    
    private float currentTemperature;
    private bool isTemperatureIncreasing = false; // Flag to control temperature increase
    private float temperatureIncreaseTimer = 0f;  // Timer for temperature increase duration

    // Static instance for global access
    public static UIManager Instance { get; private set; }

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
    }

    // Update is called once per frame
    void Update()
    {
        // Handle pause menu input
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC key pressed");
            TogglePauseMenu();
        }

        // Update temperature system
        UpdateTemperatureSystem();
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
            else
            {
                // Slowly decrease temperature
                currentTemperature -= temperatureDecreaseRate * Time.deltaTime;
            }

            // Clamp the value between min and max
            currentTemperature = Mathf.Clamp(currentTemperature, minTemperatureValue, maxTemperatureValue);

            // Update the slider
            if (temperatureSlider != null)
            {
                temperatureSlider.value = currentTemperature;
                
                // Update fill color based on temperature
                if (temperatureFillImage != null)
                {
                    if (currentTemperature >= criticalThreshold)
                    {
                        temperatureFillImage.color = criticalTemperatureColor;
                    }
                    else if (currentTemperature >= dangerThreshold)
                    {
                        temperatureFillImage.color = dangerTemperatureColor;
                    }
                    else
                    {
                        temperatureFillImage.color = normalTemperatureColor;
                    }
                }
            }

            // Check for temperature thresholds and trigger effects
            CheckTemperatureEffects();
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
    }

    public void StopTemperatureIncrease()
    {
        isTemperatureIncreasing = false;
    }

    public float GetCurrentTemperature()
    {
        return currentTemperature;
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
}
