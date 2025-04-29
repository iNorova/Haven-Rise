using UnityEngine;
using UnityEngine.UI; // For UI elements

public class UIManager : MonoBehaviour
{
    [Header("Pause Menu")]
    public Button pauseButton;        // Reference to pause button
    public RawImage pauseMenuImage;   // Reference to the pause menu background
    private bool isPaused = false;    // Track pause state

    [Header("Climate System")]
    public Slider climateSlider;              // Reference to climate UI slider
    public float maxClimateValue = 100f;      // Maximum climate value
    public float minClimateValue = 0f;        // Minimum climate value
    public float climateDecreaseRate = 5f;    // How fast climate decreases per second when conditions are bad
    public float climateIncreaseRate = 2f;    // How fast climate recovers when conditions are good
    
    [Header("Climate Thresholds")]
    public float dangerThreshold = 25f;       // Climate level where danger effects start
    public float criticalThreshold = 10f;     // Climate level where critical effects start
    
    private float currentClimateValue;
    private bool isClimateDecreasing = false; // Flag to control climate decrease

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
        
        // Initialize climate system
        currentClimateValue = maxClimateValue;
        if (climateSlider != null)
        {
            climateSlider.maxValue = maxClimateValue;
            climateSlider.minValue = minClimateValue;
            climateSlider.value = currentClimateValue;
        }
        else
        {
            Debug.LogError("Climate Slider is not assigned in the inspector!");
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

        // Update climate system
        UpdateClimateSystem();
    }

    private void UpdateClimateSystem()
    {
        if (!isPaused)
        {
            if (isClimateDecreasing)
            {
                // Decrease climate value
                currentClimateValue -= climateDecreaseRate * Time.deltaTime;
            }
            else
            {
                // Slowly recover climate value
                currentClimateValue += climateIncreaseRate * Time.deltaTime;
            }

            // Clamp the value between min and max
            currentClimateValue = Mathf.Clamp(currentClimateValue, minClimateValue, maxClimateValue);

            // Update the slider
            if (climateSlider != null)
            {
                climateSlider.value = currentClimateValue;
            }

            // Check for climate thresholds and trigger effects
            CheckClimateEffects();
        }
    }

    private void CheckClimateEffects()
    {
        if (currentClimateValue <= criticalThreshold)
        {
            ApplyCriticalClimateEffects();
        }
        else if (currentClimateValue <= dangerThreshold)
        {
            ApplyDangerClimateEffects();
        }
    }

    private void ApplyDangerClimateEffects()
    {
        // TODO: Implement danger level effects
        // Examples:
        // - Change skybox color
        // - Add screen effects
        // - Slow down player
        Debug.Log("Danger climate level reached!");
    }

    private void ApplyCriticalClimateEffects()
    {
        // TODO: Implement critical level effects
        // Examples:
        // - Damage player
        // - Extreme weather effects
        // - Screen distortion
        Debug.Log("Critical climate level reached!");
    }

    // Public methods to control climate state
    public void StartClimateDecrease()
    {
        isClimateDecreasing = true;
    }

    public void StopClimateDecrease()
    {
        isClimateDecreasing = false;
    }

    public float GetCurrentClimateValue()
    {
        return currentClimateValue;
    }

    public bool IsInDangerZone()
    {
        return currentClimateValue <= dangerThreshold;
    }

    public bool IsInCriticalZone()
    {
        return currentClimateValue <= criticalThreshold;
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
