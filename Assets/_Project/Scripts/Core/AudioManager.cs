using System;
using System.Collections;
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

    [Serializable]
    public struct SceneBgmMapping
    {
        [Tooltip("Exact name of the scene (e.g. 'scn_campus', 'scn_publication-office').")]
        public string sceneName;
        [Tooltip("Name of the registered BGM sound in bgmSounds.")]
        public string bgmName;
        [Tooltip("Crossfade duration in seconds.")]
        [Range(0f, 5f)]
        public float crossfadeDuration;
    }

    public enum AudioChannel
    {
        BGM,
        SFX
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
        [Tooltip("Primary audio source dedicated to background music.")]
        public AudioSource bgmSource;
        [Tooltip("Secondary audio source for BGM crossfading (automatically created at runtime if unassigned).")]
        public AudioSource bgmCrossfadeSource;
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

        [Header("Per-Scene BGM Configuration")]
        [Tooltip("Default BGM per scene if a SceneBGM override component is absent.")]
        public SceneBgmMapping[] defaultSceneBgm;

        private AudioSource activeBgmSource;
        private AudioSource inactiveBgmSource;
        private Coroutine bgmCrossfadeCoroutine;
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeBgmSources();

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
            EvaluateSceneBgm(SceneManager.GetActiveScene().name);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAllVolumesDirectly();
            EvaluateSceneBgm(scene.name);
        }

        private void EvaluateSceneBgm(string sceneName)
        {
            // 1. If an explicit SceneBGM component exists in the newly loaded scene, let it govern playback
            #if UNITY_2023_1_OR_NEWER
            SceneBGM sceneBgm = UnityEngine.Object.FindFirstObjectByType<SceneBGM>();
            #else
            SceneBGM sceneBgm = UnityEngine.Object.FindObjectOfType<SceneBGM>();
            #endif
            if (sceneBgm != null)
            {
                return;
            }

            // 2. If a LevelLoader exists in the scene (e.g. scn_campus), LevelLoader governs level-specific BGM
            #if UNITY_2023_1_OR_NEWER
            if (UnityEngine.Object.FindFirstObjectByType<Master.Scripts.SaveSystem.LevelLoader>() != null)
            #else
            if (UnityEngine.Object.FindObjectOfType<Master.Scripts.SaveSystem.LevelLoader>() != null)
            #endif
            {
                return;
            }

            // 3. Resolve default scene BGM
            string targetTrack = ResolveDefaultSceneBgm(sceneName, out float duration);
            if (!string.IsNullOrEmpty(targetTrack))
            {
                PlayBGM(targetTrack, duration);
            }
        }

        public string ResolveDefaultSceneBgm(string sceneName, out float duration)
        {
            duration = 1.0f;
            if (defaultSceneBgm != null && defaultSceneBgm.Length > 0)
            {
                for (int i = 0; i < defaultSceneBgm.Length; i++)
                {
                    if (defaultSceneBgm[i].sceneName == sceneName)
                    {
                        duration = defaultSceneBgm[i].crossfadeDuration > 0f ? defaultSceneBgm[i].crossfadeDuration : 1.0f;
                        return defaultSceneBgm[i].bgmName;
                    }
                }
            }

            // Built-in defaults for known game scenes
            switch (sceneName)
            {
                case "scn_main-menu":
                case "scn_campus":
                    return "GoldenGleam";
                case "scn_publication-office":
                case "scn_classroom0":
                case "scn_classroom1":
                case "scn_editor":
                    return "GentleBreeze";
                default:
                    return null; // Inherits previous scene's BGM smoothly
            }
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
        /// Returns the AudioMixerGroup corresponding to the specified channel (BGM or SFX).
        /// </summary>
        public AudioMixerGroup GetChannelGroup(AudioChannel channel)
        {
            if (channel == AudioChannel.BGM && bgmSource != null && bgmSource.outputAudioMixerGroup != null)
                return bgmSource.outputAudioMixerGroup;
            if (channel == AudioChannel.SFX && sfxSource != null && sfxSource.outputAudioMixerGroup != null)
                return sfxSource.outputAudioMixerGroup;

            var mixer = ResolveMixer();
            if (mixer != null)
            {
                var groups = mixer.FindMatchingGroups(channel.ToString());
                if (groups != null && groups.Length > 0) return groups[0];
            }
            return null;
        }

        /// <summary>
        /// Attempts to find a registered sound clip by name across BGM and SFX libraries.
        /// </summary>
        public AudioClip FindClip(string soundName, AudioChannel channel = AudioChannel.BGM)
        {
            if (string.IsNullOrEmpty(soundName)) return null;

            if (channel == AudioChannel.BGM && bgmSounds != null)
            {
                foreach (var s in bgmSounds)
                {
                    if (MatchSoundName(s.name, soundName)) return s.clip;
                }
            }
            else if (channel == AudioChannel.SFX && sfxSounds != null)
            {
                foreach (var s in sfxSounds)
                {
                    if (MatchSoundName(s.name, soundName)) return s.clip;
                }
            }

            if (bgmSounds != null)
            {
                foreach (var s in bgmSounds)
                {
                    if (MatchSoundName(s.name, soundName)) return s.clip;
                }
            }
            if (sfxSounds != null)
            {
                foreach (var s in sfxSounds)
                {
                    if (MatchSoundName(s.name, soundName)) return s.clip;
                }
            }

            return null;
        }

        private static bool MatchSoundName(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) return true;
            string cleanA = a.Replace(" ", "").Replace("_", "").Replace("'", "");
            string cleanB = b.Replace(" ", "").Replace("_", "").Replace("'", "");
            return string.Equals(cleanA, cleanB, StringComparison.OrdinalIgnoreCase);
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

        public void PlaySFX(AudioClip clip)
        {
            PlaySFX(clip, false, 1f, 1f);
        }

        public void PlaySFX(AudioClip clip, bool randomPitch, float minPitch = 0.85f, float maxPitch = 1.15f, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null) return;

            float currentSFX = GetSFXVolume();
            if (currentSFX <= 0.001f || AudioListener.volume <= 0.001f) return;

            SetMixerVolume(SfxParam, currentSFX);

            sfxSource.pitch = randomPitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public AudioClip CurrentBGMClip => activeBgmSource != null && activeBgmSource.isPlaying ? activeBgmSource.clip : null;
        public bool IsBGMPlaying => activeBgmSource != null && activeBgmSource.isPlaying;

        private void InitializeBgmSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = true;
            }

            EnsureCrossfadeSource();

            activeBgmSource = bgmSource;
            inactiveBgmSource = bgmCrossfadeSource;

            if (activeBgmSource.isPlaying)
            {
                activeBgmSource.volume = 1f;
            }
            if (inactiveBgmSource != null && inactiveBgmSource != activeBgmSource)
            {
                inactiveBgmSource.volume = 0f;
            }
        }

        private void EnsureCrossfadeSource()
        {
            if (bgmSource != null)
            {
                bgmSource.spatialBlend = 0f;
            }

            if (bgmCrossfadeSource == null || bgmCrossfadeSource == bgmSource)
            {
                Transform child = transform.Find("BGM_CrossfadeSource");
                if (child != null)
                {
                    bgmCrossfadeSource = child.GetComponent<AudioSource>();
                }
                else
                {
                    GameObject crossfadeObj = new GameObject("BGM_CrossfadeSource");
                    crossfadeObj.transform.SetParent(transform);
                    bgmCrossfadeSource = crossfadeObj.AddComponent<AudioSource>();
                }
            }

            if (bgmCrossfadeSource != null)
            {
                bgmCrossfadeSource.playOnAwake = false;
                bgmCrossfadeSource.loop = true;
                bgmCrossfadeSource.spatialBlend = 0f;
                if (bgmCrossfadeSource.outputAudioMixerGroup == null)
                {
                    bgmCrossfadeSource.outputAudioMixerGroup = bgmSource != null ? bgmSource.outputAudioMixerGroup : GetChannelGroup(AudioChannel.BGM);
                }
            }

            if (activeBgmSource == null || activeBgmSource == bgmCrossfadeSource) activeBgmSource = bgmSource;
            if (inactiveBgmSource == null || inactiveBgmSource == activeBgmSource) inactiveBgmSource = bgmCrossfadeSource;
        }

        public void PlayBGM(string name, float crossfadeDuration = 1.0f)
        {
            AudioClip clip = FindClip(name, AudioChannel.BGM);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM clip '{name}' not found in registry.");
                return;
            }
            CrossfadeBGM(clip, crossfadeDuration);
        }

        public void PlayBGM(string name)
        {
            PlayBGM(name, 1.0f);
        }

        public void PlayBGM(AudioClip clip)
        {
            CrossfadeBGM(clip, 1.0f);
        }

        public void CrossfadeBGM(string name, float duration = 1.0f)
        {
            PlayBGM(name, duration);
        }

        public void CrossfadeBGM(AudioClip newClip, float duration = 1.0f)
        {
            EnsureCrossfadeSource();

            if (newClip == null)
            {
                FadeOutBGM(duration);
                return;
            }

            // 1. If active source is already playing this exact clip
            if (activeBgmSource != null && activeBgmSource.clip == newClip && activeBgmSource.isPlaying)
            {
                if (bgmCrossfadeCoroutine != null)
                {
                    StopCoroutine(bgmCrossfadeCoroutine);
                    bgmCrossfadeCoroutine = null;
                }
                if (inactiveBgmSource != null && inactiveBgmSource.isPlaying)
                {
                    inactiveBgmSource.Stop();
                    inactiveBgmSource.volume = 0f;
                }
                if (activeBgmSource.volume < 1f)
                {
                    bgmCrossfadeCoroutine = StartCoroutine(RampVolumeRoutine(activeBgmSource, 1f, 0.3f));
                }
                return;
            }

            // 2. If a crossfade into this exact clip is already in progress, let it continue
            if (bgmCrossfadeCoroutine != null && inactiveBgmSource != null && inactiveBgmSource.clip == newClip && inactiveBgmSource.isPlaying)
            {
                return;
            }

            // 3. Stop any conflicting in-progress crossfade
            if (bgmCrossfadeCoroutine != null)
            {
                StopCoroutine(bgmCrossfadeCoroutine);
                bgmCrossfadeCoroutine = null;
            }
            if (inactiveBgmSource != null && inactiveBgmSource.isPlaying)
            {
                inactiveBgmSource.Stop();
                inactiveBgmSource.volume = 0f;
            }

            // 4. Immediate switch or crossfade
            if (duration <= 0.01f || activeBgmSource == null || !activeBgmSource.isPlaying)
            {
                if (activeBgmSource != null && activeBgmSource.isPlaying)
                {
                    activeBgmSource.Stop();
                }
                if (activeBgmSource == null) activeBgmSource = bgmSource;
                activeBgmSource.clip = newClip;
                activeBgmSource.volume = 1f;
                activeBgmSource.loop = true;
                activeBgmSource.Play();
            }
            else
            {
                bgmCrossfadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(newClip, duration));
            }
        }

        private IEnumerator CrossfadeBGMRoutine(AudioClip newClip, float duration)
        {
            inactiveBgmSource.clip = newClip;
            inactiveBgmSource.loop = true;
            inactiveBgmSource.volume = 0f;
            inactiveBgmSource.Play();

            float startActiveVolume = activeBgmSource != null && activeBgmSource.isPlaying ? activeBgmSource.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (activeBgmSource != null && activeBgmSource.isPlaying)
                {
                    activeBgmSource.volume = Mathf.Lerp(startActiveVolume, 0f, t);
                }

                inactiveBgmSource.volume = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            if (activeBgmSource != null && activeBgmSource.isPlaying)
            {
                activeBgmSource.Stop();
                activeBgmSource.volume = 0f;
            }

            inactiveBgmSource.volume = 1f;

            // Swap active and inactive
            var temp = activeBgmSource;
            activeBgmSource = inactiveBgmSource;
            inactiveBgmSource = temp;
            bgmSource = activeBgmSource;

            bgmCrossfadeCoroutine = null;
        }

        public void FadeOutBGM(float duration = 1.0f)
        {
            EnsureCrossfadeSource();
            if (activeBgmSource == null || !activeBgmSource.isPlaying) return;

            if (bgmCrossfadeCoroutine != null)
            {
                StopCoroutine(bgmCrossfadeCoroutine);
                bgmCrossfadeCoroutine = null;
            }

            if (duration <= 0.01f)
            {
                activeBgmSource.Stop();
                activeBgmSource.volume = 0f;
            }
            else
            {
                bgmCrossfadeCoroutine = StartCoroutine(FadeOutBGMRoutine(duration));
            }
        }

        private IEnumerator FadeOutBGMRoutine(float duration)
        {
            float startVol = activeBgmSource != null ? activeBgmSource.volume : 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (activeBgmSource != null)
                {
                    activeBgmSource.volume = Mathf.Lerp(startVol, 0f, t);
                }
                yield return null;
            }

            if (activeBgmSource != null)
            {
                activeBgmSource.volume = 0f;
                activeBgmSource.Stop();
            }
            bgmCrossfadeCoroutine = null;
        }

        public void StopBGM()
        {
            if (bgmCrossfadeCoroutine != null)
            {
                StopCoroutine(bgmCrossfadeCoroutine);
                bgmCrossfadeCoroutine = null;
            }
            if (activeBgmSource != null)
            {
                activeBgmSource.Stop();
                activeBgmSource.volume = 0f;
            }
            if (inactiveBgmSource != null)
            {
                inactiveBgmSource.Stop();
                inactiveBgmSource.volume = 0f;
            }
        }

        public IEnumerator FadeBGMOut(float duration)
        {
            FadeOutBGM(duration);
            if (bgmCrossfadeCoroutine != null)
            {
                yield return bgmCrossfadeCoroutine;
            }
        }

        public IEnumerator FadeBGMIn(float duration)
        {
            EnsureCrossfadeSource();
            if (activeBgmSource != null && activeBgmSource.isPlaying)
            {
                if (bgmCrossfadeCoroutine != null)
                {
                    StopCoroutine(bgmCrossfadeCoroutine);
                    bgmCrossfadeCoroutine = null;
                }
                bgmCrossfadeCoroutine = StartCoroutine(RampVolumeRoutine(activeBgmSource, 1f, duration));
                yield return bgmCrossfadeCoroutine;
            }
        }

        private IEnumerator RampVolumeRoutine(AudioSource source, float targetVolume, float duration)
        {
            if (source == null) yield break;
            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVol, targetVolume, t);
                yield return null;
            }

            source.volume = targetVolume;
            bgmCrossfadeCoroutine = null;
        }
        #endregion
    }
}
