using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuPanel;
    public Button saveAndMenuButton;
    public Slider volumeSlider;

    [Header("Optional Buttons")]
    public Button saveButton;
    public Button loadButton;

    private bool isPaused = false;
    private static PauseMenuManager instance;

    // PlayerPrefs key used by Main Menu "Continue" to know which scene to load
    public const string LastSaveSceneKey = "LastSaveScene";

	// Additional keys to remember player's last transform
	public const string LastPlayerPosXKey = "LastPlayerPosX";
	public const string LastPlayerPosYKey = "LastPlayerPosY";
	public const string LastPlayerPosZKey = "LastPlayerPosZ";
	public const string LastPlayerRotXKey = "LastPlayerRotX";
	public const string LastPlayerRotYKey = "LastPlayerRotY";
	public const string LastPlayerRotZKey = "LastPlayerRotZ";
	public const string LastPlayerRotWKey = "LastPlayerRotW";

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        pauseMenuPanel.SetActive(false);

        saveAndMenuButton.onClick.AddListener(SaveAndGoToMenu);
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (saveButton != null) saveButton.onClick.AddListener(SaveGameOnly);
        if (loadButton != null) loadButton.onClick.AddListener(LoadGameOnly);
    }

    void Update()
    {
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
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;

        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null) motor.SetInputActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;

        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null) motor.SetInputActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SaveAndGoToMenu()
    {
		// Save scene name and player's current transform so Continue resumes where you left off
		var currentScene = SceneManager.GetActiveScene().name;
		PlayerPrefs.SetString(LastSaveSceneKey, currentScene);

		// Try to capture player transform (prefers CharController_Motor, falls back to tag "Player")
		Transform playerTransform = null;
		var motor = FindObjectOfType<CharController_Motor>();
		if (motor != null) playerTransform = motor.transform;
		if (playerTransform == null)
		{
			var playerGo = GameObject.FindGameObjectWithTag("Player");
			if (playerGo != null) playerTransform = playerGo.transform;
		}

		if (playerTransform != null)
		{
			var p = playerTransform.position;
			var r = playerTransform.rotation;
			PlayerPrefs.SetFloat(LastPlayerPosXKey, p.x);
			PlayerPrefs.SetFloat(LastPlayerPosYKey, p.y);
			PlayerPrefs.SetFloat(LastPlayerPosZKey, p.z);
			PlayerPrefs.SetFloat(LastPlayerRotXKey, r.x);
			PlayerPrefs.SetFloat(LastPlayerRotYKey, r.y);
			PlayerPrefs.SetFloat(LastPlayerRotZKey, r.z);
			PlayerPrefs.SetFloat(LastPlayerRotWKey, r.w);
		}

		PlayerPrefs.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene("MAIN MENU FINAL");
    }

    // Optional minimal save/load hooks if you wire buttons in gameplay directly
    public void SaveGameOnly()
    {
        var currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString(LastSaveSceneKey, currentScene);
        PlayerPrefs.Save();
    }

    public void LoadGameOnly()
    {
        var sceneToLoad = PlayerPrefs.GetString(LastSaveSceneKey, string.Empty);
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("No saved scene found. Save once from Pause Menu first.");
        }
    }

    // Static method for other scripts to check if pause menu is open
    public static bool IsPauseMenuOpen()
    {
        if (instance != null && instance.pauseMenuPanel != null)
        {
            return instance.isPaused && instance.pauseMenuPanel.activeSelf;
        }
        return false;
    }

    void OnDestroy()
    {
        saveAndMenuButton.onClick.RemoveListener(SaveAndGoToMenu);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (saveButton != null) saveButton.onClick.RemoveListener(SaveGameOnly);
        if (loadButton != null) loadButton.onClick.RemoveListener(LoadGameOnly);
    }
}
