using UnityEngine;

public class TaskManager : MonoBehaviour
{
    // The currently active task (The "Notepad")
    public RuntimeTask currentActiveTask;

    private void Awake()
    {
        // Check if we have a task object, but it's empty (no Data)
        if (currentActiveTask != null && currentActiveTask.data == null)
        {
            // Delete the ghost so the NPC sees we are free
            currentActiveTask = null;
        }
    }
    public void AcceptTask(TaskData taskData)
    {
        // Create a new runtime instance so we don't write to the SO
        currentActiveTask = new RuntimeTask(taskData);
        Debug.Log("Accepted Task: " + taskData.taskName);
    }

    // Call this from KeyItem.cs or EnemyDeath.cs
    public void ReportEvent(string objectiveKey, int amount = 1)
    {
        if (currentActiveTask == null || currentActiveTask.isCompleted) return;

        // Tell the task to update itself
        currentActiveTask.IncrementProgress(objectiveKey, amount);

        // Check if we just finished it
        if (currentActiveTask.isCompleted)
        {
            Debug.Log("Task Complete: " + currentActiveTask.data.taskName);
            // You could trigger UI effects here
        }
    }
}