using UnityEngine;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Conversation conversation;

    public void Interact()
    {
        // When interacted with, tell the Manager to start this conversation
        if (DialogueManager.Instance.dialoguePanel.activeInHierarchy)
        {
            DialogueManager.Instance.DisplayNextLine();
        }
        else
        {
            DialogueManager.Instance.StartConversation(conversation);
            Debug.unityLogger.Log("Dialogue Interact");
        }
    }
}