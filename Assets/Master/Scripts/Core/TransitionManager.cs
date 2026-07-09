using System.Collections;
using UnityEngine;

namespace Master.Scripts
{
    public class TransitionManager : MonoBehaviour
    {
        // 1. The Singleton Instance
        public static TransitionManager Instance { get; private set; }

        [Header("References")] [Tooltip("The GameObject handling the visual transition.")]
        public GameObject transitionGameObject;

        // The legacy Animation component attached to the transition object
        public Animation legacyAnimation;
        
        private void Awake()
        {
            // Singleton Setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy duplicates if they exist
                return;
            }

            Instance = this;

            // Optional but recommended: Keep this manager alive across scene loads
            DontDestroyOnLoad(gameObject);

            // Look for the Transition GO for the first time.
            FindTransitionObject();
        }

        /// <summary>
        /// Searches the scene for the transition object and grabs its Animation component.
        /// </summary>
        public void FindTransitionObject()
        {
            // Searching by Tag is more efficient than searching by name
            transitionGameObject = GameObject.FindWithTag("TransitionObject");

            if (transitionGameObject != null)
            {
                legacyAnimation = transitionGameObject.GetComponent<Animation>();

                if (legacyAnimation == null)
                {
                    Debug.LogWarning(
                        "TransitionManager: Transition object found, but it is missing a Legacy Animation component.");
                }
            }
            else
            {
                Debug.LogWarning(
                    "TransitionManager: No GameObject with the tag 'TransitionObject' was found in the scene.");
            }
        }

        /// <summary>
        /// 3. Triggers a legacy animation by name.
        /// </summary>
        /// <param name="animationName">The name of the animation clip to play (e.g., "FadeOut").</param>
        public IEnumerator PlayTransitionAndWait(string animationName)
        {
            if (legacyAnimation != null)
            {
                // Start the animation
                legacyAnimation.Play(animationName);

                // Keep yielding (waiting) for one frame at a time 
                // as long as this specific animation is still playing.
                while (legacyAnimation.IsPlaying(animationName))
                {
                    yield return null; 
                }
            }
            else
            {
                Debug.LogError($"TransitionManager: Cannot play '{animationName}'. Component missing.");
            }
        }
    }
}
