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
    [SerializeField] private string cutsceneSceneName = "scn_cutscene";

    [Header("Level Configuration")]
    [Tooltip("The central LevelDatabase to determine starting/continuing days.")]
    [SerializeField] private LevelDatabase levelDatabase;

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

        // Set initial starting level (Index 0) in GameManager
        LevelSequenceData startingSequence = null;
        if (levelDatabase != null && levelDatabase.Count > 0)
        {
            startingSequence = levelDatabase.GetLevelByIndex(0);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetLevel(startingSequence, CutsceneMode.Intro);
            }
        }

        // Play the transition animation if the TransitionManager exists in the scene
        if (Master.Scripts.TransitionManager.Instance != null)
        {
            yield return Master.Scripts.TransitionManager.Instance.PlayTransitionAndWait("transition");
        }
        else
        {
            //Debug.LogWarning("MainMenu: No TransitionManager found in scene. Skipping transition animation.");
        }

        // If level 0 has intro cutscenes, load cutscene scene; otherwise go directly to gameplay
        if (startingSequence != null && startingSequence.HasIntro)
        {
            SceneManager.LoadScene(cutsceneSceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
    }

    public void ContinueGame()
    {
        StartCoroutine(ContinueGameRoutine());
    }

    private IEnumerator ContinueGameRoutine()
    {
        // Resolve resume level based on save progress
        PlayerData data = SaveManager.Load();
        LevelSequenceData resumeSequence = null;
        if (levelDatabase != null)
        {
            resumeSequence = levelDatabase.GetFirstIncompleteLevel(data);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetLevel(resumeSequence, CutsceneMode.Intro);
            }
        }

        if (Master.Scripts.TransitionManager.Instance != null)
        {
            yield return Master.Scripts.TransitionManager.Instance.PlayTransitionAndWait("transition");
        }

        // If player already has a mid-day saved position, resume in gameplay scene directly
        if (data != null && data.HasSavedPosition())
        {
            SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
        // Otherwise, if this day has an Intro cutscene, play it first
        else if (resumeSequence != null && resumeSequence.HasIntro)
        {
            SceneManager.LoadScene(cutsceneSceneName, LoadSceneMode.Single);
        }
        else
        {
            SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }
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