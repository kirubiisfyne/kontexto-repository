using System;
using System.Collections;
using Master.Scripts.DialogueSystem;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class HostTaskManager : MonoBehaviour, IInteractable
    {
        [Header("Host Type")]
        public HostType hostType;
        
        // TODO Instead assigned by KeyItemInstance
        public TaskData heldTask;
        public TaskStatus hostTaskStatus;
        
        public DialogueManager dialogueManager;
        
        public TaskCycleManager taskCycleManager;
        public ClientTaskManager clientTaskManager;

        private void Awake()
        {
            dialogueManager = gameObject.GetComponent<DialogueManager>();
        }

        public void Interact(ClientTaskManager clientTaskManager)
        {
            this.clientTaskManager = clientTaskManager;
            StartCoroutine(dialogueManager.StartDialogueRoutine(clientTaskManager));
        }

        public void GiveTaskTo() => StartCoroutine(TryGiveTaskTo());
        private IEnumerator TryGiveTaskTo()
        {
            yield return new WaitUntil(() => !clientTaskManager.Equals(null));
            
            clientTaskManager.AcceptTask(heldTask);
            taskCycleManager.UpdateTaskStatus(clientTaskManager.currentActiveTask.status);
        }
        
        public void CompleteTask() => StartCoroutine(TryCompleteTask());
        public IEnumerator TryCompleteTask()
        {
            yield return new WaitUntil(() => !clientTaskManager.Equals(null));

            if (clientTaskManager.currentActiveTask.status == TaskStatus.Found)
            {
                taskCycleManager.UpdateTaskStatus(TaskStatus.Completed);            // If KeyItem found set Client RuntimeTask to TaskStatus.Completed
                clientTaskManager.currentActiveTask = null;                         // Clear Client RuntimeTask to allow accepting next task
            }
        }
    }
}