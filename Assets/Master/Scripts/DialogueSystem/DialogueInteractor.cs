using UnityEngine;

public class DialogueInteractor : MonoBehaviour
{
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactRange);
    
        foreach (var hit in hitColliders)
        {
            // Check for the interface on the object we hit
            IInteractable interactable = hit.GetComponent<IInteractable>();
        
            if (interactable != null)
            {
                interactable.Interact();
                return; // Stop after finding the first one
            }
        }
    }   

    // Visualization for Debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        // This function is actually the same for 3D, but it will now draw a 3D sphere
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}