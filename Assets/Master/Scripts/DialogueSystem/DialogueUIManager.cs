using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Master.Scripts.DialogueSystem
{
    public class DialogueUIManager : MonoBehaviour
    {
        public static DialogueUIManager Instance;

        [Header("UI Components")]
        [SerializeField] private GameObject dialoguePanel;

        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private Image speakerPortrait;
        [SerializeField] private Image speakerCharacterImage;

        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Typing Settings")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float textSpeed = 0.5f;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float bounceDuration = 0.15f;
        [SerializeField] private float bounceScale = 1.08f;

        public bool IsTyping { get; private set; }

        private string currentFullText;
        private Coroutine typingCoroutine;

        private bool hasOpenedDialogue = false;

        private Vector3 originalCharacterScale;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            if (speakerCharacterImage != null)
                originalCharacterScale = speakerCharacterImage.rectTransform.localScale;

            HideInstant();
        }

        private void HideInstant()
        {
            dialoguePanel.SetActive(false);

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private IEnumerator FadeIn()
        {
            dialoguePanel.SetActive(true);

            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;

                float t = timer / fadeDuration;

                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;

                float t = timer / fadeDuration;

                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

                yield return null;
            }

            canvasGroup.alpha = 0f;

            dialoguePanel.SetActive(false);

            hasOpenedDialogue = false;
        }

        private IEnumerator BounceCharacterImage()
        {
            if (speakerCharacterImage == null)
                yield break;

            RectTransform rect = speakerCharacterImage.rectTransform;

            Vector3 startScale = originalCharacterScale;
            Vector3 enlargedScale = originalCharacterScale * bounceScale;

            float timer = 0f;

            while (timer < bounceDuration)
            {
                timer += Time.deltaTime;

                float t = timer / bounceDuration;

                rect.localScale = Vector3.Lerp(startScale, enlargedScale, t);

                yield return null;
            }

            timer = 0f;

            while (timer < bounceDuration)
            {
                timer += Time.deltaTime;

                float t = timer / bounceDuration;

                rect.localScale = Vector3.Lerp(enlargedScale, startScale, t);

                yield return null;
            }

            rect.localScale = startScale;
        }

        public void Hide()
        {
            StopAllCoroutines();

            StartCoroutine(FadeOut());
        }

        public void UpdateDialogueView(DialogueLine line)
        {
            // ONLY fade in the FIRST time
            if (!hasOpenedDialogue)
            {
                StartCoroutine(FadeIn());

                hasOpenedDialogue = true;
            }

            if (line.speaker != null)
            {
                // NAME
                speakerNameText.text = line.speaker.characterName;
                speakerNameText.color = line.speaker.nameColor;

                // PORTRAIT
                if (speakerPortrait != null)
                {
                    speakerPortrait.sprite = line.speaker.portrait;
                    speakerPortrait.gameObject.SetActive(line.speaker.portrait != null);
                }

                // CHARACTER IMAGE
                if (speakerCharacterImage != null)
                {
                    speakerCharacterImage.sprite = line.speaker.characterImage;

                    speakerCharacterImage.gameObject.SetActive(line.speaker.characterImage != null);

                    // BOUNCE ANIMATION
                    StopCoroutine(nameof(BounceCharacterImage));
                    StartCoroutine(BounceCharacterImage());
                }
            }
            else
            {
                speakerNameText.text = "";

                if (speakerPortrait != null)
                    speakerPortrait.gameObject.SetActive(false);

                if (speakerCharacterImage != null)
                    speakerCharacterImage.gameObject.SetActive(false);
            }

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

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
            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            dialogueText.maxVisibleCharacters = currentFullText.Length;

            dialogueText.text = currentFullText;

            IsTyping = false;
        }
    }
}