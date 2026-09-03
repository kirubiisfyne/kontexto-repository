using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Manages the visual list of active tasks for the current level.
    /// Hooks into LevelTaskTracker via UnityEvents and manages notebook visibility and cursor state.
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

        [Header("Player Control (Optional)")]
        [Tooltip("Reference to the PlayerController to freeze movement/aim while notebook is open. Auto-detected if null.")]
        [SerializeField] private PlayerController playerController;

        // Maps taskId to its visual UI component
        private Dictionary<string, TaskItemUI> activeTaskItems = new Dictionary<string, TaskItemUI>();
        
        // Pool for UI elements
        private Queue<TaskItemUI> itemPool = new Queue<TaskItemUI>();
        
        private bool isPanelVisible = false;

        private void Awake()
        {
            if (panelAnimator == null)
            {
                panelAnimator = GetComponent<Animator>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }
        }

        private void Update()
        {
            // Toggle with Tab, or close with Escape if already open
            if (Input.GetKeyDown(KeyCode.Tab) || (isPanelVisible && Input.GetKeyDown(KeyCode.Escape)))
            {
                TogglePanel();
            }
        }

        private void OnDisable()
        {
            if (isPanelVisible)
            {
                SetCursorAndInputState(false);
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

            TaskItemUI instance;
            if (itemPool.Count > 0)
            {
                instance = itemPool.Dequeue();
                instance.gameObject.SetActive(true);
            }
            else
            {
                instance = Instantiate(itemPrefab, listContainer);
            }
            
            instance.Setup(mgr.task.taskId, mgr.task.taskName);
            
            activeTaskItems[mgr.task.taskId] = instance;
            
            // Show the tracker if it was hidden
            Show();
        }

        /// <summary>
        /// Returns a task item to the pool instead of destroying it.
        /// </summary>
        public void ReturnToPool(string taskId)
        {
            if (activeTaskItems.TryGetValue(taskId, out var item))
            {
                item.gameObject.SetActive(false);
                itemPool.Enqueue(item);
                activeTaskItems.Remove(taskId);
            }
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
            SetCursorAndInputState(isPanelVisible);
        }

        public void Show()
        {
            isPanelVisible = true;
            if (panelAnimator != null)
            {
                panelAnimator.SetBool(isVisibleBool, true);
            }
            SetCursorAndInputState(true);
        }

        public void Hide()
        {
            isPanelVisible = false;
            if (panelAnimator != null)
            {
                panelAnimator.SetBool(isVisibleBool, false);
            }
            SetCursorAndInputState(false);
        }

        private void SetCursorAndInputState(bool isOpen)
        {
            // Unlock & show cursor when open; lock & hide cursor when closed
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;

            // Pause player movement and camera rotation so mouse clicks don't rotate the player
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.SetInputActive(!isOpen);
            }
        }
    }
}
