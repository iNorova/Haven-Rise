using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Continue Button")]
    public Button continueButton;
    
    [Header("Scene Settings")]
    [Tooltip("The scene to load when Continue is pressed. Drag your main game scene here.")]
    public UnityEngine.Object savedScene;
    
    [Tooltip("Alternative: type scene name manually if you prefer")]
    public string savedSceneName = "";
    
    void OnEnable()
    {
        // Refresh button state when menu becomes active
        UpdateContinueButton();
        Debug.Log("[MainMenu] Saved scene at menu open: " + PlayerPrefs.GetString(PauseMenuManager.LastSaveSceneKey, string.Empty));
    }

    void Start()
    {
        // Check if there's a saved game to enable/disable Continue button
        UpdateContinueButton();
    }
    
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync(1);
    }
    
    public void NewGame()
    {
        // Clear all saved player data to start fresh
        PlayerPrefs.DeleteKey(PauseMenuManager.LastSaveSceneKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerPosXKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerPosYKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerPosZKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerRotXKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerRotYKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerRotZKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.LastPlayerRotWKey);
        
        // Clear inventory and hotbar data
        PlayerPrefs.DeleteKey(PauseMenuManager.SavedHotbarCountKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.SavedInventoryCountKey);
        
        // Clear all hotbar and inventory item slots
        if (InventorySystem.Instance != null && InventorySystem.Instance.hotbarManager != null && InventorySystem.Instance.hotbarManager.hotbarSlots != null)
        {
            for (int i = 0; i < InventorySystem.Instance.hotbarManager.hotbarSlots.Length; i++)
            {
                PlayerPrefs.DeleteKey(PauseMenuManager.SavedHotbarItemPrefix + i);
            }
        }
        if (InventorySystem.Instance != null && InventorySystem.Instance.inventoryManager != null && InventorySystem.Instance.inventoryManager.inventorySlots != null)
        {
            for (int i = 0; i < InventorySystem.Instance.inventoryManager.inventorySlots.Length; i++)
            {
                PlayerPrefs.DeleteKey(PauseMenuManager.SavedInventoryItemPrefix + i);
            }
        }

        // Clear temperature data
        PlayerPrefs.DeleteKey(PauseMenuManager.SavedTemperatureKey);
        PlayerPrefs.DeleteKey(PauseMenuManager.SavedPermanentTemperatureIncreaseKey);
        
        PlayerPrefs.Save();
        
        Debug.Log("[MainMenu] Starting new game - cleared all saved data including inventory");
        
        // Load scene 1 like PlayGame() does (complete fresh start)
        SceneManager.LoadSceneAsync(1);
    }
    
    public void ContinueGame()
    {
        // Use the assigned scene from Inspector, or fall back to PlayerPrefs if empty
        string sceneToLoad = GetSceneToLoad();
        
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Time.timeScale = 1f; // Reset timescale in case game was paused when saved
            Debug.Log("[MainMenu] Continuing to scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("[MainMenu] No scene assigned in Inspector and no saved scene found. Drag a scene to 'Saved Scene' field or save once from Pause Menu first.");
        }
    }
    
    string GetSceneToLoad()
    {
        // Priority: 1) Dragged scene, 2) Typed scene name, 3) Saved scene from PlayerPrefs
        if (savedScene != null)
        {
            return savedScene.name;
        }
        else if (!string.IsNullOrEmpty(savedSceneName))
        {
            return savedSceneName;
        }
        else
        {
            return PlayerPrefs.GetString(PauseMenuManager.LastSaveSceneKey, string.Empty);
        }
    }
    
    void UpdateContinueButton()
    {
        // Enable/disable Continue button based on whether there's a saved game or assigned scene
        if (continueButton != null)
        {
            bool hasDraggedScene = savedScene != null;
            bool hasTypedScene = !string.IsNullOrEmpty(savedSceneName);
            bool hasSavedScene = !string.IsNullOrEmpty(PlayerPrefs.GetString(PauseMenuManager.LastSaveSceneKey, string.Empty));
            continueButton.interactable = hasDraggedScene || hasTypedScene || hasSavedScene;
        }
    }
}

