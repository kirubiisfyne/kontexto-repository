using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// Extension for DialogueManager that executes UnityEvents based on the dialogue index.
    /// Acts as a bridge between dialogue phases and other game systems.
    /// </summary>
    [RequireComponent(typeof(DialogueManager))]
    public class DialogueEventExtension : MonoBehaviour
    {
        [System.Serializable]
        public struct IndexEvent
        {
            public string name; // Helper label for the Inspector
            public int index;
            public UnityEvent onStart;
            public UnityEvent onEnd;
        }

        [Header("Event Configuration")]
        [Tooltip("Assign UnityEvents to fire when a specific dialogue index starts or finishes.")]
        public List<IndexEvent> indexEvents = new List<IndexEvent>();

        private DialogueManager dialogueManager;

        private void Awake()
        {
            dialogueManager = GetComponent<DialogueManager>();
            
            // Subscribe to the dialogue lifecycle events
            dialogueManager.OnConversationStarted += HandleConversationStarted;
            dialogueManager.OnConversationEnded += HandleConversationEnded;
        }

        private void OnDestroy()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnConversationStarted -= HandleConversationStarted;
                dialogueManager.OnConversationEnded -= HandleConversationEnded;
            }
        }

        private void HandleConversationStarted(int playedIndex)
        {
            // Find any events mapped to the index that just started
            foreach (var item in indexEvents)
            {
                if (item.index == playedIndex)
                {
                    Debug.Log($"DialogueEventExtension on {gameObject.name}: Triggering 'OnStart' for index {playedIndex}.");
                    item.onStart?.Invoke();
                }
            }
        }

        private void HandleConversationEnded(int playedIndex)
        {
            // Find any events mapped to the index that just finished
            foreach (var item in indexEvents)
            {
                if (item.index == playedIndex)
                {
                    Debug.Log($"DialogueEventExtension on {gameObject.name}: Triggering 'OnEnd' for index {playedIndex}.");
                    item.onEnd?.Invoke();
                }
            }
        }
    }
}
