using UnityEngine;

namespace Master.Scripts
{
    /// <summary>
    /// Scene-local component specifying background music for the current scene.
    /// Initiates a smooth dual-source crossfade via AudioManager without abrupt cuts or mutes.
    /// Fully decoupled: gracefully no-ops if AudioManager is absent (isolated test scene safe).
    /// </summary>
    [AddComponentMenu("Audio/Scene BGM")]
    [DisallowMultipleComponent]
    public class SceneBGM : MonoBehaviour
    {
        public enum EmptyBgmBehavior
        {
            [Tooltip("Continues playing whatever track was already active in the previous scene.")]
            KeepPlayingPrevious,
            [Tooltip("Smoothly fades out BGM to silence.")]
            FadeOutToSilence
        }

        [Header("Scene Background Music")]
        [Tooltip("Direct AudioClip to play for this scene. Overrides BGM Sound Name if assigned.")]
        [SerializeField] private AudioClip bgmClip;

        [Tooltip("Registered sound name in AudioManager.bgmSounds (e.g. 'GoldenGleam', 'GentleBreeze').")]
        [SerializeField] private string bgmSoundName = "GoldenGleam";

        [Header("Crossfade Configuration")]
        [Tooltip("Duration in seconds to crossfade from the previous track to this scene's track.")]
        [Range(0f, 5f)]
        [SerializeField] private float crossfadeDuration = 1.0f;

        [Tooltip("Action to take if neither AudioClip nor Sound Name is specified.")]
        [SerializeField] private EmptyBgmBehavior emptyBehavior = EmptyBgmBehavior.KeepPlayingPrevious;

        [Header("Timing")]
        [Tooltip("If true, automatically initiates crossfade on Start().")]
        [SerializeField] private bool playOnStart = true;

        public AudioClip BgmClip => bgmClip;
        public string BgmSoundName => bgmSoundName;
        public float CrossfadeDuration => crossfadeDuration;

        private void Start()
        {
            if (playOnStart)
            {
                ApplySceneBGM();
            }
        }

        /// <summary>
        /// Initiates crossfade into this scene's configured background music.
        /// Exposed as parameterless public method for UnityEvent wiring.
        /// </summary>
        public void ApplySceneBGM()
        {
            if (AudioManager.Instance == null) return;

            if (bgmClip != null)
            {
                AudioManager.Instance.CrossfadeBGM(bgmClip, crossfadeDuration);
            }
            else if (!string.IsNullOrEmpty(bgmSoundName))
            {
                AudioManager.Instance.CrossfadeBGM(bgmSoundName, crossfadeDuration);
            }
            else if (emptyBehavior == EmptyBgmBehavior.FadeOutToSilence)
            {
                AudioManager.Instance.FadeOutBGM(crossfadeDuration);
            }
        }

        /// <summary>
        /// Smoothly fades out the current scene's BGM.
        /// </summary>
        public void FadeOut()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeOutBGM(crossfadeDuration);
            }
        }

        /// <summary>
        /// Immediately stops BGM playback.
        /// </summary>
        public void Stop()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();
            }
        }
    }
}
