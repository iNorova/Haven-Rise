using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    [Tooltip("Duration of the cutscene in seconds.")]
    public float cutsceneDuration = 10f;

    private bool cutscenePlaying = true;

    void Start()
    {
        StartCoroutine(PlayCutsceneAndLoadGame());
    }

    IEnumerator PlayCutsceneAndLoadGame()
    {
        // Wait for the duration of the cutscene (or replace with your cutscene completion event)
        yield return new WaitForSeconds(cutsceneDuration);

        cutscenePlaying = false;

        // After cutscene, load the game scene using the stored index in SceneLoadData
        int gameSceneIndex = 2;

        if (gameSceneIndex >= 0)
        {
            SceneManager.LoadScene(gameSceneIndex);
        }
        else
        {
            Debug.LogError("Game scene index is not set or invalid.");
        }
    }

    // Alternatively, you can call this method to end the cutscene manually (e.g., after animation/event)
    public void EndCutscene()
    {
        if (cutscenePlaying)
        {
            cutscenePlaying = false;
            int gameSceneIndex = 2;
            if (gameSceneIndex >= 0)
            {
                SceneManager.LoadScene(gameSceneIndex);
            }
            else
            {
                Debug.LogError("Game scene index is not set or invalid.");
            }
        }
    }
}
