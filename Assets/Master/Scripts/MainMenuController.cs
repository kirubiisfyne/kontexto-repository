using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string gameplaySceneName = "scn_campus";

    [Header("UI Panels")]
    [SerializeField] private GameObject menuButtonsPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Options Functionality")]
    [SerializeField] private Toggle fullscreenToggle; // <-- Reference to your UI Toggle object

    private void Start()
    {
        // When the game boots up, automatically make the checkbox match 
        // whatever display state the computer is currently running in.
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }
    }

    public void PlayGame()
    {
        Debug.Log("Loading scene: " + gameplaySceneName);
        SceneManager.LoadScene(gameplaySceneName);
    }

    /// Swaps the active panel from the Main Menu to the Options Menu.
    public void OpenOptions()
    {
        menuButtonsPanel.SetActive(false); // Hide main buttons
        optionsPanel.SetActive(true);      // Show options panel
    }

    /// Swaps the active panel from the Options Menu back to the Main Menu.
    public void CloseOptions()
    {
        optionsPanel.SetActive(false);      // Hide options panel
        menuButtonsPanel.SetActive(true);   // Show main buttons
    }

    /// <summary>
    /// Swaps between Fullscreen and Windowed display modes.
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        Debug.Log("Fullscreen display toggled to: " + isFullscreen);
    }

    public void QuitGame()
    {
        Debug.Log("Quit button clicked! Closing application...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}