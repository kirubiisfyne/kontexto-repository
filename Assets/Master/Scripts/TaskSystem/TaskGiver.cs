using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class TaskGiver : MonoBehaviour
    {
        [Header("Configuration")]
        public TaskData taskToGive; // Drag your ScriptableObject here in Inspector

        [Header("Debug Settings")]
        public bool giveOnTouch = true; // Uncheck this if you want to use a Dialogue System instead

        private void OnTriggerEnter(Collider other)
        {
            if (giveOnTouch && other.CompareTag("Player"))
            {
                GiveTaskTo(other.gameObject);
            }
        }

        public void GiveTaskTo(GameObject player)
        {
            Debug.Log("Player is attempting to take a task!");
            TaskManager manager = player.GetComponent<TaskManager>();

            if (manager != null)
            {
                Debug.Log(manager.currentActiveTask);
                // Optional: Check if they already have a task?
                if (manager.currentActiveTask != null)
                {
                    Debug.Log("Player is busy with another task!");
                    return;
                }

                manager.AcceptTask(taskToGive);
                Debug.Log($"NPC gave task: {taskToGive.taskName}");
            }
            else
            {
                Debug.LogError("Player object is missing the TaskManager script!");
            }
        }
    }
}