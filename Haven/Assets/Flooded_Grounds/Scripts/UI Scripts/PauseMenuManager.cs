using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;

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

	// Inventory save keys
	public const string SavedHotbarCountKey = "SavedHotbarCount";
	public const string SavedInventoryCountKey = "SavedInventoryCount";
	public const string SavedHotbarItemPrefix = "SavedHotbarItem_";
	public const string SavedInventoryItemPrefix = "SavedInventoryItem_";

	// Temperature save keys
	public const string SavedTemperatureKey = "SavedTemperature";
	public const string SavedPermanentTemperatureIncreaseKey = "SavedPermanentTemperatureIncrease";

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

		// Save inventory and hotbar items
		SaveInventoryData();

		// Save temperature state
		SaveTemperatureData();

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

	// Save inventory and hotbar data before quitting
	private void SaveInventoryData()
	{
		InventorySystem invSystem = InventorySystem.Instance;
		if (invSystem == null)
		{
			Debug.LogWarning("[PauseMenuManager] InventorySystem not found. Cannot save inventory data.");
			return;
		}

		// Save hotbar items
		if (invSystem.hotbarManager != null && invSystem.hotbarManager.hotbarSlots != null)
		{
			int hotbarCount = 0;
			for (int i = 0; i < invSystem.hotbarManager.hotbarSlots.Length; i++)
			{
				GameObject item = invSystem.hotbarManager.GetItem(i);
				string itemName = GetItemName(item);
				if (!string.IsNullOrEmpty(itemName))
				{
					PlayerPrefs.SetString(SavedHotbarItemPrefix + i, itemName);
					hotbarCount++;
				}
				else
				{
					PlayerPrefs.DeleteKey(SavedHotbarItemPrefix + i);
				}
			}
			PlayerPrefs.SetInt(SavedHotbarCountKey, hotbarCount);
		}

		// Save inventory items
		if (invSystem.inventoryManager != null && invSystem.inventoryManager.inventorySlots != null)
		{
			int inventoryCount = 0;
			for (int i = 0; i < invSystem.inventoryManager.inventorySlots.Length; i++)
			{
				GameObject item = invSystem.inventoryManager.GetItem(i);
				string itemName = GetItemName(item);
				if (!string.IsNullOrEmpty(itemName))
				{
					PlayerPrefs.SetString(SavedInventoryItemPrefix + i, itemName);
					inventoryCount++;
				}
				else
				{
					PlayerPrefs.DeleteKey(SavedInventoryItemPrefix + i);
				}
			}
			PlayerPrefs.SetInt(SavedInventoryCountKey, inventoryCount);
		}

		Debug.Log("[PauseMenuManager] Inventory data saved successfully.");
	}

	// Save temperature data before quitting
	private void SaveTemperatureData()
	{
		UIManager uiManager = UIManager.Instance;
		if (uiManager != null)
		{
			// Save current temperature and permanent increase
			// We'll use reflection or a public method to get these values
			float currentTemp = uiManager.GetCurrentTemperature();
			
			// Try to get permanent increase using reflection since it's private
			var permanentIncreaseField = typeof(UIManager).GetField("permanentTemperatureIncrease", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			
			float permanentIncrease = 0f;
			if (permanentIncreaseField != null)
			{
				permanentIncrease = (float)permanentIncreaseField.GetValue(uiManager);
			}
			else
			{
				// Fallback: calculate from current temp (may not be perfect but better than nothing)
				// If we can't access permanentIncrease, we'll just save the final temperature
				Debug.LogWarning("[PauseMenuManager] Could not access permanentTemperatureIncrease field. Saving final temperature only.");
			}

			// Calculate base temperature (current - permanent)
			float baseTemperature = currentTemp - permanentIncrease;

			PlayerPrefs.SetFloat(SavedTemperatureKey, baseTemperature);
			PlayerPrefs.SetFloat(SavedPermanentTemperatureIncreaseKey, permanentIncrease);
			
			Debug.Log($"[PauseMenuManager] Temperature data saved: Base={baseTemperature}, Permanent={permanentIncrease}, Final={currentTemp}");
		}
		else
		{
			Debug.LogWarning("[PauseMenuManager] UIManager not found. Cannot save temperature data.");
		}
	}

	// Public method to load temperature data (called when scene loads)
	public static void LoadTemperatureData()
	{
		UIManager uiManager = UIManager.Instance;
		if (uiManager == null)
		{
			Debug.LogWarning("[PauseMenuManager] UIManager not found. Cannot load temperature data.");
			return;
		}

		// Check if there's saved temperature data
		if (!PlayerPrefs.HasKey(SavedTemperatureKey))
		{
			Debug.Log("[PauseMenuManager] No saved temperature data found. Using default values.");
			return;
		}

		// Load temperature values
		float savedBaseTemp = PlayerPrefs.GetFloat(SavedTemperatureKey, 0f);
		float savedPermanentIncrease = PlayerPrefs.GetFloat(SavedPermanentTemperatureIncreaseKey, 0f);

		// Use reflection to set private fields
		var baseTempField = typeof(UIManager).GetField("currentTemperature", 
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		var permanentField = typeof(UIManager).GetField("permanentTemperatureIncrease", 
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

		if (baseTempField != null)
		{
			baseTempField.SetValue(uiManager, savedBaseTemp);
		}

		if (permanentField != null)
		{
			permanentField.SetValue(uiManager, savedPermanentIncrease);
		}

		// Update temperature slider and notify visual effects
		float finalTemperature = savedBaseTemp + savedPermanentIncrease;
		
		// Update slider if available
		var sliderField = typeof(UIManager).GetField("temperatureSlider", 
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (sliderField != null)
		{
			UnityEngine.UI.Slider slider = sliderField.GetValue(uiManager) as UnityEngine.UI.Slider;
			if (slider != null)
			{
				slider.value = finalTemperature;
			}
		}

		// Update fill color
		var fillImageField = typeof(UIManager).GetField("temperatureFillImage", 
			System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (fillImageField != null)
		{
			UnityEngine.UI.Image fillImage = fillImageField.GetValue(uiManager) as UnityEngine.UI.Image;
			if (fillImage != null)
			{
				var normalColorField = typeof(UIManager).GetField("normalTemperatureColor", 
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				var dangerColorField = typeof(UIManager).GetField("dangerTemperatureColor", 
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				var criticalColorField = typeof(UIManager).GetField("criticalTemperatureColor", 
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				var dangerThresholdField = typeof(UIManager).GetField("dangerThreshold", 
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
				var criticalThresholdField = typeof(UIManager).GetField("criticalThreshold", 
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

				float dangerThreshold = dangerThresholdField != null ? (float)dangerThresholdField.GetValue(uiManager) : 75f;
				float criticalThreshold = criticalThresholdField != null ? (float)criticalThresholdField.GetValue(uiManager) : 90f;

				if (finalTemperature >= criticalThreshold && criticalColorField != null)
				{
					fillImage.color = (Color)criticalColorField.GetValue(uiManager);
				}
				else if (finalTemperature >= dangerThreshold && dangerColorField != null)
				{
					fillImage.color = (Color)dangerColorField.GetValue(uiManager);
				}
				else if (normalColorField != null)
				{
					fillImage.color = (Color)normalColorField.GetValue(uiManager);
				}
			}
		}

		// Notify temperature changed to update visual effects
		var notifyMethod = typeof(UIManager).GetMethod("NotifyTemperatureChanged", 
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		if (notifyMethod != null)
		{
			notifyMethod.Invoke(uiManager, new object[] { finalTemperature });
		}
		else
		{
			// Fallback: manually notify visual effects
			var visualTargetsField = typeof(UIManager).GetField("temperatureVisualTargets", 
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (visualTargetsField != null)
			{
				TemperatureVisualEffects[] targets = visualTargetsField.GetValue(uiManager) as TemperatureVisualEffects[];
				if (targets != null)
				{
					foreach (var target in targets)
					{
						if (target != null)
						{
							target.SetTemperature(finalTemperature);
						}
					}
				}
			}

			// Also notify via UnityEvent
			var onTempChangedField = typeof(UIManager).GetField("onTemperatureChanged", 
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (onTempChangedField != null)
			{
				UnityEngine.Events.UnityEvent<float> onTempChanged = onTempChangedField.GetValue(uiManager) as UnityEngine.Events.UnityEvent<float>;
				onTempChanged?.Invoke(finalTemperature);
			}
		}

		Debug.Log($"[PauseMenuManager] Temperature data loaded: Base={savedBaseTemp}, Permanent={savedPermanentIncrease}, Final={finalTemperature}");
	}

	// Helper method to get item name from ItemIconProvider or GameObject name
	private string GetItemName(GameObject item)
	{
		if (item == null) return string.Empty;

		ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
		if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
		{
			return iconProvider.itemName;
		}

		// Fallback to GameObject name (remove "(Clone)" suffix if present)
		string itemName = item.name;
		if (itemName.Contains("(Clone)"))
		{
			itemName = itemName.Replace("(Clone)", "").Trim();
		}
		return itemName;
	}

	// Public method to load inventory data (called when scene loads)
	public static void LoadInventoryData()
	{
		InventorySystem invSystem = InventorySystem.Instance;
		if (invSystem == null)
		{
			Debug.LogWarning("[PauseMenuManager] InventorySystem not found. Cannot load inventory data.");
			return;
		}

		// Small delay to ensure scene is fully loaded
		MonoBehaviour coroutineRunner = invSystem;
		if (coroutineRunner != null)
		{
			coroutineRunner.StartCoroutine(LoadInventoryDataCoroutine(invSystem));
		}
	}

	// Coroutine to load inventory data after a frame delay
	private static System.Collections.IEnumerator LoadInventoryDataCoroutine(InventorySystem invSystem)
	{
		// Wait a frame to ensure all objects in scene are loaded
		yield return null;
		yield return null;

		// Collect all persisted items (items that were DontDestroyOnLoad from previous session)
		List<GameObject> availablePersistedItems = new List<GameObject>();
		GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
		foreach (GameObject obj in allObjects)
		{
			if ((obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad") && 
			    obj.CompareTag("Pickupable"))
			{
				availablePersistedItems.Add(obj);
			}
		}

		// Load hotbar items
		if (invSystem.hotbarManager != null && invSystem.hotbarManager.hotbarSlots != null)
		{
			for (int i = 0; i < invSystem.hotbarManager.hotbarSlots.Length; i++)
			{
				string savedItemName = PlayerPrefs.GetString(SavedHotbarItemPrefix + i, string.Empty);
				if (!string.IsNullOrEmpty(savedItemName))
				{
					// First try to find a persisted item with matching name (from available list)
					GameObject itemToRestore = null;
					for (int j = availablePersistedItems.Count - 1; j >= 0; j--)
					{
						GameObject item = availablePersistedItems[j];
						if (MatchesItemName(item, savedItemName))
						{
							itemToRestore = item;
							availablePersistedItems.RemoveAt(j);
							break;
						}
					}
					
					// If not found in persisted items, try to find in scene
					if (itemToRestore == null)
					{
						itemToRestore = FindItemInScene(savedItemName);
					}
					
					if (itemToRestore != null)
					{
						// Mark as persisted if not already
						if (itemToRestore.scene.name != null && itemToRestore.scene.name != "DontDestroyOnLoad")
						{
							DontDestroyOnLoad(itemToRestore);
						}

						// Pick up the item into the hotbar slot
						invSystem.hotbarManager.SetItem(i, itemToRestore);
						
						// Configure item for hotbar (similar to PickupItem)
						itemToRestore.transform.SetParent(invSystem.hotbarManager.handHolder);
						itemToRestore.transform.localPosition = Vector3.zero;
						itemToRestore.transform.localRotation = Quaternion.identity;

						Rigidbody rb = itemToRestore.GetComponent<Rigidbody>();
						if (rb != null)
						{
							rb.isKinematic = true;
							rb.useGravity = false;
						}

						Collider itemCollider = itemToRestore.GetComponent<Collider>();
						if (itemCollider != null)
						{
							itemCollider.enabled = false;
						}

						itemToRestore.SetActive(false);
						
						Debug.Log($"[PauseMenuManager] Restored hotbar item {savedItemName} to slot {i}");
					}
					else
					{
						Debug.LogWarning($"[PauseMenuManager] Could not find item '{savedItemName}' to restore to hotbar slot {i}");
					}
				}
			}
			
			// Select the current slot to ensure correct item activation
			if (invSystem.hotbarManager.selectedSlot >= 0 && invSystem.hotbarManager.selectedSlot < invSystem.hotbarManager.hotbarSlots.Length)
			{
				invSystem.hotbarManager.SelectSlot(invSystem.hotbarManager.selectedSlot);
			}
		}

		// Load inventory items
		if (invSystem.inventoryManager != null && invSystem.inventoryManager.inventorySlots != null)
		{
			for (int i = 0; i < invSystem.inventoryManager.inventorySlots.Length; i++)
			{
				string savedItemName = PlayerPrefs.GetString(SavedInventoryItemPrefix + i, string.Empty);
				if (!string.IsNullOrEmpty(savedItemName))
				{
					// First try to find a persisted item with matching name (from available list)
					GameObject itemToRestore = null;
					for (int j = availablePersistedItems.Count - 1; j >= 0; j--)
					{
						GameObject item = availablePersistedItems[j];
						if (MatchesItemName(item, savedItemName))
						{
							itemToRestore = item;
							availablePersistedItems.RemoveAt(j);
							break;
						}
					}
					
					// If not found in persisted items, try to find in scene
					if (itemToRestore == null)
					{
						itemToRestore = FindItemInScene(savedItemName);
					}
					
					if (itemToRestore != null)
					{
						// Mark as persisted if not already
						if (itemToRestore.scene.name != null && itemToRestore.scene.name != "DontDestroyOnLoad")
						{
							DontDestroyOnLoad(itemToRestore);
						}

						// Add item to inventory
						invSystem.inventoryManager.SetItem(i, itemToRestore);
						
						// Configure item for inventory storage
						if (invSystem.inventoryManager.hiddenItemsParent != null)
						{
							itemToRestore.transform.SetParent(invSystem.inventoryManager.hiddenItemsParent);
							itemToRestore.transform.localPosition = Vector3.zero;
							itemToRestore.transform.localRotation = Quaternion.identity;
						}

						Rigidbody rb = itemToRestore.GetComponent<Rigidbody>();
						if (rb != null)
						{
							rb.isKinematic = true;
							rb.useGravity = false;
						}

						Collider itemCollider = itemToRestore.GetComponent<Collider>();
						if (itemCollider != null)
						{
							itemCollider.enabled = false;
						}

						itemToRestore.SetActive(false);
						
						Debug.Log($"[PauseMenuManager] Restored inventory item {savedItemName} to slot {i}");
					}
					else
					{
						Debug.LogWarning($"[PauseMenuManager] Could not find item '{savedItemName}' to restore to inventory slot {i}");
					}
				}
			}
		}

		Debug.Log("[PauseMenuManager] Inventory data loaded successfully.");
	}

	// Helper method to check if an item matches a saved item name
	private static bool MatchesItemName(GameObject item, string itemName)
	{
		if (item == null || string.IsNullOrEmpty(itemName)) return false;

		ItemIconProvider iconProvider = item.GetComponent<ItemIconProvider>();
		if (iconProvider != null && !string.IsNullOrEmpty(iconProvider.itemName))
		{
			return iconProvider.itemName == itemName;
		}

		// Fallback to GameObject name (remove "(Clone)" if present)
		string objName = item.name;
		if (objName.Contains("(Clone)"))
		{
			objName = objName.Replace("(Clone)", "").Trim();
		}
		return objName == itemName;
	}


	// Helper method to find an item in the scene by name (checking ItemIconProvider.itemName)
	private static GameObject FindItemInScene(string itemName)
	{
		// Find all objects with "Pickupable" tag
		GameObject[] pickupableObjects = GameObject.FindGameObjectsWithTag("Pickupable");
		
		foreach (GameObject obj in pickupableObjects)
		{
			// Skip if it's a persisted item (DontDestroyOnLoad)
			if (obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad")
			{
				continue;
			}

			// Check if item is already in inventory or hotbar (skip if inactive)
			if (!obj.activeInHierarchy)
			{
				continue;
			}

			// Use MatchesItemName for consistency
			if (MatchesItemName(obj, itemName))
			{
				return obj;
			}
		}

		return null;
	}

    void OnDestroy()
    {
        saveAndMenuButton.onClick.RemoveListener(SaveAndGoToMenu);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        if (saveButton != null) saveButton.onClick.RemoveListener(SaveGameOnly);
        if (loadButton != null) loadButton.onClick.RemoveListener(LoadGameOnly);
    }
}
