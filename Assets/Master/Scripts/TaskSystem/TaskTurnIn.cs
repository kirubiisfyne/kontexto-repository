using System;
using Master.Scripts.DialogueSystem;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class TaskTurnIn : MonoBehaviour
    {
        public TaskData taskToReceive;
        
        private GameObject playerGameObject;
        private DialogueManager dialogueManager;
        
        private void Awake()
        {
            dialogueManager = gameObject.GetComponent<DialogueManager>();
        }

        public void TryCompleteTask()
        {
            playerGameObject = dialogueManager.player;
            TaskManager manager =  playerGameObject.GetComponent<TaskManager>();
            
            // Execute with onConversationEnd event with Found status
            Debug.Log($"Player is turning in {manager.currentActiveTask.data.taskName}");

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
                manager.currentActiveTask = null;
            }
            else
            {
                Debug.Log("Come back when you are finished!");
            }
        }
    
    
    }
}