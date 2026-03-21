using System;
using System.Collections;
using Master.Scripts.DialogueSystem;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class HostTaskManager : MonoBehaviour
    {
        [Header("Type")]
        public HostType hostType;
        
        [HideInInspector] public TaskData heldTask;
        [HideInInspector] public TaskStatus hostTaskStatus;
        
        [HideInInspector] public DialogueManager dialogueManager;
        [HideInInspector] public TaskCycleManager taskCycleManager;

        private void Awake()
        {
            dialogueManager = gameObject.GetComponentInParent<DialogueManager>();
        }

        public void GiveTaskTo() => StartCoroutine(TryGiveTaskTo());
        private IEnumerator TryGiveTaskTo()
        {
            yield return new WaitUntil(() => !dialogueManager.clientTaskManager.Equals(null));
            
            dialogueManager.clientTaskManager.AcceptTask(heldTask);
            taskCycleManager.UpdateTaskStatus(dialogueManager.clientTaskManager.currentActiveTask.status);
        }
        
        public void CompleteTask() => StartCoroutine(TryCompleteTask());
        private IEnumerator TryCompleteTask()
        {
            yield return new WaitUntil(() => !dialogueManager.clientTaskManager.Equals(null));

            if (dialogueManager.clientTaskManager.currentActiveTask.status.Equals(TaskStatus.Found))
            {
                taskCycleManager.UpdateTaskStatus(TaskStatus.Completed);            // If KeyItem found set Client RuntimeTask to TaskStatus.Completed
                dialogueManager.clientTaskManager.currentActiveTask = null;                         // Clear Client RuntimeTask to allow accepting next task
            }
        }
    }
}