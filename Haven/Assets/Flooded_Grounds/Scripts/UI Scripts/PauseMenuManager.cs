using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject pauseMenuPanel;
    public Button saveAndMenuButton;
    public Slider volumeSlider;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuPanel.SetActive(false);

        saveAndMenuButton.onClick.AddListener(SaveAndGoToMenu);
        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.value = AudioListener.volume;
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
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

        // Lock player input
        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null)
            motor.SetInputActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;

        // Re-enable player input
        var motor = FindObjectOfType<CharController_Motor>();
        if (motor != null)
            motor.SetInputActive(true);

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
        SaveGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MAIN MENU FINAL");
    }

    void SaveGame()
    {
        PlayerPrefs.Save();
        Debug.Log("Game saved!");
    }

    void OnDestroy()
    {
        saveAndMenuButton.onClick.RemoveListener(SaveAndGoToMenu);
        volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }
}
