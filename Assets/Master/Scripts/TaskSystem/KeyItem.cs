using Master.Scripts.DialogueSystem;
using UnityEngine;

namespace Master.Scripts.TaskSystem
{
    public class KeyItem : MonoBehaviour, IInteractable
    {
        public string itemKey; // Set this to "Wood" in Inspector
        
        private TaskManager manager;

        public void Interact(GameObject playerGameObject)
        {
            manager = playerGameObject.GetComponent<TaskManager>();
            
            if (manager == null) return;
            
            if (manager.currentActiveTask == null)
            {
                return;
            }
        
            if (manager.currentActiveTask.status == TaskStatus.Active)
            {
                if (manager != null && manager.currentActiveTask != null)
                {
                    manager.ReportEvent(itemKey, 1);
                    Destroy(gameObject);
                }
            }
        }
    }
}