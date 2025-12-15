using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles the "To Be Continued" cutscene/screen that appears when the ship is fully repaired.
/// </summary>
public class ToBeContinuedScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cutscenePanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public Image fadeImage;
    
    [Header("Settings")]
    public float fadeInDuration = 2f;
    public float displayDuration = 5f;
    public float fadeOutDuration = 2f;
    
    private static ToBeContinuedScreen instance;
    private bool isShowing = false;
    
    void Awake()
    {
        instance = this;
        
        // Create UI if it doesn't exist
        if (cutscenePanel == null)
        {
            CreateCutsceneUI();
        }
        
        // Initially hide
        if (cutscenePanel != null)
        {
            cutscenePanel.SetActive(false);
        }
    }
    
    public static void Show()
    {
        if (instance == null)
        {
            // Create instance if it doesn't exist
            GameObject obj = new GameObject("ToBeContinuedScreen");
            instance = obj.AddComponent<ToBeContinuedScreen>();
        }
        
        if (instance != null && !instance.isShowing)
        {
            instance.StartCutscene();
        }
    }
    
    private void StartCutscene()
    {
        if (isShowing) return;
        
        isShowing = true;
        
        // Disable player controls
        DisablePlayerControls();
        
        // Show panel
        if (cutscenePanel != null)
        {
            cutscenePanel.SetActive(true);
        }
        
        // Start cutscene coroutine
        StartCoroutine(CutsceneSequence());
    }
    
    private IEnumerator CutsceneSequence()
    {
        // Fade in
        if (fadeImage != null)
        {
            Color fadeColor = fadeImage.color;
            fadeColor.a = 0f;
            fadeImage.color = fadeColor;
            
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
                yield return null;
            }
            fadeColor.a = 1f;
            fadeImage.color = fadeColor;
        }
        
        // Show title and subtitle
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            Color titleColor = titleText.color;
            titleColor.a = 0f;
            titleText.color = titleColor;
            
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / 1f);
                titleColor.a = alpha;
                titleText.color = titleColor;
                yield return null;
            }
            titleColor.a = 1f;
            titleText.color = titleColor;
        }
        
        yield return new WaitForSecondsRealtime(1f);
        
        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(true);
            Color subtitleColor = subtitleText.color;
            subtitleColor.a = 0f;
            subtitleText.color = subtitleColor;
            
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / 1f);
                subtitleColor.a = alpha;
                subtitleText.color = subtitleColor;
                yield return null;
            }
            subtitleColor.a = 1f;
            subtitleText.color = subtitleColor;
        }
        
        // Display for duration
        yield return new WaitForSecondsRealtime(displayDuration);
        
        // Fade out
        if (fadeImage != null)
        {
            Color fadeColor = fadeImage.color;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                fadeColor.a = alpha;
                fadeImage.color = fadeColor;
                yield return null;
            }
            fadeColor.a = 0f;
            fadeImage.color = fadeColor;
        }
        
        // Hide text
        if (titleText != null)
        {
            Color titleColor = titleText.color;
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / 1f);
                titleColor.a = alpha;
                titleText.color = titleColor;
                yield return null;
            }
        }
        
        if (subtitleText != null)
        {
            Color subtitleColor = subtitleText.color;
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / 1f);
                subtitleColor.a = alpha;
                subtitleText.color = subtitleColor;
                yield return null;
            }
        }
        
        // Hide panel
        if (cutscenePanel != null)
        {
            cutscenePanel.SetActive(false);
        }
        
        // Re-enable player controls
        EnablePlayerControls();
        
        isShowing = false;
    }
    
    private void DisablePlayerControls()
    {
        // Disable player movement
        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null)
        {
            motor.SetInputActive(false);
        }
        
        // Disable hotbar input
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(false);
        }
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Pause game time (but use unscaled time for cutscene)
        Time.timeScale = 0f;
    }
    
    private void EnablePlayerControls()
    {
        // Resume time
        Time.timeScale = 1f;
        
        // Re-enable player movement
        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null)
        {
            motor.SetInputActive(true);
        }
        
        // Re-enable hotbar input
        HotbarManager hotbarManager = FindObjectOfType<HotbarManager>();
        if (hotbarManager != null)
        {
            hotbarManager.SetInputActive(true);
        }
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void CreateCutsceneUI()
    {
        // Find or create canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CutsceneCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // High priority
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create panel
        GameObject panelObj = new GameObject("CutscenePanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        panelRect.anchoredPosition = Vector2.zero;
        
        // Add fade image (black background)
        fadeImage = panelObj.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        
        // Create title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panelObj.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.6f);
        titleRect.anchorMax = new Vector2(0.5f, 0.6f);
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = Vector2.zero;
        
        titleText.text = "TO BE CONTINUED...";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;
        titleText.gameObject.SetActive(false);
        
        // Create subtitle text
        GameObject subtitleObj = new GameObject("SubtitleText");
        subtitleObj.transform.SetParent(panelObj.transform, false);
        subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        RectTransform subtitleRect = subtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(0.5f, 0.4f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.4f);
        subtitleRect.sizeDelta = new Vector2(800, 60);
        subtitleRect.anchoredPosition = Vector2.zero;
        
        subtitleText.text = "Thank you for playing!";
        subtitleText.fontSize = 36;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = Color.white;
        subtitleText.gameObject.SetActive(false);
        
        cutscenePanel = panelObj;
        cutscenePanel.SetActive(false);
    }
}

