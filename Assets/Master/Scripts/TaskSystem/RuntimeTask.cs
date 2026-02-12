using System.Collections.Generic;
using UnityEngine;

[System.Serializable] // Makes it visible in the Inspector for debugging!
public class RuntimeTask
{
    public TaskData data; // Reference to the static info
    public List<int> currentAmounts; // Tracks progress for each objective
    public bool isCompleted;

    // Constructor: When we start a task, we set everything to 0
    public RuntimeTask(TaskData taskData)
    {
        data = taskData;
        isCompleted = false;
        currentAmounts = new List<int>();

        // Initialize counters for every objective in the data
        foreach (var obj in taskData.objectives)
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
        bool allDone = true;
        for (int i = 0; i < data.objectives.Count; i++)
        {
            if (currentAmounts[i] < data.objectives[i].requiredAmount)
            {
                allDone = false;
                break;
            }
        }
        isCompleted = allDone;
    }
}