using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class ClientTaskManager : MonoBehaviour
    {
        public RuntimeTask currentActiveTask;

        private void Awake()
        {
            if (currentActiveTask != null && currentActiveTask.data == null)
                currentActiveTask = null;
        }
        public void AcceptTask(TaskData taskData)
        {
            currentActiveTask = new RuntimeTask(taskData);
            Debug.Log($"Accepted task {taskData.name}");
        }

        public void ReportEvent(string objectiveKey, int amount)
        {
            if (currentActiveTask == null || currentActiveTask.status == TaskStatus.Found) return;
            currentActiveTask.IncrementProgress(objectiveKey, amount);
        }
    }
}