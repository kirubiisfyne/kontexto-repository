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
        [Tooltip("Unique, stable identifier for save/load. Set once, never rename.")]
        public string taskId;
        public string taskName;
        [Space(10)]
        [TextArea] public string description;
        [Space(10)]
        public TaskPrerequisite prerequisite;
        public TaskRequirements requirements;
    }
}
