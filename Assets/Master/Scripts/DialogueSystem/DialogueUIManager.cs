using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Master.Scripts.DialogueSystem
{
    public class DialogueUIManager : MonoBehaviour
    {
        public static DialogueUIManager Instance;

        [Header("UI Components")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Image speakerPortrait;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;

        [FormerlySerializedAs("dialogueAnimaton")]
        [Header("Animations")]
        [SerializeField] private Animation dialogueAnimation;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Settings")]
        [Range(0.1f, 1.0f)] [SerializeField] private float textSpeed = 0.5f; 

        public bool IsTyping { get; private set; }
        public bool IsAnimating => (dialogueAnimation != null && dialogueAnimation.isPlaying) || hideCoroutine != null;
        
        private string currentFullText;
        private Coroutine typingCoroutine;
        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            // Hide immediately without animation on start
            dialoguePanel.SetActive(false);
        }

        private void Show()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            if (!dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(true);
                if (dialogueAnimation != null)
                {
                    dialogueAnimation.Play("anim_DialogueBoxFadeIn");
                }
            }
        }

        public void Hide()
        {
            if (gameObject.activeInHierarchy && dialoguePanel.activeSelf)
            {
                if (hideCoroutine != null) StopCoroutine(hideCoroutine);
                hideCoroutine = StartCoroutine(FadeOutAndHide());
            }
            else
            {
                dialoguePanel.SetActive(false);
            }
        }

        private IEnumerator FadeOutAndHide()
        {
            if (dialogueAnimation != null)
            {
                dialogueAnimation.Play("anim_DialogueBoxFadeOut");
            }
            
            yield return new WaitForSeconds(fadeDuration);
            dialoguePanel.SetActive(false);
            hideCoroutine = null;
        }

        public void UpdateDialogueView(DialogueLine line)
        {
            Show(); 

            if (line.speaker != null)
            {
                speakerNameText.text = line.speaker.characterName;
                speakerNameText.color = line.speaker.nameColor;
                speakerPortrait.sprite = line.speaker.portrait;
                speakerPortrait.gameObject.SetActive(line.speaker.portrait != null);

                // Trigger portrait jump animation
                if (dialogueAnimation != null)
                {
                    dialogueAnimation.Play("anim_DialogueBoxCharacterJump");
                }
            }
            else
            {
                speakerNameText.text = "";
                speakerPortrait.gameObject.SetActive(false);
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(line.text));
        }

        IEnumerator TypeSentence(string sentence)
        {
            IsTyping = true;
            currentFullText = sentence;
            dialogueText.maxVisibleCharacters = 0;

            float waitTime = 0.02f / textSpeed;
            
            dialogueText.text = sentence;
            foreach (char letter in sentence)
            {
                dialogueText.maxVisibleCharacters++;
                yield return new WaitForSeconds(waitTime);
            }
            
            IsTyping = false;
        }

        public void FinishTyping()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.maxVisibleCharacters = currentFullText.Length;
            dialogueText.text = currentFullText;
            IsTyping = false;
        }
    }
}