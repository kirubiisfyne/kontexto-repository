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

        [Header("Player Initial Transform (Optional)")]
        [Tooltip("If true, positions the player at this location when starting/spawning for this task.")]
        public bool setPlayerInitialTransform;
        [Tooltip("World position to place the player.")]
        public Vector3 playerInitialPosition;
        [Tooltip("World rotation (Euler angles) for the player.")]
        public Vector3 playerInitialRotation;

        [Header("Document Data (Optional)")]
        [Tooltip("If this task requires editing a document, assign its JSON file here.")]
        public TextAsset documentData;
    }
}
