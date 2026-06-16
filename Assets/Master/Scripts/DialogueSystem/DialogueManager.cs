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

        private const string FALLBACK_JSON = "{\"conversationMap\": [{\"name\": \"Fallback\",\"lines\": [{\"speaker\": \"System\",\"text\": \"Dialogue missing. Check JSON configuration.\"}]}]}";

        [Header("Dialogue Content")]
        [Tooltip("This list is automatically populated from the JSON file at runtime.")]
        public List<Conversation> conversations = new List<Conversation>();
        public int currentConversationIndex = 0;

        [Header("Data Assets")]
        public TextAsset dialogueJson;

        private Conversation activeConversation;
        private readonly Queue<DialogueLine> linesQueue = new Queue<DialogueLine>();
        private bool isTalking;

        private void Awake()
        {
            if (dialogueJson != null)
            {
                Debug.Log($"DialogueManager on {gameObject.name}: Parsing JSON '{dialogueJson.name}'...");
                LoadFromJSON(dialogueJson.text);
            }
        }

        #region Public API

        public void Interact()
        {
            LastInteracted = this;

            // SNAPSHOT: Grab the conversation branch IMMEDIATELY.
            // This ensures we capture the state BEFORE any other IInteractable (like Task System) changes the index.
            if (currentConversationIndex >= 0 && currentConversationIndex < conversations.Count)
            {
                activeConversation = conversations[currentConversationIndex];
            }

            StartCoroutine(StartDialogueRoutine());
        }

        /// <summary>
        /// Moves to the next conversation branch in the loaded list.
        /// </summary>
        public void UpdateIndex()
        {
            if (conversations.Count > 0)
                currentConversationIndex = (currentConversationIndex + 1) % conversations.Count;
        }

        public static void GlobalUpdateIndex()
        {
            if (LastInteracted != null) LastInteracted.UpdateIndex();
        }

        public void SetIndex(int index)
        {
            currentConversationIndex = Mathf.Clamp(index, 0, conversations.Count - 1);
        }

        #endregion

        #region Core Logic

        private IEnumerator StartDialogueRoutine()
        {
            // Fallback check: if the snapshot is invalid, use hardcoded fallback
            if (IsConversationEmpty(activeConversation))
            {
                var map = JsonUtility.FromJson<ConversationMap>(FALLBACK_JSON);
                activeConversation = map.conversationMap[0];
            }

            IsConversationActive = true;
            linesQueue.Clear();

            foreach (var line in activeConversation.lines)
                linesQueue.Enqueue(line);

            // Wait one frame to avoid capturing the same 'E' press for line skipping
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
            IsConversationActive = false;
            DialogueUIManager.Instance.Hide();
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

                // Overwrite list with JSON content
                conversations = new List<Conversation>(map.conversationMap);
                
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
            if (isTalking && Input.GetKeyDown(KeyCode.E)) 
                DisplayNextLine();
        }

        #endregion
    }
}
