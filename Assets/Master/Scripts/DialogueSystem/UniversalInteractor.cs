using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    public class UniversalInteractor : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private string interactButton = "Interact";
        
        [Tooltip("Optional: Only interact with specific layers to save performance.")]
        [SerializeField] private LayerMask interactableLayer = ~0; // '~0' means 'Everything'

        private void Update()
        {
            // Keep your global lock! Even though this is universal, 
            // we don't want the player pulling levers while trapped in a conversation.
            if (DialogueInteractable.isConversationActive) 
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
            // 1. THE TRIGGER FIX: 'QueryTriggerInteraction.Collide' tells Unity to detect Triggers!
            Collider[] hitColliders = Physics.OverlapSphere(
                transform.position, 
                interactRange, 
                interactableLayer, 
                QueryTriggerInteraction.Collide 
            );

            IInteractable closestInteractable = null;
            float closestDistance = float.MaxValue;

            // 2. THE QUALITY OF LIFE UPGRADE: Find the closest interactable object
            foreach (var hit in hitColliders)
            {
                IInteractable interactable = hit.GetComponent<IInteractable>();
                
                if (interactable != null)
                {
                    // Calculate how far away this specific object is
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    
                    // If it's the closest one we've found so far, remember it
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestInteractable = interactable;
                    }
                }
            }

            // 3. EXECUTE: Only interact with the single closest object
            if (closestInteractable != null)
            {
                closestInteractable.Interact(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
