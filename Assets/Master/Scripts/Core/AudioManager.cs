using System;
using UnityEngine;

namespace Master.Scripts
{
    // A simple struct to map names to AudioClips in the Unity Inspector
    [Serializable]
    public struct Sound
    {
        public string name;
        public AudioClip clip;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("Audio source dedicated to background music.")]
        public AudioSource bgmSource;
        [Tooltip("Audio source dedicated to sound effects.")]
        public AudioSource sfxSource;

        [Header("Audio Libraries")]
        [Tooltip("Map names to background music clips here.")]
        public Sound[] bgmSounds;
        [Tooltip("Map names to sound effect clips here.")]
        public Sound[] sfxSounds;

        private void Awake()
        {
            // Singleton pattern to ensure only one AudioManager exists across all scenes
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Plays a sound effect by its mapped string name. (This version works perfectly in Unity UI Button events!)
        /// </summary>
        /// <param name="name">The string name of the SFX mapped in the Inspector.</param>
        public void PlaySFX(string name)
        {
            // Call the main function with no pitch shifting
            PlaySFX(name, false, 1f, 1f);
        }

        /// <summary>
        /// Plays a sound effect with optional pitch shifting. (Call this version from your C# scripts!)
        /// </summary>
        public void PlaySFX(string name, bool randomPitch, float minPitch = 0.85f, float maxPitch = 1.15f)
        {
            AudioClip clipToPlay = null;

            // Search our library for the sound by name
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
                //Debug.LogWarning($"[AudioManager] SFX '{name}' not found in the library!");
                return;
            }

            // Apply random pitch shifting if requested
            if (randomPitch)
            {
                sfxSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            }
            else
            {
                sfxSource.pitch = 1f; // Always reset back to normal if no shift is requested
            }

            // PlayOneShot allows overlapping sounds on the same AudioSource
            sfxSource.PlayOneShot(clipToPlay);
        }

        /// <summary>
        /// Plays background music by its mapped string name.
        /// </summary>
        public void PlayBGM(string name)
        {
            AudioClip clipToPlay = null;

            foreach (Sound s in bgmSounds)
            {
                if (s.name == name)
                {
                    clipToPlay = s.clip;
                    break;
                }
            }

            if (clipToPlay == null)
            {
                //Debug.LogWarning($"[AudioManager] BGM '{name}' not found in the library!");
                return;
            }

            PlayBGM(clipToPlay);
        }

        /// <summary>
        /// Plays background music directly from an AudioClip.
        /// </summary>
        public void PlayBGM(AudioClip bgmClip)
        {
            // Don't restart the song if it's already the active track
            if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return; 

            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        /// <summary>
        /// Stops the current background music.
        /// </summary>
        public void StopBGM()
        {
            bgmSource.Stop();
        }
    }
}
