using System.Collections;
using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using TMPro;
using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Manages the visual list of active tasks for the current level.
    /// Hooks into LevelTaskTracker via UnityEvents and manages notebook visibility,
    /// HUD new-task notifications, and the active task count badge (red dot).
    /// </summary>
    public class TaskTrackerUI : MonoBehaviour
    {
        [Header("Prefabs & Containers")]
        [Tooltip("The prefab for a single task item in the UI.")]
        [SerializeField] private TaskItemUI itemPrefab;
        
        [Tooltip("The container where task items will be spawned (e.g., a VerticalLayoutGroup).")]
        [SerializeField] private Transform listContainer;

        [Header("Notebook Animation & Visibility")]
        [Tooltip("The Animator component that handles the notebook open/close transition.")]
        [SerializeField] private Animator panelAnimator;
        
        [Tooltip("The boolean parameter name in the Animator Controller to trigger show/hide.")]
        [SerializeField] private string isVisibleBool = "IsVisible";

        [Header("New Task Notification Banner")]
        [Tooltip("Root GameObject of the popup notification panel.")]
        [SerializeField] private GameObject notificationRoot;

        [Tooltip("The Animator on the notification panel.")]
        [SerializeField] private Animator notificationAnimator;

        [Tooltip("Optional text component to display the new task title.")]
        [SerializeField] private TMP_Text notificationTitleText;

        [Tooltip("Animator boolean parameter name to trigger notification show/hide.")]
        [SerializeField] private string notificationVisibleBool = "IsVisible";

        [Tooltip("How many seconds the notification stays on screen before hiding.")]
        [SerializeField] private float notificationDuration = 3.5f;

        [Header("Active Task Badge (Red Dot)")]
        [Tooltip("Text component inside the red dot displaying the active task count.")]
        [SerializeField] private TMP_Text badgeCountText;

        [Header("Player Control (Optional)")]
        [Tooltip("Reference to the PlayerController to freeze movement/aim while notebook is open. Auto-detected if null.")]
        [SerializeField] private PlayerController playerController;

        // Maps taskId to its visual UI component
        private Dictionary<string, TaskItemUI> activeTaskItems = new Dictionary<string, TaskItemUI>();
        private Queue<TaskItemUI> itemPool = new Queue<TaskItemUI>();
        
        private int activeUncompletedCount = 0;
        private bool isPanelVisible = false;
        private Coroutine notificationCoroutine;

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

            if (notificationRoot != null && notificationAnimator == null)
            {
                notificationRoot.SetActive(false);
            }

            UpdateBadge();
        }

        private void Update()
        {
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

        #region Task Management

        public void AddTask(HostTaskManager mgr)
        {
            if (mgr == null || mgr.task == null || string.IsNullOrEmpty(mgr.task.taskId)) return;

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

            activeUncompletedCount++;
            UpdateBadge();

            TriggerNotification(mgr.task.taskName);
        }

        public void ReturnToPool(string taskId)
        {
            if (activeTaskItems.TryGetValue(taskId, out var item))
            {
                item.gameObject.SetActive(false);
                itemPool.Enqueue(item);
                activeTaskItems.Remove(taskId);
            }
        }

        public void OnTaskCompleted(string taskId)
        {
            if (activeTaskItems.TryGetValue(taskId, out var item))
            {
                item.MarkCompleted();

                if (activeUncompletedCount > 0)
                {
                    activeUncompletedCount--;
                }
                UpdateBadge();
            }
        }

        #endregion

        #region Notification & Badge

        private void TriggerNotification(string taskName)
        {
            if (notificationTitleText != null && !string.IsNullOrEmpty(taskName))
            {
                notificationTitleText.text = taskName;
            }

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }
            notificationCoroutine = StartCoroutine(NotificationRoutine());
        }

        private IEnumerator NotificationRoutine()
        {
            if (notificationRoot != null)
            {
                notificationRoot.SetActive(true);
            }

            if (notificationAnimator != null)
            {
                notificationAnimator.SetBool(notificationVisibleBool, true);
            }

            yield return new WaitForSeconds(notificationDuration);

            if (notificationAnimator != null)
            {
                notificationAnimator.SetBool(notificationVisibleBool, false);
            }
            else if (notificationRoot != null)
            {
                notificationRoot.SetActive(false);
            }

            notificationCoroutine = null;
        }

        private void UpdateBadge()
        {
            if (badgeCountText != null)
            {
                badgeCountText.text = activeUncompletedCount.ToString();
            }
        }

        #endregion

        #region Visibility

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
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isOpen;

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (playerController != null)
            {
                playerController.SetInputActive(!isOpen);
            }
        }

        #endregion
    }
}
