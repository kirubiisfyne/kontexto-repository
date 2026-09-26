using System;
using UnityEngine;
using UnityEngine.Audio;

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

            InitializeMixerVolumes();
        }

        #region Mixer Volume Control & Persistence

        public AudioMixer ResolveMixer()
        {
            if (mainMixer != null) return mainMixer;
            if (bgmSource != null && bgmSource.outputAudioMixerGroup != null)
                return bgmSource.outputAudioMixerGroup.audioMixer;
            if (sfxSource != null && sfxSource.outputAudioMixerGroup != null)
                return sfxSource.outputAudioMixerGroup.audioMixer;
            return null;
        }

        private void InitializeMixerVolumes()
        {
            var mixer = ResolveMixer();
            if (mixer == null) return;

            // Only apply PlayerPrefs overrides if player previously changed them.
            // Otherwise, keep the mixer's designed snapshot default levels.
            if (PlayerPrefs.HasKey(PrefMaster))
                SetMixerVolume(MasterParam, PlayerPrefs.GetFloat(PrefMaster));
            if (PlayerPrefs.HasKey(PrefBgm))
                SetMixerVolume(BgmParam, PlayerPrefs.GetFloat(PrefBgm));
            if (PlayerPrefs.HasKey(PrefSfx))
                SetMixerVolume(SfxParam, PlayerPrefs.GetFloat(PrefSfx));
        }

        public void SetMasterVolume(float linear)
        {
            SetAndPersistVolume(MasterParam, PrefMaster, linear);
        }

        public void SetBGMVolume(float linear)
        {
            SetAndPersistVolume(BgmParam, PrefBgm, linear);
        }

        public void SetSFXVolume(float linear)
        {
            SetAndPersistVolume(SfxParam, PrefSfx, linear);
        }

        private void SetAndPersistVolume(string paramName, string prefKey, float linear)
        {
            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            SetMixerVolume(paramName, linear);
            PlayerPrefs.SetFloat(prefKey, linear);
            PlayerPrefs.Save();
        }

        private void SetMixerVolume(string paramName, float linear)
        {
            var mixer = ResolveMixer();
            if (mixer != null)
            {
                mixer.SetFloat(paramName, Mathf.Log10(Mathf.Clamp(linear, 0.0001f, 1f)) * 20f);
            }
        }

        public float GetMasterVolume() => GetChannelVolume(MasterParam, PrefMaster);
        public float GetBGMVolume() => GetChannelVolume(BgmParam, PrefBgm);
        public float GetSFXVolume() => GetChannelVolume(SfxParam, PrefSfx);

        private float GetChannelVolume(string paramName, string prefKey)
        {
            if (PlayerPrefs.HasKey(prefKey))
            {
                return Mathf.Clamp(PlayerPrefs.GetFloat(prefKey), 0.0001f, 1f);
            }

            var mixer = ResolveMixer();
            if (mixer != null && mixer.GetFloat(paramName, out float db))
            {
                return Mathf.Clamp(Mathf.Pow(10f, db / 20f), 0.0001f, 1f);
            }

            return 0.8f;
        }

        #endregion

        #region Sound & Music Playback

        public void PlaySFX(string name)
        {
            PlaySFX(name, false, 1f, 1f);
        }

        public void PlaySFX(string name, bool randomPitch, float minPitch = 0.85f, float maxPitch = 1.15f, float volumeScale = 1f)
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
