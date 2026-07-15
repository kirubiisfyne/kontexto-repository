using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts
{
    [RequireComponent(typeof(AudioSource))]
    public class LocalAudioPlayer : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Tooltip("The audio source to play sounds from. If empty, will try to get from this GameObject.")]
        public AudioSource audioSource;
        
        [Tooltip("Map names to sound effect clips here.")]
        public Sound[] sfxSounds;

        [Header("Pitch Shifting")]
        public bool useRandomPitch = false;
        [Tooltip("Min and Max pitch range. X = min pitch, Y = max pitch")]
        public Vector2 pitchShiftRange = new Vector2(0.85f, 1.15f);

        [Header("Detached Playback")]
        [Tooltip("If true, spawns a temporary audio object to play the sound. Perfect if this object is destroyed or disabled immediately after playing!")]
        public bool playDetached = false;

        [Header("Events")]
        [Tooltip("Triggered when a sound is played. Can be hooked to other Unity Events.")]
        public UnityEvent onSoundPlayed;
        
        [Tooltip("Triggered when the played sound finishes. Note: If playDetached is true and this object is destroyed, this may not fire.")]
        public UnityEvent onSoundFinished;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        public void PlaySFX(string name)
        {
            AudioClip clipToPlay = null;

            foreach (Sound s in sfxSounds)
            {
                if (s.name == name)
                {
                    clipToPlay = s.clip;
                    break;
                }
            }

            if (clipToPlay == null)
            {
                //Debug.LogWarning($"[LocalAudioPlayer] SFX '{name}' not found on {gameObject.name}!");
                return;
            }

            HandlePlayback(clipToPlay);
        }

        public void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            HandlePlayback(clip);
        }

        private void HandlePlayback(AudioClip clip)
        {
            float targetPitch = 1f;
            if (useRandomPitch)
            {
                targetPitch = Random.Range(pitchShiftRange.x, pitchShiftRange.y);
            }

            if (playDetached)
            {
                // Create a temporary object to hold the sound
                GameObject tempAudioObj = new GameObject("TempAudio_" + clip.name);
                tempAudioObj.transform.position = transform.position;
                
                // Copy settings over to the new AudioSource
                AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
                tempSource.clip = clip;
                tempSource.spatialBlend = audioSource.spatialBlend; // Keeps it 3D if it was 3D
                tempSource.volume = audioSource.volume;
                tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                tempSource.pitch = targetPitch;
                tempSource.playOnAwake = false;

                tempSource.Play();
                onSoundPlayed?.Invoke();

                // Destroy the temporary object after the clip finishes
                float actualDuration = clip.length / Mathf.Abs(targetPitch);
                Destroy(tempAudioObj, actualDuration);
            }
            else
            {
                audioSource.pitch = targetPitch;
                audioSource.PlayOneShot(clip);
                onSoundPlayed?.Invoke();
                
                // Wait for the clip to finish then trigger the finished event
                StartCoroutine(WaitForSound(clip.length, audioSource.pitch));
            }
        }

        private IEnumerator WaitForSound(float clipLength, float pitch)
        {
            float actualDuration = clipLength / Mathf.Abs(pitch);
            yield return new WaitForSeconds(actualDuration);
            onSoundFinished?.Invoke();
        }
    }
}
