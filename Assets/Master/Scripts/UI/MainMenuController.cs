using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using Master.Scripts;
using Master.Scripts.SaveSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string gameplaySceneName = "scn_campus";

    [Header("UI Panels")]
    [SerializeField] private GameObject menuButtonsPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject betaThankYouPanel;

    [Header("Options Functionality")]
    [SerializeField] private Toggle fullscreenToggle; // <-- Reference to your UI Toggle object

    private void Start()
    {
        // Ensure cursor is visible in main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // When the game boots up, automatically make the checkbox match 
        // whatever display state the computer is currently running in.
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
        }

        if (betaThankYouPanel != null)
        {
            betaThankYouPanel.SetActive(false);
            PlayerData data = SaveManager.Load();
            if (data != null)
            {
                foreach (var level in data.levels)
                {
                    if (level.isCompleted)
                    {
                        betaThankYouPanel.SetActive(true);
                        break;
                    }
                }
            }
        }

        // WIPE old save data here so the next playthrough is totally fresh!
        // (We do this AFTER checking for the beta completion panel above)
        SaveManager.DeleteSave();
    }

    public void PlayGame()
    {
        Debug.Log("Play button clicked! Playing transition...");
        StartCoroutine(PlayGameRoutine());
    }

    private IEnumerator PlayGameRoutine()
    {
        // Play the transition animation if the TransitionManager exists in the scene
        if (Master.Scripts.TransitionManager.Instance != null)
        {
            yield return Master.Scripts.TransitionManager.Instance.PlayTransitionAndWait("transition");
        }
        else
        {
            Debug.LogWarning("MainMenu: No TransitionManager found in scene. Skipping transition animation.");
        }

        Debug.Log("Transition complete. Loading gameplay scene...");
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
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