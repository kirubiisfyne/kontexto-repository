using UnityEngine;

namespace Master.Scripts.TaskSystem
{
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
            currentActiveTask = new RuntimeTask(taskData);
            Debug.Log("Accepted Task: " + taskData.taskName);
        }

        public void ReportEvent(string objectiveKey, int amount)
        {
            if (currentActiveTask == null || currentActiveTask.status == TaskStatus.Found) return;

            currentActiveTask.IncrementProgress(objectiveKey, amount);

            if (currentActiveTask.status == TaskStatus.Found)
            {
                Debug.Log("Task Complete: " + currentActiveTask.data.taskName);
            }
        }
    }
}