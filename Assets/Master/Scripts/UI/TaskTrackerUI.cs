using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Manages the visual list of active tasks for the current level.
    /// Hooks into LevelTaskTracker via UnityEvents.
    /// </summary>
    public class TaskTrackerUI : MonoBehaviour
    {
        [Header("Prefabs & Containers")]
        [Tooltip("The prefab for a single task item in the UI.")]
        [SerializeField] private TaskItemUI itemPrefab;
        
        [Tooltip("The container where task items will be spawned (e.g., a VerticalLayoutGroup).")]
        [SerializeField] private Transform listContainer;

        [Header("Animation")]
        [Tooltip("The Animator component that handles the transition.")]
        [SerializeField] private Animator panelAnimator;
        
        [Tooltip("The boolean parameter name in the Animator Controller to trigger show/hide.")]
        [SerializeField] private string isVisibleBool = "IsVisible";

        // Maps taskId to its visual UI component
        private Dictionary<string, TaskItemUI> activeTaskItems = new Dictionary<string, TaskItemUI>();
        
        private bool isPanelVisible = false;

        private void Awake()
        {
            if (panelAnimator == null)
            {
                panelAnimator = GetComponent<Animator>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TogglePanel();
            }
        }

        /// <summary>
        /// Called when a task becomes active dynamically (e.g., after NPC conversation).
        /// </summary>
        public void AddTask(HostTaskManager mgr)
        {
            if (mgr == null || mgr.task == null || string.IsNullOrEmpty(mgr.task.taskId)) return;

            // Prevent duplicates
            if (activeTaskItems.ContainsKey(mgr.task.taskId)) return;

            var instance = Instantiate(itemPrefab, listContainer);
            instance.Setup(mgr.task.taskId, mgr.task.taskName);
            
            activeTaskItems[mgr.task.taskId] = instance;
            
            // Show the tracker if it was hidden
            Show();
        }

        /// <summary>
        /// Called when a task is completed.
        /// </summary>
        /// <param name="taskId">The ID of the completed task.</param>
        public void OnTaskCompleted(string taskId)
        {
            if (activeTaskItems.TryGetValue(taskId, out var item))
            {
                item.MarkCompleted();
            }
        }

        /// <summary>
        /// Toggles the visibility state of the panel using the Animator.
        /// </summary>
        public void TogglePanel()
        {
            isPanelVisible = !isPanelVisible;
            if (panelAnimator != null)
            {
                panelAnimator.SetBool(isVisibleBool, isPanelVisible);
            }
        }

        public void Show()
        {
            isPanelVisible = true;
            if (panelAnimator != null)
            {
                panelAnimator.SetBool(isVisibleBool, true);
            }
        }

        public void Hide()
        {
            isPanelVisible = false;
            if (panelAnimator != null)
            {
                panelAnimator.SetBool(isVisibleBool, false);
            }
        }
    }
}
