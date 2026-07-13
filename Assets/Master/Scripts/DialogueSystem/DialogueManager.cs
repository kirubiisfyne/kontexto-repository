using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// Pure dialogue playback system. Automatically populates conversations from an assigned JSON file.
    /// </summary>
    public class DialogueManager : MonoBehaviour, IInteractable
    {
        public static DialogueManager LastInteracted;
        public static bool IsConversationActive;

        /// <summary>
        /// Fired when a dialogue session begins. Passes the index of the conversation being played.
        /// </summary>
        public event Action<int> OnConversationStarted;

        /// <summary>
        /// Fired when a dialogue session ends. Passes the index of the conversation that was played.
        /// </summary>
        public event Action<int> OnConversationEnded;

        private const string FALLBACK_JSON = "{\"conversationMap\": [{\"name\": \"Fallback\",\"lines\": [{\"speaker\": \"System\",\"text\": \"Dialogue missing. Check JSON configuration.\"}]}]}";

        [Header("Dialogue Content")]
        [Tooltip("This list is automatically populated from the JSON file at runtime.")]
        public List<Conversation> conversations = new List<Conversation>();
        public int currentConversationIndex = 0;

        [Header("Data Assets")]
        public TextAsset dialogueJson;

        [Header("Playback Settings")]
        [Tooltip("If true, updating past the last conversation loops back to the first. If false, it stays on the last conversation.")]
        public bool loopConversations = false;

        private Conversation activeConversation;
        private int lastPlayedIndex;
        private readonly Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
        private bool isTalking;

        public Conversation idleConversation;
        private int overrideFrame = -1;

        private void Awake()
        {
            if (dialogueJson != null)
            {
                Debug.Log($"DialogueManager on {gameObject.name}: Parsing JSON '{dialogueJson.name}'...");
                LoadFromJSON(dialogueJson.text);
            }
        }

        private void Start()
        {
            // Auto-inject feedback from Editor if this is the Adviser
            if (dialogueJson != null && dialogueJson.name.ToLower().Contains("adviser") && 
                GameManager.Instance != null && GameManager.Instance.pendingAdviserFeedback != null && GameManager.Instance.pendingAdviserFeedback.Count > 0)
            {
                // Index 2 is assumed to be the "Completed" conversation branch
                InjectDynamicLines(2, GameManager.Instance.pendingAdviserFeedback, "Mrs. Santos");
                GameManager.Instance.pendingAdviserFeedback.Clear();
            }
        }

        #region Public API

        public void UseIdleDialogue()
        {
            overrideFrame = Time.frameCount;
            LastInteracted = this;

            if (idleConversation == null || IsConversationEmpty(idleConversation))
            {
                Debug.LogWarning($"DialogueManager on {gameObject.name}: No idle dialogue configured.");
                return;
            }

            activeConversation = idleConversation;
            lastPlayedIndex = -1;

            StopAllCoroutines();
            StartCoroutine(StartDialogueRoutine());
        }

        public void Interact()
        {
            if (overrideFrame == Time.frameCount) return;

            LastInteracted = this;

            // SNAPSHOT: Grab the conversation branch IMMEDIATELY.
            // This ensures we capture the state AT THE MOMENT of interaction,
            // preventing other scripts from skipping the story sequence.
            if (currentConversationIndex >= 0 && currentConversationIndex < conversations.Count)
            {
                activeConversation = conversations[currentConversationIndex];
                lastPlayedIndex = currentConversationIndex;
            }

            StartCoroutine(StartDialogueRoutine());
        }

        /// <summary>
        /// Moves to the next conversation branch in the loaded list.
        /// </summary>
        public void UpdateIndex()
        {
            if (conversations.Count > 0)
            {
                if (currentConversationIndex < conversations.Count - 1)
                {
                    // Move to the next index safely
                    currentConversationIndex++;
                }
                else if (loopConversations)
                {
                    // We are at the end, and looping is enabled
                    currentConversationIndex = 0;
                }
                // If we are at the end and looping is FALSE, do nothing (stops at the last index)
            }
        }

        public static void GlobalUpdateIndex()
        {
            if (LastInteracted != null) LastInteracted.UpdateIndex();
        }

        public void SetIndex(int index)
        {
            currentConversationIndex = Mathf.Clamp(index, 0, conversations.Count - 1);
        }

        public void InjectDynamicLines(int index, List<string> newLines, string speakerName = "Mrs. Santos")
        {
            if (index < 0 || index >= conversations.Count) return;
            var branch = conversations[index];
            
            int insertIndex = branch.lines.FindIndex(l => l.text == "[FEEDBACK]");
            if (insertIndex != -1)
            {
                branch.lines.RemoveAt(insertIndex);
            }
            else
            {
                insertIndex = branch.lines.Count;
            }
            
            foreach (var text in newLines)
            {
                branch.lines.Insert(insertIndex, new DialogueLine { speaker = speakerName, text = text });
                insertIndex++;
            }
        }

        #endregion

        #region Core Logic

        private IEnumerator StartDialogueRoutine()
        {
            // 1. Broadcast the START event. 
            // Note: Since we snapped the index in Interact(), extensions firing here 
            // will update the index for NEXT time, but won't affect this talk session.
            OnConversationStarted?.Invoke(lastPlayedIndex);

            // 2. Fallback check: if the snapshot taken in Interact() is invalid, use hardcoded fallback
            if (IsConversationEmpty(activeConversation))
            {
                var map = JsonUtility.FromJson<ConversationMap>(FALLBACK_JSON);
                activeConversation = map.conversationMap[0];
                lastPlayedIndex = -1; // Fallback doesn't count for indexed events
            }

            IsConversationActive = true;
            linesQueue.Clear();

            foreach (var line in activeConversation.lines)
                linesQueue.Enqueue(line);

            yield return null; 
            
            isTalking = true;
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
            
            // Broadcast the end of this conversation session
            OnConversationEnded?.Invoke(lastPlayedIndex);
            
            StartCoroutine(ResetInteractionFlag());
        }

        private IEnumerator ResetInteractionFlag()
        {
            yield return new WaitForEndOfFrame();
            IsConversationActive = false;
        }

        #endregion

        #region JSON Loading

        private void LoadFromJSON(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return;

            try
            {
                var map = JsonUtility.FromJson<ConversationMap>(jsonContent);
                if (map?.conversationMap == null) return;

                conversations = new List<Conversation>(map.conversationMap);
                idleConversation = map.idleDialogue;
                
                if (conversations.Count > 0)
                {
                    Debug.Log($"DialogueManager on {gameObject.name}: Successfully loaded {conversations.Count} conversation branches.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DialogueManager on {gameObject.name}: Error parsing JSON: {e.Message}");
            }
        }

        private bool IsConversationEmpty(Conversation conv) => 
            conv?.lines == null || conv.lines.Count == 0;

        private void Update()
        {
            if (isTalking && Input.GetKeyDown(KeyCode.F)) 
                DisplayNextLine();
        }

        #endregion
    }
}
