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
        public List<ObjectiveData> objectives;
    }

    [System.Serializable]
    public class ObjectiveData
    {
        public string key; 
        public int requiredAmount;
    }
    
    [System.Serializable]
    public class RuntimeTask
    {
        public TaskData data;
        public List<int> currentAmounts;
        public TaskStatus status;

        public RuntimeTask(TaskData taskData)
        {
            data = taskData;
            status = TaskStatus.Active;
            currentAmounts = new List<int>();

            foreach (ObjectiveData objective in taskData.objectives)
            {
                currentAmounts.Add(0);
            }
        }

        public void IncrementProgress(string key, int amount)
        {
            if (data == null || data.objectives == null) return;
            
            if (currentAmounts == null || currentAmounts.Count != data.objectives.Count)
            {
                currentAmounts = new List<int>();
                foreach (var objective in data.objectives)
                {
                    currentAmounts.Add(0);
                }
            }

            for (int i = 0; i < data.objectives.Count; i++)
            {
                if (data.objectives[i].key == key)
                {
                    currentAmounts[i] += amount;
                
                    if (currentAmounts[i] > data.objectives[i].requiredAmount)
                        currentAmounts[i] = data.objectives[i].requiredAmount;
                }
            }
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            for (int i = 0; i < data.objectives.Count; i++)
            {
                if (currentAmounts[i] < data.objectives[i].requiredAmount)
                {
                    return;
                }
            }
            status = TaskStatus.Found;
        }
    }
#region TaskSystem States
    [System.Serializable]
    public enum TaskStatus
    {
        NotStarted,
        Active,
        Found,
        Completed,
    }
    [System.Serializable]
    public enum HostType
    {
        Giver,
        Closer
    }
#endregion
}