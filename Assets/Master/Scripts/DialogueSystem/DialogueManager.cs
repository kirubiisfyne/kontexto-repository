using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using Unity.VisualScripting;
using UnityEngine.Events;

namespace Master.Scripts.DialogueSystem
{
    public class DialogueManager : MonoBehaviour, IInteractable
    {
    #region Dialogue Data Setup
        // Conversation Data setup
        [System.Serializable]
        public struct ConversationEvent
        {
            public string name;
            public Conversation conversation;
            public TaskStatus status;
            [Space(15)]
            public UnityEvent onConversationEnd;
        }
        
        // Conversation data variables
        [Header("Dialogue Data")]
        public List<ConversationEvent> conversations;
        private ConversationEvent activeConversation;
        private readonly Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
        
        private bool isTalking;
        public static bool IsConversationActive;
    #endregion
    
        [HideInInspector] public HostTaskManager hostTaskManager;
        [HideInInspector] public ClientTaskManager clientTaskManager;
        private TaskData taskData;

        private void Awake()
        {
            hostTaskManager = GetComponentInChildren<HostTaskManager>();
        }

        public void Interact(ClientTaskManager clientTaskManager)
        {
            this.clientTaskManager = clientTaskManager;
            
            // TODO Fix to avoid taking another task
            StartCoroutine(StartDialogueRoutine());
        }
        
        public IEnumerator StartDialogueRoutine()
        {
            yield return new WaitUntil(() => !clientTaskManager.Equals(null));

            foreach (ConversationEvent conversation in conversations)
                if (conversation.status == hostTaskManager.hostTaskStatus)
                    activeConversation = conversation; // Pick Dialogues based on HostTaskManager's status
            
            IsConversationActive = true;
            
            yield return null; 

            isTalking = true;
            linesQueue.Clear();
            
            foreach (DialogueLine line in activeConversation.conversation.lines)
                linesQueue.Enqueue(line);
            
            DisplayNextLine();
        }

        public void DisplayNextLine()
        {
            if (DialogueUIManager.Instance.IsTyping)
            {
                DialogueUIManager.Instance.FinishTyping();
                return;
            }

            if (linesQueue.Count == 0)
            {
                EndDialogue();
                return;
            }

            DialogueLine currentLine = linesQueue.Dequeue();
            DialogueUIManager.Instance.UpdateDialogueView(currentLine);
        }

        private void EndDialogue()
        {
            isTalking = false;
            
            DialogueUIManager.Instance.Hide();
            
            activeConversation.onConversationEnd?.Invoke();
            StartCoroutine(UnlockConversation());
        }

        private IEnumerator UnlockConversation()
        {
            yield return null;
            IsConversationActive = false;
        }
        
        private void Update()
        {
            if (isTalking && Input.GetButtonDown("Interact")) DisplayNextLine();
        }
    }
}
