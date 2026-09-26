using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Master.Scripts
{
    [Serializable]
    public struct Sound
    {
        public string name;
        public AudioClip clip;
    }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public const string MasterParam = "MasterVolume";
        public const string BgmParam = "BGMVolume";
        public const string SfxParam = "SFXVolume";

        public const string PrefMaster = "kontexto.audio.master";
        public const string PrefBgm = "kontexto.audio.bgm";
        public const string PrefSfx = "kontexto.audio.sfx";

        [Header("Audio Sources")]
        [Tooltip("Audio source dedicated to background music.")]
        public AudioSource bgmSource;
        [Tooltip("Audio source dedicated to sound effects.")]
        public AudioSource sfxSource;

        [Header("Audio Mixer")]
        [Tooltip("The main AudioMixer asset controlling game audio channels.")]
        public AudioMixer mainMixer;

        [Header("Audio Libraries")]
        [Tooltip("Map names to background music clips here.")]
        public Sound[] bgmSounds;
        [Tooltip("Map names to sound effect clips here.")]
        public Sound[] sfxSounds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initial synchronous pass
            ApplyAllVolumesDirectly();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            // Standard Unity lifecycle: Start() runs after scene and AudioMixer snapshot have loaded.
            // Re-apply without any coroutines or frame waits to guarantee saved volumes stick.
            ApplyAllVolumesDirectly();
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAllVolumesDirectly();
        }

        #region Volume Control & Persistence

        public AudioMixer ResolveMixer()
        {
            if (mainMixer != null) return mainMixer;
            if (bgmSource != null && bgmSource.outputAudioMixerGroup != null)
                return bgmSource.outputAudioMixerGroup.audioMixer;
            if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
                return sfxSource.outputAudioMixerGroup.audioMixer;
            return null;
        }

        /// <summary>
        /// Synchronously applies volume settings to AudioListener, AudioSources, and AudioMixer.
        /// Avoids double-attenuation: component volumes act as instant hardware mute gates (0 or 1),
        /// while AudioMixer manages the exact decibel curve.
        /// </summary>
        public void ApplyAllVolumesDirectly()
        {
            float master = GetMasterVolume();
            float bgm = GetBGMVolume();
            float sfx = GetSFXVolume();

            // 1. Hardware-level mute gates (0 if muted, 1 if active)
            AudioListener.volume = master <= 0.001f ? 0f : 1f;

            if (bgmSource != null)
            {
                bgmSource.volume = bgm <= 0.001f ? 0f : 1f;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = sfx <= 0.001f ? 0f : 1f;
            }

            // 2. AudioMixer manages the smooth decibel attenuation curve
            SetMixerVolume(MasterParam, master);
            SetMixerVolume(BgmParam, bgm);
            SetMixerVolume(SfxParam, sfx);
        }

        public void SetMasterVolume(float linear)
        {
            linear = Mathf.Clamp01(linear);
            AudioListener.volume = linear <= 0.001f ? 0f : 1f;
            SetMixerVolume(MasterParam, linear);

            PlayerPrefs.SetFloat(PrefMaster, linear);
            PlayerPrefs.Save();
        }

        public void SetBGMVolume(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (bgmSource != null)
            {
                bgmSource.volume = linear <= 0.001f ? 0f : 1f;
            }
            SetMixerVolume(BgmParam, linear);

            PlayerPrefs.SetFloat(PrefBgm, linear);
            PlayerPrefs.Save();
        }

        public void SetSFXVolume(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (sfxSource != null)
            {
                sfxSource.volume = linear <= 0.001f ? 0f : 1f;
            }
            SetMixerVolume(SfxParam, linear);

            PlayerPrefs.SetFloat(PrefSfx, linear);
            PlayerPrefs.Save();
        }

        private void SetMixerVolume(string paramName, float linear)
        {
            var mixer = ResolveMixer();
            if (mixer != null)
            {
                // When slider is at 0 (or near-zero), explicitly set to -80dB to mute
                float db = linear <= 0.001f ? -80f : Mathf.Log10(linear) * 20f;
                mixer.SetFloat(paramName, db);
            }
        }

        public float GetMasterVolume() => GetChannelVolume(MasterParam, PrefMaster);
        public float GetBGMVolume() => GetChannelVolume(BgmParam, PrefBgm);
        public float GetSFXVolume() => GetChannelVolume(SfxParam, PrefSfx);

        private float GetChannelVolume(string paramName, string prefKey)
        {
            if (PlayerPrefs.HasKey(prefKey))
            {
                return Mathf.Clamp01(PlayerPrefs.GetFloat(prefKey));
            }

            var mixer = ResolveMixer();
            if (mixer != null && mixer.GetFloat(paramName, out float db))
            {
                if (db <= -79.9f) return 0f;
                return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
            }

            return 1f;
        }

        #endregion

        #region Sound & Music Playback

        public void PlaySFX(string name)
        {
            PlaySFX(name, false, 1f, 1f);
        }

        public void PlaySFX(string name, bool randomPitch, float minPitch = 0.85f, float maxPitch = 1.15f, float volumeScale = 1f)
        {
            float currentSFX = GetSFXVolume();
            if (currentSFX <= 0.001f || AudioListener.volume <= 0.001f) return;

            SetMixerVolume(SfxParam, currentSFX);

            AudioClip clipToPlay = null;

            foreach (Sound s in sfxSounds)
            {
                if (s.name == name)
                {
                    clipToPlay = s.clip;
                    break;
                }
            }

            if (clipToPlay == null) return;

            sfxSource.pitch = randomPitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            sfxSource.PlayOneShot(clipToPlay, volumeScale);
        }

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

            if (clipToPlay == null) return;

            PlayBGM(clipToPlay);
        }

        public void PlayBGM(AudioClip bgmClip)
        {
            if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return;

            // Apply volumes immediately before starting playback
            float bgm = GetBGMVolume();
            bgmSource.volume = bgm <= 0.001f ? 0f : 1f;
            SetMixerVolume(BgmParam, bgm);

            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            bgmSource.Stop();
        }

        #endregion
    }
}
