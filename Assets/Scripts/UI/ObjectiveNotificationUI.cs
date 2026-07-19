using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Kontexto.UI
{
    public class ObjectiveNotificationUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The Animator controlling the fade in/out animations. Ensure Update Mode is set to Unscaled Time.")]
        [SerializeField] private Animator animator;
        
        [Tooltip("The text component displaying the name/title of the objective (e.g. 'Collect Books').")]
        [SerializeField] private TMP_Text objectiveNameText;
        
        [Tooltip("The text component displaying the progress count (e.g. '1/4').")]
        [SerializeField] private TMP_Text notificationText;
        
        [Tooltip("The slider acting as the progress bar.")]
        [SerializeField] private Slider progressBar;

        [Header("Settings")]
        [Tooltip("How long the notification stays fully visible before the Hide animation is triggered.")]
        [SerializeField] private float displayDuration = 3f;

        // Cached Animator parameter hash for performance
        private static readonly int IsVisibleBool = Animator.StringToHash("IsVisible");

        private Coroutine displayCoroutine;

        /// <summary>
        /// Modular method to set the objective's title or name.
        /// </summary>
        public void SetObjectiveName(string name)
        {
            if (objectiveNameText != null)
            {
                objectiveNameText.text = name;
            }
        }

        /// <summary>
        /// Modular method to set the progress text manually.
        /// </summary>
        public void SetText(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }
        }

        /// <summary>
        /// Modular method to set the progress bar (expects a value between 0.0 and 1.0).
        /// </summary>
        public void SetProgress(float normalizedProgress)
        {
            if (progressBar != null)
            {
                progressBar.value = normalizedProgress;
            }
        }

        /// <summary>
        /// Modular method to trigger the show animation and timer without changing data.
        /// </summary>
        public void Show()
        {
            if (animator != null)
            {
                animator.SetBool(IsVisibleBool, true);
            }

            // Restart the display timer to handle rapid sequential interruptions smoothly
            if (displayCoroutine != null)
            {
                StopCoroutine(displayCoroutine);
            }
            displayCoroutine = StartCoroutine(DisplayTimerSequence());
        }

        /// <summary>
        /// Combined method to update text, normalized progress (0.0 to 1.0), and show the UI.
        /// </summary>
        public void ShowNotification(string message, float normalizedProgress)
        {
            SetText(message);
            SetProgress(normalizedProgress);
            Show();
        }

        /// <summary>
        /// Combined method to update name, progress text, normalized progress, and show the UI.
        /// </summary>
        public void ShowNotification(string objectiveName, string progressMessage, float normalizedProgress)
        {
            SetObjectiveName(objectiveName);
            SetText(progressMessage);
            SetProgress(normalizedProgress);
            Show();
        }

        /// <summary>
        /// Combined method that automatically formats the "current/max" text with ZERO string allocation!
        /// </summary>
        public void ShowNotification(string objectiveName, float currentProgress, float maxProgress)
        {
            SetObjectiveName(objectiveName);
            
            // Use TMP's highly performant SetText to completely avoid Garbage Collection!
            if (notificationText != null)
            {
                notificationText.SetText("{0}/{1}", currentProgress, maxProgress);
            }
            
            // Calculate the 0.0 to 1.0 value for the Slider
            float normalized = maxProgress > 0 ? (currentProgress / maxProgress) : 0f;
            SetProgress(normalized);
            
            Show();
        }

        private IEnumerator DisplayTimerSequence()
        {
            float timer = displayDuration;
            
            // Using unscaledDeltaTime to ensure the timer works even when Time.timeScale = 0
            while (timer > 0)
            {
                timer -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (animator != null)
            {
                // Trigger the fade out animation
                animator.SetBool(IsVisibleBool, false);
            }

            displayCoroutine = null;
        }
    }
}
