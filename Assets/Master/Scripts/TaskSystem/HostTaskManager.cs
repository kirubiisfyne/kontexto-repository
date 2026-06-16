using System.Collections.Generic;
using UnityEngine;
using Master.Scripts.DialogueSystem;

namespace Master.Scripts.TaskSystem
{
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

        private DialogueManager dialogueManager;

        private void Awake()
        {
            dialogueManager = GetComponent<DialogueManager>();
        }

        /// <summary>
        /// Handles the task-related interaction logic.
        /// </summary>
        public void Interact()
        {
            if (task == null) return;

            // Only Givers initiate or manually check completion via interaction.
            if (hostType == HostType.Closer) return;

            switch (status)
            {
                case TaskStatus.Inactive:
                    StartTask();
                    break;
                case TaskStatus.Active:
                    CheckCompletion();
                    break;
            }
        }

        private void StartTask()
        {
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
        /// Reports progress toward a specific objective key. Returns true if the report was accepted.
        /// </summary>
        public bool ReportProgress(string key, int amount)
        {
            if (status != TaskStatus.Active || task == null) return false;

            bool wasReported = false;
            for (int i = 0; i < task.objectives.Count; i++)
            {
                if (task.objectives[i].key == key)
                {
                    currentProgress[i] = Mathf.Clamp(currentProgress[i] + amount, 0, task.objectives[i].requiredAmount);
                    Debug.Log($"HostTaskManager on {gameObject.name}: Progress for '{key}' updated to {currentProgress[i]}/{task.objectives[i].requiredAmount}");
                    wasReported = true;
                    break;
                }
            }

            if (wasReported)
            {
                CheckCompletion();
                return true;
            }

            return false;
        }

        private void CheckCompletion()
        {
            if (task == null || status != TaskStatus.Active) return;

            for (int i = 0; i < task.objectives.Count; i++)
            {
                if (currentProgress[i] < task.objectives[i].requiredAmount) return;
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

            // Update dialogue to match status: 0=Inactive, 1=Active, 2=Completed
            if (dialogueManager != null)
            {
                GetComponent<DialogueManager>()?.UpdateIndex();
                Debug.Log($"{gameObject.name} Dialogue Index Updated");
            }

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
