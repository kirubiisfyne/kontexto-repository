using System.Collections;
using UnityEngine;

namespace Master.Scripts
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        public static event System.Action<bool> OnTransitionStateChanged;
        public static bool IsTransitioning { get; private set; } = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsTransitioning = false;
            OnTransitionStateChanged = null;
        }

        [Header("References")] 
        [Tooltip("The GameObject handling the visual transition.")]
        public GameObject transitionGameObject;

        public Animator animator;

        [Header("Grace Periods")]
        [Tooltip("Seconds to wait as a solid black screen BEFORE fading in when a scene loads.")]
        public float gracePeriodIn = 1f;
        [Tooltip("Seconds to wait as a solid black screen AFTER fading out before loading the next scene.")]
         public float gracePeriodOut = 1f;
 
        [Header("Audio Transitions")]
        [Tooltip("Whether to fade BGM volume to 0 during transition out and restore it on transition in.")]
        public bool fadeBgmOnTransition = true;
        [Tooltip("Fallback duration for BGM fade if visual animation length cannot be resolved.")]
        public float defaultBgmFadeDuration = 0.8f;

         [Header("Performance")]
        [Tooltip("Disables the GameObject after the Fade-In completes so it doesn't waste performance during gameplay.")]
        public bool disableAfterTransitionIn = true;
        [Tooltip("Disables the GameObject after the Fade-Out completes.")]
        public bool disableAfterTransitionOut = false;
        
        private Coroutine sceneLoadFadeInCoroutine;

        private void Awake()
        {
            Instance = this;
            FindTransitionObject();
        }

        private void Start()
        {
            sceneLoadFadeInCoroutine = StartCoroutine(SceneLoadFadeInRoutine());
        }

        private IEnumerator SceneLoadFadeInRoutine()
        {
            FindTransitionObject();

            // Ensure transition object is enabled for scene load fade-in
            if (transitionGameObject != null)
            {
                transitionGameObject.SetActive(true);
            }

            if (animator != null)
            {
                NotifyTransitionState(true);

                // Force animator to ignore paused time so the fade-in never gets stuck
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;

                // Delay the "Transition In" animation by freezing the Animator
                if (gracePeriodIn > 0f)
                {
                    animator.speed = 0f; // Freeze the animation
                    yield return new WaitForSecondsRealtime(gracePeriodIn);
                    animator.speed = 1f; // Let it play
                }

                 yield return null;
 
                 // Wait for the Fade-In animation to completely finish based on its clip length
                 float transitionLength = animator.GetCurrentAnimatorStateInfo(0).length;
                if (transitionLength <= 0.05f) transitionLength = defaultBgmFadeDuration;

                if (fadeBgmOnTransition && AudioManager.Instance != null)
                {
                    StartCoroutine(AudioManager.Instance.FadeBGMIn(transitionLength));
                }

                 yield return new WaitForSecondsRealtime(transitionLength);
 
                // Disable it during gameplay to save performance!
                if (disableAfterTransitionIn && transitionGameObject != null)
                {
                    transitionGameObject.SetActive(false);
                }
            }

            // Transition in finished: re-enable inputs
            NotifyTransitionState(false);
            sceneLoadFadeInCoroutine = null;
        }

        public void FindTransitionObject()
        {
            if (transitionGameObject == null)
            {
                transitionGameObject = GameObject.FindWithTag("TransitionObject");

                if (transitionGameObject == null)
                {
                    Transform child = transform.Find("TransitionObject");
                    if (child != null)
                    {
                        transitionGameObject = child.gameObject;
                    }
                }
            }

            if (transitionGameObject != null && animator == null)
            {
                animator = transitionGameObject.GetComponent<Animator>();
            }
        }

        public IEnumerator PlayTransitionAndWait(string triggerName)
        {
            // Stop any ongoing scene-load fade-in so it doesn't conflict or prematurely disable transitionGameObject
            if (sceneLoadFadeInCoroutine != null)
            {
                StopCoroutine(sceneLoadFadeInCoroutine);
                sceneLoadFadeInCoroutine = null;
            }

            // Transition out starting: lock inputs
            NotifyTransitionState(true);

            if (transitionGameObject != null && !transitionGameObject.activeSelf)
            {
                transitionGameObject.SetActive(true);
                yield return null;
            }

            if (animator != null)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;

                int currentStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
                 animator.SetTrigger(triggerName);
 
                if (fadeBgmOnTransition && AudioManager.Instance != null)
                {
                    float fadeOutTime = defaultBgmFadeDuration;
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.length > 0.05f) fadeOutTime = state.length;
                    fadeOutTime += Mathf.Max(0f, gracePeriodOut);
                    StartCoroutine(AudioManager.Instance.FadeBGMOut(fadeOutTime));
                }
                float safetyTimeout = 2.5f;
                float timer = 0f;
                while (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == currentStateHash && !animator.IsInTransition(0) && timer < safetyTimeout)
                {
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }

                timer = 0f;
                while (animator.IsInTransition(0) && timer < safetyTimeout)
                {
                    timer += Time.unscaledDeltaTime;
                    yield return null;
                }

                timer = 0f;
                while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f && timer < safetyTimeout)
                {
                    timer += Time.unscaledDeltaTime;
                    yield return null; 
                }

                if (gracePeriodOut > 0f)
                {
                    yield return new WaitForSecondsRealtime(gracePeriodOut);
                }

                if (disableAfterTransitionOut && transitionGameObject != null)
                {
                    transitionGameObject.SetActive(false);
                }
            }
        }

        private static void NotifyTransitionState(bool transitioning)
        {
            IsTransitioning = transitioning;

            if (OnTransitionStateChanged == null) return;

            var invocationList = OnTransitionStateChanged.GetInvocationList();
            foreach (var handler in invocationList)
            {
                try
                {
                    ((System.Action<bool>)handler)(transitioning);
                }
                catch (System.Exception)
                {
                    // Ignore dead object references from unloaded scenes
                }
            }
        }
    }
}
