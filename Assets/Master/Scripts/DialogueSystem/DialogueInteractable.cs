using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using UnityEngine.Events;

namespace Master.Scripts.DialogueSystem
{
    public class DialogueInteractable : MonoBehaviour, IInteractable
    {
    #region Conversation Data Setup
        [System.Serializable]
        public struct ConversationEvent
        {
            public string name;
            public Conversation conversation;
            public TaskStatus status;
            [Space(15)]
            public UnityEvent onConversationEnd;
        }
        
        [Header("Scene Events")]
        [SerializeField] private List<ConversationEvent> conversationEvents;
        private ConversationEvent conversationEvent;
        
        private readonly Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
        
        private bool isTalking;
        private static bool isConversationActive;
        private TaskStatus status = TaskStatus.NotStarted;
    #endregion


        public void Interact(GameObject player)
        {
            if (isConversationActive) return;

            StartCoroutine(StartDialogueRoutine(player));
        }
        private IEnumerator StartDialogueRoutine(GameObject player)
        {
            TaskManager manager = player.GetComponent<TaskManager>();

            if (manager.currentActiveTask != null)
            {
                status = manager.currentActiveTask.status;
            }
            
            foreach (ConversationEvent c in conversationEvents)
            {
                if (c.status == status)
                {
                    conversationEvent = c;
                    Debug.Log(c.name);
                }
            }
            isConversationActive = true;

            yield return null; 

            isTalking = true;
            linesQueue.Clear();

            foreach (DialogueLine line in conversationEvent.conversation.lines)
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

            DialogueUI.Instance.UpdateDialogueView(currentLine);
        }

        private void EndDialogue()
        {
            isTalking = false;
            
            DialogueUI.Instance.Hide();
            
            conversationEvent.onConversationEnd?.Invoke();

            StartCoroutine(UnlockConversation());
        }

        private IEnumerator UnlockConversation()
        {
            yield return null;
            isConversationActive = false;
        }
    }
}
