using System.Collections;
using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool GameIsPaused = false;
    [SerializeField] private GameObject PauseMenuUI;
    
    [Header("Animation Settings")]
    [SerializeField] private Animator PauseMenuAnimator;
    [SerializeField] private float transitionDuration;
    
    //privates
    private Coroutine resumeCoroutine;
    private Coroutine pauseCoroutine;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        if (pauseCoroutine != null) StopCoroutine(pauseCoroutine);
        pauseCoroutine = null;
        
        PauseMenuAnimator.ResetTrigger("In");
        PauseMenuAnimator.SetTrigger("Out");
        resumeCoroutine = StartCoroutine(ResumeCoroutine());
    }

    private IEnumerator ResumeCoroutine()
    {
        Time.timeScale = 1f;
        
        yield return new WaitForSecondsRealtime(transitionDuration);

        GameIsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PauseMenuUI.SetActive(false);
    }
    
    public void Pause()
    {
        if (resumeCoroutine != null) StopCoroutine(resumeCoroutine);
        resumeCoroutine = null;
        
        PauseMenuAnimator.ResetTrigger("Out");
        PauseMenuAnimator.SetTrigger("In");
        pauseCoroutine = StartCoroutine(PauseCoroutine());
    }

    private IEnumerator PauseCoroutine()
    {
        // Turn on the UI immediately so the animation is visible
        PauseMenuUI.SetActive(true);
        
        // Pause the game immediately so the player is safe
        Time.timeScale = 0f;
        GameIsPaused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Use Realtime so the coroutine doesn't freeze when timeScale is 0
        yield return new WaitForSecondsRealtime(transitionDuration);
    }

}
