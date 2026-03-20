using System;
using Master.Scripts.DialogueSystem;
using Unity.VisualScripting;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class TaskGiver : MonoBehaviour
    {
        public TaskData taskToGive;
        
        private GameObject playerGameObject;
        private DialogueManager dialogueManager;

        private void Awake()
        {
            dialogueManager = gameObject.GetComponent<DialogueManager>();
        }

        public void GiveTaskTo()
        {
            playerGameObject =  dialogueManager.player;
            TaskManager manager = playerGameObject.GetComponent<TaskManager>();
            
            // Execute with onConversationEnd event with NotActive status
            if (manager == null) return;
            
            if (manager.currentActiveTask != null)
            {
                Debug.Log($"Player is busy with another task!");
                return;
            }

            manager.AcceptTask(taskToGive);
            Debug.Log($"NPC gave task: {taskToGive.taskName}");
        }
    }
}