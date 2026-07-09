using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts
{
    [RequireComponent(typeof(KeyItemInstance))]
    public class AutoCollectPrintJob : MonoBehaviour
    {
        private void Start()
        {
            if (GameManager.Instance != null && GameManager.Instance.pendingDocumentSuccess)
            {
                GameManager.Instance.pendingDocumentSuccess = false; // Reset the flag
                
                var keyItem = GetComponent<KeyItemInstance>();
                if (keyItem != null)
                {
                    // If targetGiver is unassigned, fallback to finding it via the item key
                    HostTaskManager foundGiver = keyItem.targetGiver;
                    if (foundGiver == null && !string.IsNullOrEmpty(keyItem.itemKey))
                    {
                        var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
                        foreach (var manager in allManagers)
                        {
                            if (manager.task != null && manager.task.requirements != null)
                            {
                                foreach (var obj in manager.task.requirements.objectives)
                                {
                                    if (obj.key == keyItem.itemKey)
                                    {
                                        foundGiver = manager;
                                        break;
                                    }
                                }
                            }
                            if (foundGiver != null) break;
                        }
                    }

                    // If the scene reloaded, the task was likely reset to Inactive because the SaveSystem only tracks 'Completed' tasks.
                    // We must forcefully resume it to Active before reporting progress!
                    if (foundGiver != null && foundGiver.status == TaskStatus.Inactive)
                    {
                        Debug.Log("AutoCollectPrintJob: Force-resuming task state to Active before collection.");
                        foundGiver.StartTask();
                    }
                    
                    Debug.Log("AutoCollectPrintJob: Automatically collecting KeyItem based on GameManager success flag.");
                    keyItem.enabled = true; // Force enable so it doesn't return early
                    keyItem.Interact();
                }
            }
        }
    }
}
