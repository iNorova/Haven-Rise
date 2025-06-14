using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Static class to hold scene data between transitions.
/// </summary>
public static class SceneLoadData
{
    // Scene index of the game scene to load after the cutscene finishes
    public static int gameSceneIndex = 2;
}

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject loadingScreen;      // Panel or Canvas group for the loading UI overlay
    public Slider progressBar;            // Progress bar slider UI
    public TextMeshProUGUI progressText;  // Text showing percentage of loading
    public TextMeshProUGUI tipsText;      // Text showing random tips

    [Header("Settings")]
    [Tooltip("Set this to the build index of the cutscene scene as arranged in Unity Build Settings.")]
    public int cutsceneSceneIndex = 1;    // Index of the cutscene scene to load

    [Tooltip("Set this to the build index of the game scene as arranged in Unity Build Settings.")]
    public int gameSceneIndex = 2;         // Index of the game scene to load after cutscene finishes

    public float tipChangeInterval = 2f;

    private string[] tips = new string[]
    {
        "Tip: Use cover to survive enemy fire!",
        "Tip: Explore every corner for hidden treasures!",
        "Tip: Upgrade your equipment regularly.",
        "Tip: Use special abilities to gain advantage.",
        "Tip: Keep an eye on your health bar.",
        "Tip: Listen for audio cues to detect danger.",
        "Tip: Use the map to navigate efficiently.",
        "Tip: Strategy beats brute force.",
        "Tip: Save your progress often.",
        "Tip: Experiment with different weapon combinations."
    };

    private Coroutine tipShuffleCoroutine; // Reference to the tip shuffling coroutine

    void Start()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(false); // Hide loading UI initially
    }

    // Called by the Start Button's OnClick event
    public void OnStartButtonPressed()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true); // Show loading UI overlay

        // Store game scene index in static class so the cutscene scene knows which scene to load next
        SceneLoadData.gameSceneIndex = gameSceneIndex;

        ShowRandomTip();
        tipShuffleCoroutine = StartCoroutine(ShuffleTips()); // Start shuffling tips
        StartCoroutine(LoadCutsceneAsync());
    }

    void ShowRandomTip()
    {
        if (tipsText != null && tips.Length > 0)
        {
            int randomIndex = Random.Range(0, tips.Length);
            tipsText.text = tips[randomIndex];
        }
    }

    IEnumerator ShuffleTips()
    {
        while (true) // Infinite loop to keep changing tips
        {
            ShowRandomTip(); // Show a random tip
            yield return new WaitForSeconds(tipChangeInterval); // Wait for the specified interval
        }
    }

    IEnumerator LoadCutsceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(cutsceneSceneIndex);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            if (progressText != null)
                progressText.text = (progress * 100f).ToString("F0") + "%";

            if (operation.progress >= 0.9f)
            {
                if (progressText != null)
                    progressText.text = "Press any key to start the game";

                if (Input.anyKeyDown)
                {
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;

            if (tipShuffleCoroutine != null)
            {
                StopCoroutine(tipShuffleCoroutine);
                tipShuffleCoroutine = null;
            }
        }
    }
}

