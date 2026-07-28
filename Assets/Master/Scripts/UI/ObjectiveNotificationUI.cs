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

        // Cached Animator parameter hash for performance
        private static readonly int IsVisibleBool = Animator.StringToHash("IsVisible");

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
        /// Triggers the transition to display the notification.
        /// </summary>
        public void Show()
        {
            if (animator != null)
            {
                animator.SetBool(IsVisibleBool, true);
            }
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

        /// <summary>
        /// Plays the hide animation and destroys the UI object after a delay.
        /// </summary>
        public void CompleteAndDestroy()
        {
            if (animator != null)
            {
                animator.SetBool(IsVisibleBool, false);
            }
            
            // Destroy the object after 1 second to give the Hide animation time to play
            Destroy(gameObject, 1f);
        }
    }
}
