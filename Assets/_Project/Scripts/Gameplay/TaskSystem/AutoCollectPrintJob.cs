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

                    // If targetGiver is a Closer, resolve the actual Giver/Both manager
                    if (foundGiver != null && foundGiver.hostType == HostType.Closer)
                    {
                        var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
                        foreach (var manager in allManagers)
                        {
                            if (manager.task == foundGiver.task && (manager.hostType == HostType.Giver || manager.hostType == HostType.Both))
                            {
                                foundGiver = manager;
                                break;
                            }
                        }
                    }

                    // If the scene reloaded, the task was likely reset to Inactive because the SaveSystem only tracks 'Completed' tasks.
                    // We must forcefully resume it to Active before reporting progress!
                    if (foundGiver != null && foundGiver.status == TaskStatus.Inactive)
                    {
                        //Debug.Log("AutoCollectPrintJob: Force-resuming task state to Active before collection.");
                        foundGiver.StartTask();
                    }

                    // If the task enforces sequential order, restore any objectives preceding this itemKey
                    // to full, since scene reloads wipe in-progress task objective counters.
                    if (foundGiver != null && foundGiver.task != null && foundGiver.task.requirements != null && foundGiver.task.requirements.needsSequentialOrder)
                    {
                        var objectives = foundGiver.task.requirements.objectives;
                        for (int i = 0; i < objectives.Count; i++)
                        {
                            if (objectives[i].key == keyItem.itemKey) break;
                            if (foundGiver.currentProgress != null && i < foundGiver.currentProgress.Count)
                            {
                                foundGiver.currentProgress[i] = objectives[i].requiredAmount;
                            }
                        }
                    }
                    
                    //Debug.Log("AutoCollectPrintJob: Automatically collecting KeyItem based on GameManager success flag.");
                    keyItem.enabled = true; // Force enable so it doesn't return early
                    keyItem.Interact();
                }
            }
        }
    }
}
