using UnityEngine;
using Master.Scripts.DialogueSystem;

namespace Master.Scripts
{
    /// <summary>
    /// Simple interactor that finds the nearest GameObject with IInteractable components and triggers all of them.
    /// </summary>
    public class UniversalInteractor : MonoBehaviour
    {
        [Header("Settings")]
        public float interactRange = 3f;
        public LayerMask interactableLayer = ~0;

        private void Update()
        {
            // Prevent interaction if a dialogue is currently active
            if (DialogueManager.IsConversationActive) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);
            GameObject closestObject = null;
            float closestDistance = float.MaxValue;

            // Find the closest GameObject that has at least one IInteractable component
            foreach (var hit in hitColliders)
            {
                if (hit.GetComponent<IInteractable>() != null)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestObject = hit.gameObject;
                    }
                }
            }

            // Trigger all IInteractable components on the closest object
            if (closestObject != null)
            {
                IInteractable[] interactables = closestObject.GetComponents<IInteractable>();
                foreach (var interactable in interactables)
                {
                    interactable.Interact();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}
