using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    /// <summary>
    /// Defines a task's information and its requirements.
    /// </summary>
    [CreateAssetMenu(fileName = "New Task", menuName = "Tasks/Task")]
    public class TaskData : ScriptableObject
    {
        [Header("Information")]
        public string taskName;
        [TextArea] public string description;

        [Header("Requirements")]
        public List<ObjectiveData> objectives;
    }

    /// <summary>
    /// Represents a specific goal within a task.
    /// </summary>
    [System.Serializable]
    public class ObjectiveData
    {
        public string key; 
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
        Completed
    }

    [System.Serializable]
    public enum HostType
    {
        Giver,
        Closer
    }
}
