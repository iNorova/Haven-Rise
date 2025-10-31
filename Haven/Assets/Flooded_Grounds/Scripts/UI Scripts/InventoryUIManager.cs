using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class InventoryUIManager : MonoBehaviour
{
    public GameObject inventoryPanel;
    public InventoryManager inventoryManager; // Reference to the new InventoryManager
    public CharController_Motor playerController; // Reference to the player controller script
    public HotbarManager hotbarManager; // Reference to the HotbarManager
    public GameObject craftingPanel; // New: optional crafting UI panel shown with inventory
    
    // Static flag to prevent pause menu from opening on same frame inventory closes via ESC
    private static bool inventoryJustClosedViaEsc = false;
    
    // Coroutine to enforce cursor lock after closing via ESC
    private Coroutine cursorLockCoroutine = null;
    
    // Flag to track if we closed via ESC (different handling needed)
    private bool closedViaEsc = false;

    void Start()
    {
        // Ensure cursor is locked and invisible at the start of the game
        LockCursor();

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
            closedViaEsc = false; // Not closing via ESC
            ToggleInventory();
        }
        
        // Handle ESC key to close inventory if it's open
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsInventoryOpen())
            {
                closedViaEsc = true; // Mark that we're closing via ESC
                CloseInventory();
                inventoryJustClosedViaEsc = true; // Mark that inventory was closed via ESC this frame
            }
        }
    }
    
    void FixedUpdate()
    {
        // Also enforce cursor lock in FixedUpdate for immediate effect
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            // Force cursor lock immediately in FixedUpdate too
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    void LateUpdate()
    {
        // Reset the flag at the end of each frame, after all Update methods have run
        inventoryJustClosedViaEsc = false;
        
        // CRITICAL: ALWAYS enforce cursor state when inventory is closed
        // This must run after ALL other scripts to override any conflicting changes
        // We enforce it every frame, not just when we detect an issue, to handle any interference
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            // Inventory is closed - ALWAYS lock cursor (don't check condition, just do it)
            // This ensures it works even if something else is interfering
            ForceLockCursorImmediately();
        }
    }
    
    void OnDisable()
    {
        // Clean up coroutine if component is disabled
        if (cursorLockCoroutine != null)
        {
            StopCoroutine(cursorLockCoroutine);
            cursorLockCoroutine = null;
        }
    }
    
    // Static method for other scripts to check if inventory was just closed via ESC
    public static bool WasInventoryJustClosedViaEsc()
    {
        return inventoryJustClosedViaEsc;
    }
    
    // Public method to check if inventory is open
    public bool IsInventoryOpen()
    {
        return inventoryPanel != null && inventoryPanel.activeSelf;
    }
    
    // Public method to toggle inventory
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);

            if (isActive)
            {
                OpenInventory();
            }
            else
            {
                CloseInventory();
            }
        }
        else
        {
            Debug.LogWarning("Inventory Panel is not assigned in the Inspector!");
        }
    }
    
    // Public method to open inventory
    public void OpenInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            
            // Show and unlock cursor when inventory is open
            UnlockCursor();

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
    }
    
    // Public method to close inventory
    public void CloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            
            // IMMEDIATELY clear UI focus and lock cursor BEFORE doing anything else
            // This must happen first to prevent cursor from staying visible
            ClearUIFocusImmediately();
            ForceLockCursorImmediately();
            
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
            
            // If closed via ESC, use special handling
            if (closedViaEsc)
            {
                // For ESC, we need more aggressive cursor locking
                // Disable all GraphicRaycasters temporarily to prevent UI interference
                StartCoroutine(CloseInventoryViaEscRoutine());
            }
            else
            {
                // Normal close (via I key)
                LockCursor();
            }
            
            // Start coroutine to enforce cursor lock at end of frame
            if (cursorLockCoroutine != null)
            {
                StopCoroutine(cursorLockCoroutine);
            }
            cursorLockCoroutine = StartCoroutine(EnforceCursorLockAtEndOfFrame());
        }
    }
    
    // Immediately clear all UI focus to prevent cursor from staying visible
    private void ClearUIFocusImmediately()
    {
        // Clear EventSystem selection
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // Clear any hover states by disabling and re-enabling EventSystem
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.enabled)
        {
            eventSystem.enabled = false;
            eventSystem.enabled = true;
        }
    }
    
    // Force cursor lock immediately without any delays
    private void ForceLockCursorImmediately()
    {
        // Set visibility FIRST - this is critical
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Set again immediately (Unity quirk)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Force it one more time for good measure
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    // Special routine for closing via ESC that handles UI interference
    private IEnumerator CloseInventoryViaEscRoutine()
    {
        // Disable all GraphicRaycasters to prevent UI from interfering
        GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();
        bool[] raycastersEnabled = new bool[raycasters.Length];
        
        for (int i = 0; i < raycasters.Length; i++)
        {
            if (raycasters[i] != null)
            {
                raycastersEnabled[i] = raycasters[i].enabled;
                raycasters[i].enabled = false;
            }
        }
        
        // Disable EventSystem temporarily
        EventSystem eventSystem = EventSystem.current;
        bool eventSystemWasEnabled = eventSystem != null && eventSystem.enabled;
        if (eventSystem != null)
        {
            eventSystem.enabled = false;
        }
        
        // Force cursor lock multiple times
        for (int i = 0; i < 3; i++)
        {
            LockCursor();
            yield return null; // Wait one frame
        }
        
        // Re-enable GraphicRaycasters
        for (int i = 0; i < raycasters.Length; i++)
        {
            if (raycasters[i] != null)
            {
                raycasters[i].enabled = raycastersEnabled[i];
            }
        }
        
        // Re-enable EventSystem
        if (eventSystem != null && eventSystemWasEnabled)
        {
            eventSystem.enabled = true;
        }
        
        // Final cursor lock
        LockCursor();
        
        closedViaEsc = false; // Reset flag
    }
    
    // Coroutine to enforce cursor lock at the end of the frame and next frame
    private IEnumerator EnforceCursorLockAtEndOfFrame()
    {
        // First, wait for end of current frame
        yield return new WaitForEndOfFrame();
        // Force lock cursor again after all frame updates are complete
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            LockCursor();
        }
        
        // Also enforce it again next frame to handle Unity's cursor lock quirks
        yield return null; // Wait one frame
        if (inventoryPanel != null && !inventoryPanel.activeSelf)
        {
            LockCursor();
        }
        
        cursorLockCoroutine = null;
    }
    
    // Helper method to lock cursor
    // Unity sometimes needs these set multiple times to work reliably
    private void LockCursor()
    {
        // Clear any UI selection that might interfere
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        // Set again to ensure it sticks (Unity quirk)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    // Helper method to unlock cursor
    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}