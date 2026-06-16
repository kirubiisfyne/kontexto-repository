using Master.Scripts.DialogueSystem;
using UnityEngine;

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
        
        [Tooltip("The NPC/Manager that should receive this progress report.")]
        public HostTaskManager targetGiver;

        [Header("Interaction Settings")]
        public bool destroyOnInteract = true;
        [Tooltip("If true, automatically updates the DialogueManager index on this GameObject.")]
        public bool updateLocalIndex = false;

        /// <summary>
        /// Reports progress and optionally disables the item or this script based on a "receipt" from the Giver.
        /// </summary>
        public void Interact()
        {
            if (!enabled) return;

            if (targetGiver == null)
            {
                Debug.LogWarning($"KeyItemInstance on {gameObject.name}: No targetGiver assigned!");
                return;
            }

            // RECEIPT MODEL: Only retire if the Giver NPC actually accepted the report
            bool wasAccepted = targetGiver.ReportProgress(itemKey, 1);
            if (wasAccepted)
            {
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
}
