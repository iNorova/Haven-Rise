using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add this for TextMeshPro support
using System.Collections;

public class PopupInstructions : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public Text legacyText; // For legacy Text component
    public TextMeshProUGUI tmpText; // For TextMeshPro component
    
    [Header("Settings")]
    public bool showOnStart = false;
    public float fadeSpeed = 2f;
    
    private CanvasGroup canvasGroup;
    private bool isPopupActive = false;
    
    // The instruction text content
    private string instructions = 
        "GAME CONTROLS\n\n" +
        "WASD - Movement\n" +
        "SHIFT - Run\n" +
        "F - Pick Up Items\n" +
        "I - Open Inventory\n" +
        "ESC - Pause Menu\n\n" +
        "Press any key to continue...";

    void Start()
    {
        // Get the canvas group component
        canvasGroup = popupPanel.GetComponent<CanvasGroup>();
        
        // Set the instruction text (try both text types)
        SetInstructionText();
        
        // Show popup on start if enabled
        if (showOnStart)
        {
            ShowPopup();
        }
    }
    
    void Update()
    {
        // Check if popup is active and any key is pressed
        if (isPopupActive && Input.anyKeyDown)
        {
            HidePopup();
        }
    }
    
    /// <summary>
    /// Set text on whichever text component is available
    /// </summary>
    private void SetInstructionText()
    {
        if (tmpText != null)
        {
            tmpText.text = instructions;
        }
        else if (legacyText != null)
        {
            legacyText.text = instructions;
        }
    }
    
    /// <summary>
    /// Show the popup with fade-in effect
    /// </summary>
    public void ShowPopup()
    {
        if (isPopupActive) return;
        
        isPopupActive = true;
        popupPanel.SetActive(true);
        
        // Pause the game
        Time.timeScale = 0f;
        
        // Start fade in animation
        StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// Hide the popup with fade-out effect
    /// </summary>
    public void HidePopup()
    {
        if (!isPopupActive) return;
        
        StartCoroutine(FadeOut());
    }
    
    /// <summary>
    /// Fade in animation
    /// </summary>
    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    /// <summary>
    /// Fade out animation
    /// </summary>
    private IEnumerator FadeOut()
    {
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= fadeSpeed * Time.unscaledDeltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        popupPanel.SetActive(false);
        isPopupActive = false;
        
        // Resume the game
        Time.timeScale = 1f;
    }
}