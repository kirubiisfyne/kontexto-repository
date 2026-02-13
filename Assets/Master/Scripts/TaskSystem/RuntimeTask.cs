using System.Collections.Generic;

namespace Master.Scripts.TaskSystem
{
    [System.Serializable] // Makes it visible in the Inspector for debugging!
    public class RuntimeTask
    {
        public TaskData data; // Reference to the static info
        public List<int> currentAmounts; // Tracks progress for each objective
        public TaskStatus status;

        // Constructor: When we start a task, we set everything to 0
        public RuntimeTask(TaskData taskData)
        {
            data = taskData;
            status = TaskStatus.Active;
            currentAmounts = new List<int>();

            // Initialize counters for every objective in the data
            foreach (var unused in taskData.objectives)
            {
                currentAmounts.Add(0);
            }
        }

        // Helper function to check if specific objective is done
        public void IncrementProgress(string key, int amount)
        {
            for (int i = 0; i < data.objectives.Count; i++)
            {
                if (data.objectives[i].key == key)
                {
                    currentAmounts[i] += amount;
                
                    // Cap the amount so it doesn't go over (optional)
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
                    break;
                }
            }
            status = TaskStatus.Found;
        }
    }
}