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
        if (respawnNearButton != null)
        {
            respawnNearButton.onClick.AddListener(OnRespawnNearClicked);
        }
        if (respawnStartButton != null)
        {
            respawnStartButton.onClick.AddListener(OnRespawnStartClicked);
        }
    }

    private void OnEnable()
    {
        UIManager.OnPlayerDeath += Show;
    }

    private void OnDisable()
    {
        UIManager.OnPlayerDeath -= Show;
    }

    private void Show()
    {
        if (isShowing) return;
        isShowing = true;
        // Save and then unlock/show cursor for UI interaction
        prevCursorVisible = Cursor.visible;
        prevCursorLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Pause temperature overlays (stop blinking)
        if (temperatureOverlays != null)
        {
            for (int i = 0; i < temperatureOverlays.Length; i++)
            {
                if (temperatureOverlays[i] != null) temperatureOverlays[i].SetPaused(true);
            }
        }
        StartCoroutine(FadeInRoutine());
    }

    private void Hide()
    {
        if (!isShowing) return;
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
}


