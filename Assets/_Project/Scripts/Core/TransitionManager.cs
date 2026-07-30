using System.Collections;
using UnityEngine;

namespace Master.Scripts
{
    public class TransitionManager : MonoBehaviour
    {
        public static TransitionManager Instance { get; private set; }

        [Header("References")] 
        [Tooltip("The GameObject handling the visual transition.")]
        public GameObject transitionGameObject;

        public Animator animator;

        [Header("Grace Periods")]
        [Tooltip("Seconds to wait as a solid black screen BEFORE fading in when a scene loads.")]
        public float gracePeriodIn = 1f;
        [Tooltip("Seconds to wait as a solid black screen AFTER fading out before loading the next scene.")]
        public float gracePeriodOut = 1f;

        [Header("Performance")]
        [Tooltip("Disables the GameObject after the Fade-In completes so it doesn't waste performance during gameplay.")]
        public bool disableAfterTransitionIn = true;
        [Tooltip("Disables the GameObject after the Fade-Out completes.")]
        public bool disableAfterTransitionOut = false;
        
        private void Awake()
        {
            Instance = this;
            FindTransitionObject();
        }

        private IEnumerator Start()
        {
            FindTransitionObject();

            // Ensure transition object is enabled for scene load fade-in
            if (transitionGameObject != null)
            {
                transitionGameObject.SetActive(true);
            }

            if (animator != null)
            {
                // Force animator to ignore paused time so the fade-in never gets stuck
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;

                // Delay the "Transition In" animation by freezing the Animator
                if (gracePeriodIn > 0f)
                {
                    animator.speed = 0f; // Freeze the animation
                    yield return new WaitForSecondsRealtime(gracePeriodIn);
                    animator.speed = 1f; // Let it play
                }

                // Wait one frame to ensure the Animator has fully transitioned into its default state
                yield return null;

                // Wait for the Fade-In animation to completely finish based on its clip length
                float transitionLength = animator.GetCurrentAnimatorStateInfo(0).length;
                yield return new WaitForSecondsRealtime(transitionLength);

                // Disable it during gameplay to save performance!
                if (disableAfterTransitionIn && transitionGameObject != null)
                {
                    transitionGameObject.SetActive(false);
                }
            }
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

                while (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == currentStateHash && !animator.IsInTransition(0))
                {
                    yield return null;
                }

                while (animator.IsInTransition(0))
                {
                    yield return null;
                }

                while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
                {
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
    }
}
