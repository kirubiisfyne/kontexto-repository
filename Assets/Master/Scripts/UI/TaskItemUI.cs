using System.Text;
using TMPro;
using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Represents a single task in the visual task tracker.
    /// </summary>
    public class TaskItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The text component displaying the task description.")]
        [SerializeField] private TMP_Text descriptionText;

        [Tooltip("Optional checkmark image to enable when the task is completed.")]
        [SerializeField] private GameObject checkmarkImage;

        public string TaskId { get; private set; }
        
        private readonly StringBuilder _sb = new StringBuilder();

        /// <summary>
        /// Initializes the UI with the task data.
        /// </summary>
        public void Setup(string taskId, string description)
        {
            TaskId = taskId;
            
            if (descriptionText != null)
            {
                descriptionText.raycastTarget = false;
                _sb.Clear();
                _sb.Append(description);
                descriptionText.SetText(_sb);
            }
            else
            {
                //Debug.LogWarning("TaskItemUI: descriptionText is not assigned.", this);
            }

            if (checkmarkImage != null)
            {
                checkmarkImage.SetActive(false);
            }
        }

        /// <summary>
        /// Marks the UI task as completed.
        /// </summary>
        public void MarkCompleted()
        {
            if (descriptionText != null)
            {
                descriptionText.fontStyle |= FontStyles.Strikethrough;
                descriptionText.alpha = 0.5f;
            }

            if (checkmarkImage != null)
            {
                checkmarkImage.SetActive(true);
            }
        }
    }
}
