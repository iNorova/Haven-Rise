using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class DeathScreenUI : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup panelCanvasGroup; // Assign a panel with CanvasGroup
    public Button respawnNearButton;     // Button to respawn near death
    public Button respawnStartButton;    // Button to respawn at start

    [Header("Behavior")]
    public float fadeDuration = 0.5f;

    private RespawnManager respawnManager;
    private bool isShowing;
    private bool prevCursorVisible;
    private CursorLockMode prevCursorLock;
    private TemperatureVisualEffects[] temperatureOverlays;
    private Coroutine cursorUnlockCoroutine;
    private MonoBehaviour inventoryUIManager;
    private bool wasInventoryUIManagerEnabled;

    private void Awake()
    {
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = GetComponent<CanvasGroup>();
        }
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Start()
    {
        respawnManager = FindObjectOfType<RespawnManager>();
        temperatureOverlays = FindObjectsOfType<TemperatureVisualEffects>(true);
        
        // Find InventoryUIManager to disable it when death screen is shown
        // Use reflection to find it since we can't directly reference the type
        MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (var behaviour in allBehaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "InventoryUIManager")
            {
                inventoryUIManager = behaviour;
                wasInventoryUIManagerEnabled = behaviour.enabled;
                Debug.Log("DeathScreenUI: Found InventoryUIManager");
                break;
            }
        }
        
        if (respawnNearButton != null)
        {
            respawnNearButton.onClick.AddListener(OnRespawnNearClicked);
        }
        if (respawnStartButton != null)
        {
            respawnStartButton.onClick.AddListener(OnRespawnStartClicked);
        }
        
        // Ensure we're subscribed (backup in case OnEnable wasn't called or GameObject was disabled)
        SubscribeToDeathEvent();
        
        // Ensure this GameObject stays active so it can receive events
        gameObject.SetActive(true);
        Debug.Log($"DeathScreenUI: Start() - GameObject active: {gameObject.activeInHierarchy}, Enabled: {enabled}");
    }

    private void OnEnable()
    {
        SubscribeToDeathEvent();
        Debug.Log($"DeathScreenUI: OnEnable() - GameObject active: {gameObject.activeInHierarchy}, Enabled: {enabled}");
    }

    private void OnDisable()
    {
        // Only unsubscribe if we're actually being destroyed, not just disabled
        // This prevents unsubscribing when the GameObject is temporarily disabled
        UIManager.OnPlayerDeath -= Show;
        Debug.LogWarning($"DeathScreenUI: OnDisable() - Unsubscribed from OnPlayerDeath event. GameObject active: {gameObject.activeInHierarchy}");
    }
    
    private void SubscribeToDeathEvent()
    {
        // Remove any existing subscription first to avoid duplicates
        UIManager.OnPlayerDeath -= Show;
        // Then subscribe
        UIManager.OnPlayerDeath += Show;
        Debug.Log("DeathScreenUI: Subscribed to OnPlayerDeath event");
    }
    
    private void OnDestroy()
    {
        // Clean up subscription when destroyed
        UIManager.OnPlayerDeath -= Show;
        Debug.Log("DeathScreenUI: OnDestroy() - Unsubscribed from OnPlayerDeath event");
    }

    private void Show()
    {
        Debug.Log("DeathScreenUI: Show() called!");
        if (isShowing)
        {
            Debug.LogWarning("DeathScreenUI: Show() called but already showing!");
            return;
        }
        isShowing = true;
        // Save and then unlock/show cursor for UI interaction
        prevCursorVisible = Cursor.visible;
        prevCursorLock = Cursor.lockState;
        
        // Temporarily disable InventoryUIManager to prevent it from locking cursor
        if (inventoryUIManager != null)
        {
            wasInventoryUIManagerEnabled = inventoryUIManager.enabled;
            inventoryUIManager.enabled = false;
            Debug.Log("DeathScreenUI: Temporarily disabled InventoryUIManager to prevent cursor locking.");
        }
        else
        {
            // Try to find it again if we didn't find it in Start()
            MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
            foreach (var behaviour in allBehaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "InventoryUIManager")
                {
                    inventoryUIManager = behaviour;
                    wasInventoryUIManagerEnabled = behaviour.enabled;
                    inventoryUIManager.enabled = false;
                    Debug.Log("DeathScreenUI: Found and disabled InventoryUIManager to prevent cursor locking.");
                    break;
                }
            }
        }
        
        // Unlock cursor immediately
        UnlockCursorForDeathScreen();
        
        // Start continuous cursor unlock coroutine (other systems might try to lock it)
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
        }
        cursorUnlockCoroutine = StartCoroutine(ContinuousCursorUnlock());
        
        // Pause temperature overlays (stop blinking)
        if (temperatureOverlays != null)
        {
            for (int i = 0; i < temperatureOverlays.Length; i++)
            {
                if (temperatureOverlays[i] != null) temperatureOverlays[i].SetPaused(true);
            }
        }
        Debug.Log("DeathScreenUI: Starting fade in routine...");
        StartCoroutine(FadeInRoutine());
    }

    private void Hide()
    {
        if (!isShowing) return;
        
        // Stop cursor unlock coroutine
        if (cursorUnlockCoroutine != null)
        {
            StopCoroutine(cursorUnlockCoroutine);
            cursorUnlockCoroutine = null;
        }
        
        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        // Pause gameplay while death UI is visible
        Time.timeScale = 0f;
        float t = 0f;
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        float t = fadeDuration;
        if (panelCanvasGroup != null)
        {
            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }
        isShowing = false;
        // Resume gameplay after fade out
        Time.timeScale = 1f;
        
        // Restore InventoryUIManager
        if (inventoryUIManager != null)
        {
            inventoryUIManager.enabled = wasInventoryUIManagerEnabled;
            Debug.Log("DeathScreenUI: Re-enabled InventoryUIManager.");
        }
        
        // Restore cursor state
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevCursorLock;
        // Resume temperature overlays
        if (temperatureOverlays != null)
        {
            for (int i = 0; i < temperatureOverlays.Length; i++)
            {
                if (temperatureOverlays[i] != null) temperatureOverlays[i].SetPaused(false);
            }
        }
    }

    private void OnRespawnNearClicked()
    {
        if (respawnManager != null)
        {
            respawnManager.RespawnAtDeathSpot();
        }
        Hide();
    }

    private void OnRespawnStartClicked()
    {
        if (respawnManager != null)
        {
            respawnManager.RespawnAtStart();
        }
        Hide();
    }
    
    private void UnlockCursorForDeathScreen()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // If it's still locked, try Confined mode
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        
        // Force unlock again
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator ContinuousCursorUnlock()
    {
        while (isShowing)
        {
            UnlockCursorForDeathScreen();
            yield return null; // Wait one frame
            UnlockCursorForDeathScreen();
            yield return new WaitForEndOfFrame(); // Wait until end of frame
            UnlockCursorForDeathScreen();
        }
    }
}


