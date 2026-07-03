using Master.Scripts.DialogueSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts.TaskSystem
{
    /// <summary>
    /// An object or event that reports progress to a specific HostTaskManager.
    /// </summary>
    public class KeyItemInstance : MonoBehaviour, IInteractable
    {
        [Header("Reporting Settings")]
        [Tooltip("The ID key matching an objective in the Giver's TaskData.")]
        public string itemKey;

        [Tooltip("The NPC/Manager that should receive this progress report. If left empty, it will auto-populate by finding the task that needs this itemKey.")]
        public HostTaskManager targetGiver;

        [Header("Interaction Settings")]
        public bool destroyOnInteract = true;
        [Tooltip("If true, automatically updates the DialogueManager index on this GameObject.")]
        public bool updateLocalIndex = false;

        [Header("Events")]
        [Tooltip("Fired ONLY if the Giver NPC accepts the report. Use this for SFX, VFX, or specific state changes.")]
        public UnityEvent onAcceptedReport;

        /// <summary>
        /// Reports progress and optionally disables the item or this script based on a "receipt" from the Giver.
        /// </summary>
        public void Interact()
        {
            if (!enabled) return;

            // Auto-populate targetGiver based on the itemKey
            if (targetGiver == null && !string.IsNullOrEmpty(itemKey))
            {
                var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
                foreach (var manager in allManagers)
                {
                    if ((manager.hostType == HostType.Giver || manager.hostType == HostType.Both) && manager.task != null && manager.task.requirements != null && manager.task.requirements.objectives != null)
                    {
                        foreach (var objective in manager.task.requirements.objectives)
                        {
                            if (objective.key == itemKey)
                            {
                                targetGiver = manager;
                                break;
                            }
                        }
                    }
                    if (targetGiver != null) break;
                }
            }

            if (targetGiver == null)
            {
                Debug.LogWarning($"KeyItemInstance on {gameObject.name}: Could not find any active task requiring the item key '{itemKey}'!");
                return;
            }

            // RECEIPT MODEL: Only retire if the Giver NPC actually accepted the report
            bool wasAccepted = targetGiver.ReportProgress(itemKey, 1);

            if (wasAccepted)
            {
                Retire();
            }
        }

        /// <summary>
        /// Forcibly retires the item (fires events, updates dialogue, destroys object).
        /// Used by LevelLoader to clean up the scene when restoring completed tasks.
        /// </summary>
        public void Retire()
        {
            // Fire custom events (SFX, Index updates, etc.)
            onAcceptedReport?.Invoke();

            // Sync local dialogue if it is an NPC acting as an item
            if (updateLocalIndex)
            {
                GetComponent<DialogueManager>()?.UpdateIndex();
            }

            if (destroyOnInteract)
            {
                gameObject.SetActive(false); 
            }
            else
            {
                this.enabled = false;
            }
        }
    }
}
