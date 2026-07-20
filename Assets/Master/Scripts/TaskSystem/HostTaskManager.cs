using System.Collections;
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

        [Tooltip("Fired when the player tries to start the task, but the prerequisite is not met.")]
        public UnityEvent onPrerequisiteNotMet;
    }

    /// <summary>
    /// The primary manager for an NPC's task. Tracks status and progress.
    /// Can sync status across multiple NPCs (Givers and Closers).
    /// </summary>
    public class HostTaskManager : MonoBehaviour, IInteractable
    {
        // Global events to notify UI Controllers without Inspector wiring
        public static event System.Action<string, string, float> OnTaskStartedGlobal;
        public static event System.Action<string, string, float, float> OnProgressReportedGlobal;

        [Header("Task Configuration")]
        public TaskData task;
        public HostType hostType;
        public TaskStatus status = TaskStatus.Inactive;
        
        [Header("Sync Settings")]
        [Tooltip("Legacy. You no longer need to link Closers manually. The system auto-links them if they share the same TaskData.")]
        public List<HostTaskManager> linkedClosers = new List<HostTaskManager>();

        [Header("Progress")]
        [Tooltip("Tracks progress for each objective in the TaskData list.")]
        public List<int> currentProgress;

        [Header("Events")]
        [Tooltip("Per-status events. Expand to assign callbacks for Inactive, Active, ReadyToComplete, and Completed states.")]
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

            // Prerequisite check must happen synchronously so it can override the DialogueManager
            // before the DialogueManager's coroutine yields for the first frame.
            if (status == TaskStatus.Inactive && HasUnmetPrerequisite())
            {
                //Debug.Log($"HostTaskManager on {gameObject.name}: Prerequisite '{task.prerequisite.task.taskName}' not met. Cannot start task.");
                events.onPrerequisiteNotMet?.Invoke();
                return;
            }

            // Both: defers the state change by one frame so DialogueManager.Interact()
            // can snapshot the correct conversation index before status (and index) changes.
            if (hostType == HostType.Both)
            {
                StartCoroutine(DeferredBothInteract());
                return;
            }

            // Givers only start the task.
            if (status == TaskStatus.Inactive)
            {
                StartTask();
            }
        }

        private IEnumerator DeferredBothInteract()
        {
            // Wait one frame so DialogueManager.Interact() snapshots the correct
            // conversation index before StartTask() / CompleteTask() change the status.
            yield return null;
            if (status == TaskStatus.Inactive)
            {
                StartTask();
            }
            else if (status == TaskStatus.ReadyToComplete)
            {
                CompleteTask();
            }
        }

        private bool HasUnmetPrerequisite()
        {
            if (task == null || task.prerequisite == null || task.prerequisite.task == null) return false;
            if (string.IsNullOrEmpty(task.prerequisite.task.taskId)) return false;

            // 1. If the requirement is Completed, check the global save data first.
            // This ensures prerequisites can span across levels.
            if (task.prerequisite.requiredStatus == TaskStatus.Completed)
            {
                var pData = Master.Scripts.GameManager.Instance != null 
                    ? Master.Scripts.GameManager.Instance.currentPlayerData 
                    : Master.Scripts.SaveSystem.SaveManager.Load();

                if (pData != null && pData.IsTaskCompletedGlobally(task.prerequisite.task.taskId))
                {
                    return false; // Prerequisite IS met globally
                }
            }

            // 2. Check the current scene's active task managers.
            // This supports unlocking if the prerequisite is in the same scene and meets the minimum status.
            var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
            foreach (var manager in allManagers)
            {
                // Find the manager handling the prerequisite task
                if (manager.task != null && manager.task.taskId == task.prerequisite.task.taskId)
                {
                    // In TaskStatus enum: Inactive=0, Active=1, ReadyToComplete=2, Completed=3.
                    // If its status is >= the required status, the prerequisite is met.
                    if (manager.status >= task.prerequisite.requiredStatus)
                    {
                        return false; // Prerequisite IS met in this scene
                    }
                }
            }

            return true; // Prerequisite is UNMET
        }

        public void StartTask()
        {
            if (task == null || task.requirements == null || task.requirements.objectives == null)
            {
                //Debug.LogError($"HostTaskManager on {gameObject.name}: Cannot start task. TaskData or Objectives are missing!");
                return;
            }

            UpdateStatus(TaskStatus.Active);
            
            // Initialize progress list based on task objectives
            currentProgress = new List<int>();
            foreach (var objective in task.requirements.objectives)
            {
                currentProgress.Add(0);
                
                // Notify the UI that this objective has started
                string uniqueId = $"{task.taskId}_{objective.key}";
                string displayName = string.IsNullOrEmpty(objective.notificationDisplayName) ? task.taskName : objective.notificationDisplayName;
                OnTaskStartedGlobal?.Invoke(uniqueId, displayName, objective.requiredAmount);
            }

            //Debug.Log($"HostTaskManager on {gameObject.name}: Task '{task.taskName}' started.");
        }

        /// <summary>
        /// Locates the Giver task manager dynamically based on the TaskData.
        /// </summary>
        private HostTaskManager FindGiver()
        {
            var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
            foreach (var manager in allManagers)
            {
                if (manager.task == this.task && (manager.hostType == HostType.Giver || manager.hostType == HostType.Both))
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
            // Closers and Both defer progress reporting to the Giver.
            // Both is its own Giver, so it handles reports directly — no delegation needed.
            if (hostType == HostType.Closer)
            {
                var giver = FindGiver();
                if (giver != null)
                {
                    return giver.ReportProgress(key, amount);
                }
                //Debug.LogWarning($"HostTaskManager on {gameObject.name}: Is a Closer, but could not find a Giver that links to it.");
                return false;
            }

            if (status == TaskStatus.Completed)
            {
                //Debug.Log($"{gameObject.name}: Ignoring report for '{key}'. Task is already completed.");
                return false;
            }

            if (status != TaskStatus.Active || task == null) return false;
            if (currentProgress == null || task.requirements == null || task.requirements.objectives == null || currentProgress.Count < task.requirements.objectives.Count)
            {
                //Debug.LogWarning($"HostTaskManager on {gameObject.name}: Progress reported but currentProgress is not initialized/ready.");
                return false;
            }

            for (int i = 0; i < task.requirements.objectives.Count; i++)
            {
                if (task.requirements.objectives[i].key == key)
                {
                    // SEQUENCE CHECK: Only enforce if the task settings require it
                    if (task.requirements.needsSequentialOrder)
                    {
                        for (int prev = 0; prev < i; prev++)
                        {
                            if (currentProgress[prev] < task.requirements.objectives[prev].requiredAmount)
                            {
                                //Debug.LogWarning($"HostTaskManager on {gameObject.name}: Interaction for '{key}' ignored. Complete '{task.requirements.objectives[prev].key}' first.");
                                return false;
                            }
                        }
                    }

                    currentProgress[i] = Mathf.Clamp(currentProgress[i] + amount, 0, task.requirements.objectives[i].requiredAmount);
                    //Debug.Log($"HostTaskManager on {gameObject.name}: Progress for '{key}' updated to {currentProgress[i]}/{task.requirements.objectives[i].requiredAmount}");
                    
                    // Notify the UI Controller of the new progress!
                    string uniqueId = $"{task.taskId}_{task.requirements.objectives[i].key}";
                    string displayName = string.IsNullOrEmpty(task.requirements.objectives[i].notificationDisplayName) ? task.taskName : task.requirements.objectives[i].notificationDisplayName;
                    OnProgressReportedGlobal?.Invoke(uniqueId, displayName, currentProgress[i], task.requirements.objectives[i].requiredAmount);

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
            if (currentProgress == null || task.requirements == null || task.requirements.objectives == null || currentProgress.Count < task.requirements.objectives.Count) return false;
            for (int i = 0; i < task.requirements.objectives.Count; i++)
            {
                if (currentProgress[i] < task.requirements.objectives[i].requiredAmount) return false;
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
            // Closers delegate to their Giver.
            // Both is its own Giver, so it completes itself directly — no delegation needed.
            if (hostType == HostType.Closer)
            {
                var giver = FindGiver();
                if (giver != null)
                {
                    giver.CompleteTask();
                    return;
                }
                //Debug.LogWarning($"HostTaskManager on {gameObject.name}: Is a Closer, but could not find a Giver that links to it.");
                return;
            }

            if (status != TaskStatus.ReadyToComplete) return;

            if (!AreObjectivesMet())
            {
                //Debug.LogWarning($"HostTaskManager on {gameObject.name}: Manual completion failed. Requirements not met.");
                return;
            }

            UpdateStatus(TaskStatus.Completed);
            //Debug.Log($"HostTaskManager on {gameObject.name}: Task '{task.taskName}' completed!");
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
            //Debug.Log($"{gameObject.name}: Task status changed to {newStatus}.");

            // Givers and Both push status to all corresponding Closers in the scene.
            if (hostType == HostType.Giver || hostType == HostType.Both)
            {
                var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
                foreach (var manager in allManagers)
                {
                    if (manager != this && manager.task == this.task && manager.hostType == HostType.Closer)
                    {
                        manager.UpdateStatus(newStatus);
                    }
                }
            }
        }
    }
}
