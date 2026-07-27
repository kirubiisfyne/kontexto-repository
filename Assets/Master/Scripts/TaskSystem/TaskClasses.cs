using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    [System.Serializable]
    public class TaskPrerequisite
    {
        [Tooltip("Optional. The task required before this one can be started.")]
        public TaskData task;

        [Tooltip("The minimum status the prerequisite task must reach to unlock this one. (e.g. Active allows starting as soon as the other task starts).")]
        public TaskStatus requiredStatus = TaskStatus.Completed;
    }

    [System.Serializable]
    public class TaskRequirements
    {
        [Tooltip("If true, objectives must be completed in the exact order they appear in the list below.")]
        public bool needsSequentialOrder = true;

        public List<ObjectiveData> objectives;
    }

    /// <summary>
    /// Represents a specific goal within a task.
    /// </summary>
    [System.Serializable]
    public class ObjectiveData
    {
        public string key; 
        
        [Tooltip("The text shown on the UI Notification (e.g., 'Collect the Library Books').")]
        public string notificationDisplayName;

        public int requiredAmount;
    }

    /// <summary>
    /// The lifecycle states of a task.
    /// </summary>
    [System.Serializable]
    public enum TaskStatus
    {
        Inactive,
        Active,
        ReadyToComplete,
        Completed
    }

    [System.Serializable]
    public enum HostType
    {
        Giver,
        Closer,
        Both
    }
}
