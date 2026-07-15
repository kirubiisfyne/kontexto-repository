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

    private void Start()
    {
        // Reset time scale to 1 when the menu finishes loading (in case we arrived from a paused game)
        Time.timeScale = 1f;

        // Ensure cursor is visible in main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

    }

    public void PlayGame()
    {
        //Debug.Log("Play button clicked! Playing transition...");
        StartCoroutine(PlayGameRoutine());
    }

    private IEnumerator PlayGameRoutine()
    {
        // For a new game, wipe old save data so the next playthrough is fresh
        SaveManager.DeleteSave();

        // Play the transition animation if the TransitionManager exists in the scene
        if (Master.Scripts.TransitionManager.Instance != null)
        {
            yield return Master.Scripts.TransitionManager.Instance.PlayTransitionAndWait("transition");
        }
        else
        {
            //Debug.LogWarning("MainMenu: No TransitionManager found in scene. Skipping transition animation.");
        }

        //Debug.Log("Transition complete. Loading gameplay scene...");
        SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
    }

    public void ContinueGame()
    {
        StartCoroutine(ContinueGameRoutine());
    }

    private IEnumerator ContinueGameRoutine()
    {
        if (Master.Scripts.TransitionManager.Instance != null)
        {
            yield return Master.Scripts.TransitionManager.Instance.PlayTransitionAndWait("transition");
        }

        // Since we only use one Unity scene for all days/levels, we always load the gameplaySceneName.
        // The GameManager/LevelLoader in that scene will handle the current day logic based on the save file!
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
        //Debug.Log("Fullscreen display toggled to: " + isFullscreen);
    }

    public void QuitGame()
    {
        //Debug.Log("Quit button clicked! Closing application...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}