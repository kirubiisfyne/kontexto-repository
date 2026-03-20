using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Master.Scripts.TaskSystem;
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
            public DialogueClasses dialogueClasses;
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
    
    #region Dialogue and Task Systems Connection
        // Task and Dialogue Systems connections
        private TaskGiver taskGiver;
        private TaskTurnIn taskTurnIn;
        private TaskData taskData;
        
        // NPC Status
        private TaskStatus status = TaskStatus.NotStarted;
        
        // Player Reference
        [Header("Dialogue Configuration")]
        public GameObject player;
        private TaskManager manager;
        #endregion
    
    #region Dialogue Setup

    private void Awake()
        {
            GetNpcTaskManager();
        }
        private void GetNpcTaskManager()
        {
            // Get necessary data from the respective manager
            if (GetComponent<TaskGiver>() != null)
            {
                taskGiver = GetComponent<TaskGiver>();
                taskData = taskGiver.taskToGive;
                Debug.Log(taskGiver.taskToGive.name);
            }
            else if (GetComponent<TaskTurnIn>() != null)
            {
                taskTurnIn = GetComponent<TaskTurnIn>();
                taskData = taskTurnIn.taskToReceive;
                Debug.Log(taskTurnIn.taskToReceive.name);
            }
            else
            {
                Debug.Log("No NpcTaskManager found!");
            }
        }
    #endregion
    
        public void Interact(GameObject playerGameObject)
        {
            if (IsConversationActive) return;
            
            GetPlayer(playerGameObject);
            StartCoroutine(StartDialogueRoutine());
        }
        
        private IEnumerator StartDialogueRoutine()
        {
            // Wait until manager is assigned
            yield return new WaitUntil(() => manager != null);

            // Check which conversation to use based on NPC status
            foreach (ConversationEvent conversation in conversations)
            {
                if (conversation.status == status)
                {
                    activeConversation = conversation;
                    Debug.Log($"Current Conversation Title: {activeConversation.name}");
                }
            }
            IsConversationActive = true;
            
            // End coroutine at the last frame
            yield return null; 

            isTalking = true;
            linesQueue.Clear();
            
            // Reload dialogue lines queue
            foreach (DialogueLine line in activeConversation.dialogueClasses.lines)
            {
                linesQueue.Enqueue(line);
            }

            DisplayNextLine();
        }

        public void DisplayNextLine()
        {
            // Handle fast-forward
            if (DialogueUIManager.Instance.IsTyping)
            {
                DialogueUIManager.Instance.FinishTyping();
                return;
            }

            // Check if there are lines in queue.
            if (linesQueue.Count == 0)
            {
                EndDialogue();
                return;
            }

            // Update UI
            DialogueLine currentLine = linesQueue.Dequeue();

            DialogueUIManager.Instance.UpdateDialogueView(currentLine);
        }

        private void EndDialogue()
        {
            isTalking = false;
            
            DialogueUIManager.Instance.Hide();
            
            activeConversation.onConversationEnd?.Invoke();
            UpdateNpcStatus();
            StartCoroutine(UnlockConversation());
        }

        private void UpdateNpcStatus()
        {
            // Change NPC status
            if (manager.currentActiveTask != null)
            {
                // Only change status when Player active Task is the same as the NPCs
                if (manager.currentActiveTask.data == taskData)
                {
                    status = manager.currentActiveTask.status;
                }
            }
        }

        private IEnumerator UnlockConversation()
        {
            yield return null;
            IsConversationActive = false;
        }
        
        private void Update()
        {
            // Checking if player pressed "Interact" to display next line
            if (isTalking)
            {
                if (Input.GetButtonDown("Interact"))
                {
                    DisplayNextLine();
                }
            }
        }

        private void GetPlayer(GameObject _playerGameObject)
        {
            player = _playerGameObject;
            manager = player.GetComponent<TaskManager>();
        }
    }
}
