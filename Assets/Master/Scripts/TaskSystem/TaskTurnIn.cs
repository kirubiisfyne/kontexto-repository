using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class TaskTurnIn : MonoBehaviour
    {
    
        [Header("Debug Settings")]
        public bool giveOnTouch = true; // Uncheck this if you want to use a Dialogue System instead
     
        // Simple trigger to turn in the quest
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && giveOnTouch)
            {
                TryCompleteQuest(other.gameObject);
            }
        }

        private void TryCompleteQuest(GameObject player)
        {
            TaskManager manager = player.GetComponent<TaskManager>();

            if (manager == null) return;

            // Check 1: Do they have a task?
            if (manager.currentActiveTask == null)
            {
                Debug.Log("You don't have a quest to turn in.");
                return;
            }

            // Check 2: Is the task actually done?
            if (manager.currentActiveTask.status == TaskStatus.Found)
            {
                Debug.Log($"QUEST TURNED IN: {manager.currentActiveTask.data.taskName}");
            
                // TODO: Give Rewards (Gold, XP, Items) here!
            
                // Clear the task so they can accept a new one
                manager.currentActiveTask.status = TaskStatus.Completed; 
            }
            else
            {
                Debug.Log("Come back when you are finished!");
            }
        }
    
    
    }
}