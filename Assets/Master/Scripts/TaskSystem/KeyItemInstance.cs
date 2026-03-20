using System.Collections;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class KeyItemInstance : MonoBehaviour, IInteractable
    {
        public string itemKey;
        
        public TaskCycleManager taskCycleManager;
        public ClientTaskManager clientTaskManager;

        public void Interact(ClientTaskManager clientTaskManager)
        {
            this.clientTaskManager = clientTaskManager;
            StartCoroutine(TryInteract());
        }

        private IEnumerator TryInteract()
        {
            yield return new WaitUntil(() => !clientTaskManager.Equals(null));
            
            clientTaskManager.ReportEvent(itemKey, 1);
            if (clientTaskManager.currentActiveTask.status.Equals(TaskStatus.Found))
                taskCycleManager.UpdateTaskStatus(clientTaskManager.currentActiveTask.status);
        }
    }
}