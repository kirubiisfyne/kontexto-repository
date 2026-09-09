using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace Master.Scripts.NPC
{
    /// <summary>
    /// Manages standalone animation playback for NPCs using the Playables API.
    /// Allows assigning and playing Humanoid/Mixamo AnimationClips directly on the Animator
    /// without requiring an Animator Controller asset.
    /// </summary>
    public class NPCAnimationManager : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("The default AnimationClip to play and loop for this NPC.")]
        [SerializeField] private AnimationClip standbyClip;

        [Header("Target Animator")]
        [Tooltip("Optional: Drag the Animator here. If left empty, it will auto-detect on this GameObject or its children.")]
        [SerializeField] private Animator animator;

        private PlayableGraph playableGraph;

        private void Awake()
        {
            // Auto-detect Animator on self or children if not explicitly assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"NPCAnimationManager on '{gameObject.name}': No Animator component found on this object or its children.");
            }
        }

        private void Start()
        {
            if (standbyClip != null)
            {
                PlayClip(standbyClip);
            }
        }

        /// <summary>
        /// Plays an AnimationClip directly through the Animator's avatar.
        /// </summary>
        /// <param name="clip">The AnimationClip to play.</param>
        public void PlayClip(AnimationClip clip)
        {
            if (clip == null || animator == null) return;

            // Clean up previous graph if already running
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }

            // Bind and play the clip through the Animator component
            AnimationPlayableUtilities.PlayClip(animator, clip, out playableGraph);
        }

        private void OnDestroy()
        {
            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }
    }
}
