using UnityEngine;
using System.Collections; // Needed for IEnumerator
using System.Collections.Generic;
using UnityEngine.Events;

public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Conversation conversation;
    
    private Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
    private bool isTalking = false;

    public static bool isConversationActive = false;
    
    public UnityEvent onConversationEnd;

    public void Interact()
    {
        // Prevent starting if another NPC is already talking
        if (isConversationActive) return;

        // Use a Coroutine to wait 1 frame before enabling input
        StartCoroutine(StartDialogueRoutine());
    }

    private IEnumerator StartDialogueRoutine()
    {
        isConversationActive = true;

        // CRITICAL FIX: Wait for end of frame
        // This prevents the "Interact" button press from instantly skipping the first line
        yield return null; 

        isTalking = true;
        linesQueue.Clear();

        foreach (DialogueLine line in conversation.lines)
        {
            linesQueue.Enqueue(line);
        }

        DisplayNextLine();
    }

    private void Update()
    {
        // Only listen for input if THIS specific NPC is the one talking
        if (isTalking)
        {
            if (Input.GetButtonDown("Interact"))
            {
                DisplayNextLine();
            }
        }
    }

    public void DisplayNextLine()
    {
        // 1. Handle "Fast Forward" - Check directly against the UI manager
        if (DialogueUI.Instance.IsTyping)
        {
            DialogueUI.Instance.FinishTyping();
            return; // STOP here so we don't load the next line yet
        }

        // 2. Check if we have more lines
        if (linesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        // 3. Get data and tell UI to show it
        DialogueLine currentLine = linesQueue.Dequeue();
        
        currentLine.onLineStart?.Invoke(); 

        DialogueUI.Instance.UpdateDialogueView(currentLine);
    }

    private void EndDialogue()
    {
        isTalking = false;
        
        DialogueUI.Instance.Hide();
        
        conversation.onConversationEnd?.Invoke();
        onConversationEnd?.Invoke();

        StartCoroutine(UnlockConversation());
    }

    private IEnumerator UnlockConversation()
    {
        yield return null;
        isConversationActive = false;
    }
}