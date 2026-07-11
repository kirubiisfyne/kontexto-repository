using UnityEngine;

namespace Master.Scripts.UI
{
    // Automatically requires an Animator on the same GameObject (the Root)
    [RequireComponent(typeof(Animator))]
    public class InteractPromptUI : MonoBehaviour
    {
        private Animator animator;
        
        // Caching the parameter hash is slightly more performant than using the string name
        private readonly int isVisibleHash = Animator.StringToHash("IsVisible");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            
            // Ensure the animator starts in the correct state
            if (animator != null)
            {
                animator.SetBool(isVisibleHash, false);
            }
        }

        private void OnEnable()
        {
            // Subscribe to the event when this script is enabled
            UniversalInteractor.OnInteractableProximityChanged += HandleProximityChanged;
        }

        private void OnDisable()
        {
            // Unsubscribe from the event when disabled to prevent memory leaks
            UniversalInteractor.OnInteractableProximityChanged -= HandleProximityChanged;
        }

        private void HandleProximityChanged(bool isNearInteractable)
        {
            // Tell the Animator to handle the transition
            if (animator != null)
            {
                animator.SetBool(isVisibleHash, isNearInteractable);
            }
        }
    }
}
