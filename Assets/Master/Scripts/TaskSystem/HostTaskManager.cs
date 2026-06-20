using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts.TaskSystem
{
    /// <summary>
    /// Groups all per-status Unity Events into a single collapsible field in the Inspector.
    /// </summary>
    [System.Serializable]
    public class TaskEvents
    {
        [Tooltip("Fired when the task becomes Inactive.")]
        public UnityEvent onInactive;

        [Tooltip("Fired when the task becomes Active.")]
        public UnityEvent onActive;

        [Tooltip("Fired when all objectives are met and the task is awaiting hand-in at the Closer NPC.")]
        public UnityEvent onReadyToComplete;

        [Tooltip("Fired when the task becomes Completed.")]
        public UnityEvent onCompleted;
    }

    /// <summary>
    /// The primary manager for an NPC's task. Tracks status and progress.
    /// Can sync status across multiple NPCs (Givers and Closers).
    /// </summary>
    public class HostTaskManager : MonoBehaviour, IInteractable
    {
        [Header("Task Configuration")]
        public TaskData task;
        public HostType hostType;
        public TaskStatus status = TaskStatus.Inactive;
        
        [Header("Sync Settings")]
        [Tooltip("If this is a Giver, add all Closer NPCs for this task here to keep them in sync.")]
        public List<HostTaskManager> linkedClosers = new List<HostTaskManager>();

        [Header("Progress")]
        [Tooltip("Tracks progress for each objective in the TaskData list.")]
        public List<int> currentProgress;

        [Header("Events")]
        [Tooltip("Per-status events. Expand to assign callbacks for Inactive, Active, and Completed states.")]
        public TaskEvents events;

        private void Awake()
        {
            // Initialization logic (if any)
        }

        /// <summary>
        /// Handles the task-related interaction logic.
        /// </summary>
        public void Interact()
        {
            if (task == null) return;

            // Closers hand in the task when all objectives are met.
            if (hostType == HostType.Closer)
            {
                CompleteTask();
                return;
            }

            // Givers only start the task.
            if (status == TaskStatus.Inactive)
                StartTask();
        }

        private void StartTask()
        {
            if (task == null || task.objectives == null)
            {
                Debug.LogError($"HostTaskManager on {gameObject.name}: Cannot start task. TaskData or Objectives are missing!");
                return;
            }

            UpdateStatus(TaskStatus.Active);
            
            // Initialize progress list based on task objectives
            currentProgress = new List<int>();
            foreach (var objective in task.objectives)
            {
                currentProgress.Add(0);
            }

            Debug.Log($"HostTaskManager on {gameObject.name}: Task '{task.taskName}' started.");
        }

        /// <summary>
        /// Locates the Giver task manager that lists this Closer in its linkedClosers.
        /// </summary>
        private HostTaskManager FindGiver()
        {
            var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
            foreach (var manager in allManagers)
            {
                if (manager.hostType == HostType.Giver && manager.linkedClosers.Contains(this))
                {
                    return manager;
                }
            }
            return null;
        }

        /// <summary>
        /// Reports progress toward a specific objective key. Returns true if the report was accepted.
        /// Enforces sequential completion based on the list order in TaskData.
        /// </summary>
        public bool ReportProgress(string key, int amount)
        {
            if (hostType == HostType.Closer)
            {
                var giver = FindGiver();
                if (giver != null)
                {
                    return giver.ReportProgress(key, amount);
                }
                Debug.LogWarning($"HostTaskManager on {gameObject.name}: Is a Closer, but could not find a Giver that links to it.");
                return false;
            }

            if (status == TaskStatus.Completed)
            {
                Debug.Log($"{gameObject.name}: Ignoring report for '{key}'. Task is already completed.");
                return false;
            }

            if (status != TaskStatus.Active || task == null) return false;
            if (currentProgress == null || currentProgress.Count < task.objectives.Count)
            {
                Debug.LogWarning($"HostTaskManager on {gameObject.name}: Progress reported but currentProgress is not initialized/ready.");
                return false;
            }

            for (int i = 0; i < task.objectives.Count; i++)
            {
                if (task.objectives[i].key == key)
                {
                    // SEQUENCE CHECK: Only enforce if the task settings require it
                    if (task.needsSequentialOrder)
                    {
                        for (int prev = 0; prev < i; prev++)
                        {
                            if (currentProgress[prev] < task.objectives[prev].requiredAmount)
                            {
                                Debug.LogWarning($"HostTaskManager on {gameObject.name}: Interaction for '{key}' ignored. Complete '{task.objectives[prev].key}' first.");
                                return false;
                            }
                        }
                    }

                    currentProgress[i] = Mathf.Clamp(currentProgress[i] + amount, 0, task.objectives[i].requiredAmount);
                    Debug.Log($"HostTaskManager on {gameObject.name}: Progress for '{key}' updated to {currentProgress[i]}/{task.objectives[i].requiredAmount}");
                    
                    CheckCompletion();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Logic check to see if all requirements in the TaskData are satisfied.
        /// </summary>
        public bool AreObjectivesMet()
        {
            if (task == null) return false;
            if (currentProgress == null || currentProgress.Count < task.objectives.Count) return false;
            for (int i = 0; i < task.objectives.Count; i++)
            {
                if (currentProgress[i] < task.objectives[i].requiredAmount) return false;
            }
            return true;
        }

        private void CheckCompletion()
        {
            if (status == TaskStatus.Active && AreObjectivesMet())
            {
                UpdateStatus(TaskStatus.ReadyToComplete);
            }
        }

        /// <summary>
        /// Finalizes the task and syncs status across all linked NPCs.
        /// Can be called manually via events.
        /// </summary>
        public void CompleteTask()
        {
            if (hostType == HostType.Closer)
            {
                var giver = FindGiver();
                if (giver != null)
                {
                    giver.CompleteTask();
                    return;
                }
                Debug.LogWarning($"HostTaskManager on {gameObject.name}: Is a Closer, but could not find a Giver that links to it.");
                return;
            }

            if (status != TaskStatus.ReadyToComplete) return;

            if (!AreObjectivesMet())
            {
                Debug.LogWarning($"HostTaskManager on {gameObject.name}: Manual completion failed. Requirements not met.");
                return;
            }

            UpdateStatus(TaskStatus.Completed);
            Debug.Log($"HostTaskManager on {gameObject.name}: Task '{task.taskName}' completed!");
        }

        /// <summary>
        /// Updates the status of this NPC and propagates it to linked closers.
        /// </summary>
        public void UpdateStatus(TaskStatus newStatus)
        {
            this.status = newStatus;

            // Notify listeners for the specific new status
            switch (newStatus)
            {
                case TaskStatus.Inactive:        events.onInactive?.Invoke();        break;
                case TaskStatus.Active:           events.onActive?.Invoke();           break;
                case TaskStatus.ReadyToComplete:  events.onReadyToComplete?.Invoke();  break;
                case TaskStatus.Completed:        events.onCompleted?.Invoke();        break;
            }
            Debug.Log($"{gameObject.name}: Task status changed to {newStatus}.");

            // If we are a giver, push this status to all our closers
            if (hostType == HostType.Giver)
            {
                foreach (var closer in linkedClosers)
                {
                    if (closer != null) closer.UpdateStatus(newStatus);
                }
            }
        }
    }
}
