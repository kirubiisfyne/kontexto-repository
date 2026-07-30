using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// Handles the visual representation of the dialogue on the UI.
    /// </summary>
    public class DialogueUIManager : MonoBehaviour
    {
        public static DialogueUIManager Instance;
        
        [Header("UI")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private float transitionDuration = 0.5f;
        
        [Header("Settings")]
        [Range(0.1f, 1.0f)] [SerializeField] private float textSpeed = 0.5f; 
        
        [Header("Data")]
        [Tooltip("Drag and drop your SpeakerDatabase ScriptableObject here")]
        [SerializeField] private SpeakerDatabase speakerDatabase;
        [SerializeField] private UnityEngine.UI.Image speakerProfileImage;

        public bool IsTyping { get; private set; }
        
        private string currentFullText;
        private Coroutine typingCoroutine;
        private Coroutine hideCoroutine;
        
        private readonly StringBuilder _sb = new StringBuilder();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            dialoguePanel.SetActive(false);
            
            if (speakerNameText != null) speakerNameText.raycastTarget = false;
            if (dialogueText != null) dialogueText.raycastTarget = false;
            if (speakerProfileImage != null) speakerProfileImage.raycastTarget = false;
        }

        public void Show()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            dialoguePanel.SetActive(true);
            if (animator != null) 
            {
                animator.ResetTrigger("Out");
                animator.SetTrigger("In");
            }
        }

        public void Hide()
        {
            if (gameObject.activeInHierarchy)
            {
                if (animator != null) 
                {
                    animator.ResetTrigger("In");
                    animator.SetTrigger("Out");
                }
                hideCoroutine = StartCoroutine(HideRoutine());
            }
            else
            {
                dialoguePanel.SetActive(false);
            }
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(transitionDuration);
            dialoguePanel.SetActive(false);
            hideCoroutine = null;
        }

        /// <summary>
        /// Updates the UI with new dialogue content.
        /// </summary>
        public void UpdateDialogueView(DialogueLine line)
        {
            Show(); 

            _sb.Clear();
            _sb.Append(!string.IsNullOrEmpty(line.speaker) ? line.speaker : "");
            speakerNameText.SetText(_sb);

            if (speakerProfileImage != null)
            {
                if (speakerDatabase != null)
                {
                    var profile = speakerDatabase.GetProfile(line.speaker);
                    if (profile != null && profile.portraitSprite != null)
                    {
                        speakerProfileImage.sprite = profile.portraitSprite;
                        speakerProfileImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        speakerProfileImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    speakerProfileImage.gameObject.SetActive(false);
                }
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(line.text));
        }

        private IEnumerator TypeSentence(string sentence)
        {
            IsTyping = true;
            currentFullText = sentence;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.text = sentence;
            _sb.Clear();
            _sb.Append(sentence);
            dialogueText.SetText(_sb);
            
            // Force mesh update so the full text geometry is pre-calculated once, 
            // preventing array resizing during the character reveal.
            dialogueText.ForceMeshUpdate();

            float waitTime = 0.02f / textSpeed;
            // Cache WaitForSeconds to prevent generating garbage every character loop
            WaitForSeconds wait = new WaitForSeconds(waitTime);
            
            foreach (char letter in sentence)
            {
                dialogueText.maxVisibleCharacters++;
                yield return wait;
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
