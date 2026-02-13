using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    [CreateAssetMenu(fileName = "New Task", menuName = "Tasks/Task")]
    public class TaskData : ScriptableObject
    {
        [Header("Info")]
        public string taskName;
        [TextArea] public string description;

        [Header("Requirements")]
        // This list defines what needs to be done.
        // We use a simple class here so we can see it in the Inspector.
        public List<ObjectiveData> objectives;
    }

    [System.Serializable]
    public class ObjectiveData
    {
        // The "Key" acts as the ID. e.g., "Rat", "BlueGem", "TalkToChief"
        public string key; 
        public int requiredAmount;
    }

    [System.Serializable]
    public enum TaskStatus
    {
        NotStarted, // Or Generic status, conversation with no relation to any tasks
        Active,
        Found,
        Completed,
    }
}