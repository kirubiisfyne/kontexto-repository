using System;
using UnityEngine;
using Master.Scripts.DialogueSystem;
using Master.Scripts.TaskSystem;

namespace Master.Scripts
{
    public class UniversalInteractor : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactRange = 3f;
        public string interactButton = "Interact";
        
        [Tooltip("Optional: Only interact with specific layers to save performance.")]
        public LayerMask interactableLayer = ~0;
        
        public ClientTaskManager clientTaskManager;
        
        private void Update()
        {
            if (DialogueManager.IsConversationActive) 
            {
                return; 
            }

            if (Input.GetButtonDown(interactButton))
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            Collider[] hitColliders = Physics.OverlapSphere(
                transform.position, 
                interactRange, 
                interactableLayer, 
                QueryTriggerInteraction.Collide 
            );

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hitColliders)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>();
                
                if (interactable != null)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            if (closestInteractable != null)
            {
                closestInteractable.Interact(clientTaskManager);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
