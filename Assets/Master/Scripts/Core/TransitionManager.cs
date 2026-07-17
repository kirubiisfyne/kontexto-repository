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
        [Tooltip("Disables the GameObject after the Fade-Out completes. (Warning: This will hide the black screen before the scene loads!)")]
        public bool disableAfterTransitionOut = false;
        
        private void Awake()
        {
            Instance = this;
            FindTransitionObject();
        }

        private IEnumerator Start()
        {
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
                // otherwise GetCurrentAnimatorStateInfo(0).length might return 0 on the very first frame.
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
            transitionGameObject = GameObject.FindWithTag("TransitionObject");

            if (transitionGameObject != null)
            {
                animator = transitionGameObject.GetComponent<Animator>();

                if (animator == null)
                {
                    //Debug.LogWarning("TransitionManager: Transition object found, but it is missing an Animator component.");
                }
            }
            else
            {
                //Debug.LogWarning("TransitionManager: No GameObject with the tag 'TransitionObject' was found in the scene.");
            }
        }

        public IEnumerator PlayTransitionAndWait(string triggerName)
        {
            // Re-enable the GameObject in case we disabled it after Fade-In
            if (transitionGameObject != null && !transitionGameObject.activeSelf)
            {
                transitionGameObject.SetActive(true);
                // Wait one frame for the Animator to fully re-initialize
                yield return null;
            }

            if (animator != null)
            {
                // Force the animator to ignore paused time
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;

                int currentStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
                
                // Set the animator trigger
                animator.SetTrigger(triggerName);

                // 1. Wait until the animator actually starts transitioning out of the current state
                while (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == currentStateHash && !animator.IsInTransition(0))
                {
                    yield return null;
                }

                // 2. Wait until the crossfade/transition into the new clip is complete
                while (animator.IsInTransition(0))
                {
                    yield return null;
                }

                // 3. Wait until the new animation clip finishes playing
                while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
                {
                    yield return null; 
                }

                // 4. Grace Period Out (wait as a solid screen)
                if (gracePeriodOut > 0f)
                {
                    yield return new WaitForSecondsRealtime(gracePeriodOut);
                }

                // 5. Disable after transition out (if requested)
                if (disableAfterTransitionOut && transitionGameObject != null)
                {
                    transitionGameObject.SetActive(false);
                }
            }
            else
            {
                //Debug.LogError($"TransitionManager: Cannot fire trigger '{triggerName}'. Animator missing.");
            }
        }
    }
}
