using System.Collections;
using TMPro;
using UnityEngine;

namespace Master.Scripts.CutsceneSystem
{
    /// <summary>
    /// Clean, cinematic subtitle presenter for cutscenes.
    /// Handles typewriter character reveals and smooth alpha fading.
    /// </summary>
    public class CutsceneSubtitleUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Animation & Settings")]
        [Tooltip("Delay in seconds before the typewriter begins typing the first character.")]
        [SerializeField] private float initialTypingDelay = 0.5f;
        [Range(0.01f, 0.1f)] [SerializeField] private float typingCharDelay = 0.03f;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private bool useTypewriter = true;

        public bool IsTyping { get; private set; }

        private Coroutine typingCoroutine;
        private Coroutine fadeCoroutine;
        private string currentFullSentence;

        private void Awake()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (subtitleText != null) subtitleText.raycastTarget = false;
        }

        /// <summary>
        /// Displays the given subtitle text with typewriter reveal and alpha fade.
        /// </summary>
        public void DisplayLine(string sentence)
        {
            if (string.IsNullOrEmpty(sentence))
            {
                Hide();
                return;
            }

            if (canvasGroup != null && canvasGroup.alpha < 1f)
            {
                FadeTo(1f);
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            if (useTypewriter && subtitleText != null)
            {
                typingCoroutine = StartCoroutine(TypewriterRoutine(sentence));
            }
            else if (subtitleText != null)
            {
                subtitleText.text = sentence;
                IsTyping = false;
            }
        }

        /// <summary>
        /// Instantly finishes typing the current line.
        /// </summary>
        public void FinishTyping()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            if (subtitleText != null && currentFullSentence != null)
            {
                subtitleText.maxVisibleCharacters = currentFullSentence.Length;
            }
            IsTyping = false;
        }

        /// <summary>
        /// Fades out and hides the subtitle UI.
        /// </summary>
        public void Hide()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            IsTyping = false;
            FadeTo(0f);
        }

        private IEnumerator TypewriterRoutine(string sentence)
        {
            IsTyping = true;
            currentFullSentence = sentence ?? string.Empty;

            subtitleText.maxVisibleCharacters = 0;
            subtitleText.text = currentFullSentence;
            subtitleText.ForceMeshUpdate();

            if (initialTypingDelay > 0f)
            {
                yield return new WaitForSeconds(initialTypingDelay);
            }

            var wait = new WaitForSeconds(typingCharDelay);

            for (int i = 0; i < currentFullSentence.Length; i++)
            {
                subtitleText.maxVisibleCharacters++;
                yield return wait;
            }

            IsTyping = false;
        }

        private void FadeTo(float targetAlpha)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            if (gameObject.activeInHierarchy)
            {
                fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            fadeCoroutine = null;
        }
    }
}
