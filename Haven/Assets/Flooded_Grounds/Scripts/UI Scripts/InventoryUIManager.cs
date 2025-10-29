using UnityEngine;

public class InventoryUIManager : MonoBehaviour
{
    public GameObject inventoryPanel;
    public InventoryManager inventoryManager; // Reference to the new InventoryManager
    public CharController_Motor playerController; // Reference to the player controller script
    public HotbarManager hotbarManager; // Reference to the HotbarManager
    public GameObject craftingPanel; // New: optional crafting UI panel shown with inventory

    void Start()
    {
        // Ensure cursor is locked and invisible at the start of the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Ensure player input is active at start
        if (playerController != null)
        {
            playerController.SetInputActive(true);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryPanel != null)
            {
                bool isActive = !inventoryPanel.activeSelf;
                inventoryPanel.SetActive(isActive);

                if (isActive)
                {
                    // Show and unlock cursor when inventory is open
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    // Disable player movement and camera look
                    if (playerController != null)
                    {
                        playerController.SetInputActive(false);
                    }

                    // Disable hotbar item input when inventory is open
                    if (hotbarManager != null)
                    {
                        hotbarManager.SetInputActive(false);
                    }

                    // If inventory is being opened, update its UI
                    if (inventoryManager != null)
                    {
                        inventoryManager.UpdateInventoryUI();
                    }
                    // Explicitly update hotbar UI when inventory is opened
                    if (hotbarManager != null)
                    {
                        hotbarManager.UpdateHotbarUI();
                    }

                    // Show crafting panel alongside inventory if assigned
                    if (craftingPanel != null)
                    {
                        craftingPanel.SetActive(true);
                    }
                }
                else
                {
                    // Hide and lock cursor when inventory is closed
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    // Enable player movement and camera look
                    if (playerController != null)
                    {
                        playerController.SetInputActive(true);
                    }

                    // Enable hotbar item input when inventory is closed
                    if (hotbarManager != null)
                    {
                        hotbarManager.SetInputActive(true);
                    }
                    // Explicitly update hotbar UI when inventory is closed
                    if (hotbarManager != null)
                    {
                        hotbarManager.UpdateHotbarUI();
                    }

                    // Hide crafting panel with inventory
                    if (craftingPanel != null)
                    {
                        craftingPanel.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Inventory Panel is not assigned in the Inspector!");
            }
        }
    }
}