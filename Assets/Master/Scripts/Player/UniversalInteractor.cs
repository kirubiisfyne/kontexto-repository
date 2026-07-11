using System;
using UnityEngine;
using Master.Scripts.DialogueSystem;

namespace Master.Scripts
{
    /// <summary>
    /// Simple interactor that finds the nearest GameObject with IInteractable components and triggers all of them.
    /// Broadcasts an event when an interactable object is within range.
    /// </summary>
    public class UniversalInteractor : MonoBehaviour
    {
        [Header("Settings")]
        public float interactRange = 3f;
        public LayerMask interactableLayer = ~0;

        // --- NEW: Event to broadcast when we are near or far from an interactable
        public static event Action<bool> OnInteractableProximityChanged;
        
        private GameObject currentClosestInteractable = null;
        private bool isNearInteractable = false;

        private void Update()
        {
            // Prevent interaction if a dialogue is currently active
            if (DialogueManager.IsConversationActive)
            {
                if (isNearInteractable)
                {
                    isNearInteractable = false;
                    OnInteractableProximityChanged?.Invoke(false);
                }
                return;
            }

            // 1. Constantly check for the closest interactable in range
            CheckForInteractablesInRange();

            // 2. Interact if we press E and have something in range
            if (Input.GetKeyDown(KeyCode.E) && currentClosestInteractable != null)
            {
                InteractWithCurrent();
            }
        }

        private void CheckForInteractablesInRange()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);
            currentClosestInteractable = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hitColliders)
            {
                if (hit.GetComponent<IInteractable>() != null)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentClosestInteractable = hit.gameObject;
                    }
                }
            }

            // Determine if we currently have an interactable in range
            bool currentlyNearInteractable = (currentClosestInteractable != null);

            // Only invoke the event if the state actually changed
            if (currentlyNearInteractable != isNearInteractable)
            {
                isNearInteractable = currentlyNearInteractable;
                OnInteractableProximityChanged?.Invoke(isNearInteractable);
            }
        }

        private void InteractWithCurrent()
        {
            if (currentClosestInteractable != null)
            {
                IInteractable[] interactables = currentClosestInteractable.GetComponents<IInteractable>();
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
