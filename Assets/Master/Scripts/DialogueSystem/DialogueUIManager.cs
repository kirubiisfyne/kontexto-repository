using System.Collections;
using TMPro;
using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// Handles the visual representation of the dialogue on the UI.
    /// </summary>
    public class DialogueUIManager : MonoBehaviour
    {
        public static DialogueUIManager Instance;

        [Header("UI Components")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Settings")]
        [Range(0.1f, 1.0f)] [SerializeField] private float textSpeed = 0.5f; 

        public bool IsTyping { get; private set; }
        
        private string currentFullText;
        private Coroutine typingCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            dialoguePanel.SetActive(false);
        }

        public void Show() => dialoguePanel.SetActive(true);
        public void Hide() => dialoguePanel.SetActive(false);

        /// <summary>
        /// Updates the UI with new dialogue content.
        /// </summary>
        public void UpdateDialogueView(DialogueLine line)
        {
            Show(); 

            speakerNameText.text = !string.IsNullOrEmpty(line.speaker) ? line.speaker : "";

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(line.text));
        }

        private IEnumerator TypeSentence(string sentence)
        {
            IsTyping = true;
            currentFullText = sentence;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.text = sentence;

            float waitTime = 0.02f / textSpeed;
            
            foreach (char letter in sentence)
            {
                dialogueText.maxVisibleCharacters++;
                yield return new WaitForSeconds(waitTime);
            }
            
            IsTyping = false;
        }

        /// <summary>
        /// Immediately shows the full text if currently typing.
        /// </summary>
        public void FinishTyping()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.maxVisibleCharacters = currentFullText.Length;
            IsTyping = false;
        }
    }
}
