using System.Collections;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using Master.Scripts;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Configuration")]
    [SerializeField] private string gameplaySceneName = "scn_campus";

    [Header("UI Panels")]
    [SerializeField] private GameObject menuButtonsPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Options Functionality")]
    [SerializeField] private Toggle fullscreenToggle; // <-- Reference to your UI Toggle object

    private AsyncOperation loadOp;

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

        // Start loading the gameplay scene additively as a background
        StartCoroutine(LoadBackgroundAsync());
    }

    private IEnumerator LoadBackgroundAsync()
    {
        // Load target environment without unloading UI context
        loadOp = SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Additive);
        loadOp.allowSceneActivation = true;

        while (!loadOp.isDone)
        {
            yield return null;
        }

        // Set environment active to inherit correct skybox/ambient lighting settings
        Scene bgScene = SceneManager.GetSceneByName(gameplaySceneName);
        if (bgScene.IsValid())
        {
            SceneManager.SetActiveScene(bgScene);
        }
    }

    public void PlayGame()
    {
        // Safety Check: Ensure the background scene is actually loaded
        if (loadOp == null || !loadOp.isDone)
        {
            Debug.LogWarning("PlayGame: Background scene is still loading...");
            return; 
        }

        // Find the player in the additively loaded scene and activate them
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.SetInputActive(true);
        }

        // Unload the UI scene this script is part of, leaving the background scene active
        SceneManager.UnloadSceneAsync(gameObject.scene);
        Debug.Log("Play button clicked! Transitioning to gameplay scene...");
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