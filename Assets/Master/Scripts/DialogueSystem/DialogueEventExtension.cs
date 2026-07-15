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

        /// <summary>
        /// Events that fire on every conversation, regardless of index.
        /// </summary>
        [System.Serializable]
        public class GlobalEvents
        {
            [Tooltip("Fires every time any conversation starts.")]
            public UnityEvent onAnyStart;
            [Tooltip("Fires every time any conversation ends.")]
            public UnityEvent onAnyEnd;
        }

        [Header("Global Events")]
        [Tooltip("These events fire on every conversation start/end, regardless of dialogue index.")]
        public GlobalEvents globalEvents = new GlobalEvents();

        [Header("Index Events")]
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
            // Fire global start event for any index
            globalEvents.onAnyStart?.Invoke();

            // Find any events mapped to the index that just started
            foreach (var item in indexEvents)
            {
                if (item.index == playedIndex)
                {
                    //Debug.Log($"DialogueEventExtension on {gameObject.name}: Triggering 'OnStart' for index {playedIndex}.");
                    item.onStart?.Invoke();
                }
            }
        }

        private void HandleConversationEnded(int playedIndex)
        {
            // Fire global end event for any index
            globalEvents.onAnyEnd?.Invoke();

            // Find any events mapped to the index that just finished
            foreach (var item in indexEvents)
            {
                if (item.index == playedIndex)
                {
                    //Debug.Log($"DialogueEventExtension on {gameObject.name}: Triggering 'OnEnd' for index {playedIndex}.");
                    item.onEnd?.Invoke();
                }
            }
        }
    }
}
