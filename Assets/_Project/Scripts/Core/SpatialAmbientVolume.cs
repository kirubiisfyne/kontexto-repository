using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace Master.Scripts
{
    public enum AmbientState
    {
        Inactive,
        WaitingInterval,
        Playing,
        Paused
    }

    /// <summary>
    /// Spatially plays ambient music, soundscapes, or environmental cues at randomized 3D positions
    /// within a parent volume (BoxCollider, SphereCollider, or bounds extents).
    /// Decoupled component wired to AudioManager's AudioMixer channels.
    /// </summary>
    [AddComponentMenu("Audio/Spatial Ambient Volume")]
    public class SpatialAmbientVolume : MonoBehaviour
    {
        [Header("Parent Volume Settings")]
        [Tooltip("The collider defining the 3D boundary. If empty, automatically queries parent or self for a BoxCollider or SphereCollider.")]
        [SerializeField] private Collider parentCollider;

        [Tooltip("Fallback box dimensions used to sample random points if no Collider is assigned or found.")]
        [SerializeField] private Vector3 fallbackVolumeSize = new Vector3(10f, 4f, 10f);

        [Header("Audio Routing")]
        [Tooltip("Target AudioMixer channel (BGM for atmospheric music, SFX for environmental audio).")]
        [SerializeField] private AudioChannel channel = AudioChannel.BGM;

        [Tooltip("Optional custom AudioMixerGroup override. If null, automatically resolves from AudioManager.")]
        [SerializeField] private AudioMixerGroup customMixerGroup;

        [Header("Audio Clips (Zone Pool)")]
        [Tooltip("Audio clips available for this ambient zone.")]
        [SerializeField] private AudioClip[] ambientClips = Array.Empty<AudioClip>();

        [Tooltip("Optional sound names to retrieve from AudioManager.sfxSounds or AudioManager.bgmSounds at runtime.")]
        [SerializeField] private string[] registeredSoundNames = Array.Empty<string>();

        [Header("Audio Variation")]
        [Range(0f, 1f)]
        [SerializeField] private float baseVolume = 0.75f;

        [Tooltip("Random volume jitter applied per play (e.g. 0.05 = +/-5%).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float volumeJitter = 0.05f;

        [Range(0.5f, 1.5f)]
        [SerializeField] private float basePitch = 1.0f;

        [Tooltip("Random pitch jitter applied per play (e.g. 0.05 = +/-5%).")]
        [Range(0f, 0.2f)]
        [SerializeField] private float pitchJitter = 0.04f;

        [Header("Timing & Playback Control")]
        [Tooltip("Whether ambient scheduling automatically starts on Start().")]
        [SerializeField] private bool playOnStart = true;

        [Tooltip("Minimum wait time in seconds between ambient plays.")]
        [SerializeField] private float minInterval = 6.0f;

        [Tooltip("Maximum wait time in seconds between ambient plays.")]
        [SerializeField] private float maxInterval = 16.0f;

        [Tooltip("If true, pauses scheduling and active audio when Time.timeScale == 0.")]
        [SerializeField] private bool pauseWithGame = true;

        [Header("3D Spatial Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float spatialBlend = 1.0f;

        [Tooltip("Within this distance, sound remains at maximum volume.")]
        [SerializeField] private float minDistance = 1.5f;

        [Tooltip("Beyond this distance, sound is completely inaudible.")]
        [SerializeField] private float maxDistance = 25.0f;

        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Events")]
        [Tooltip("Fired when an ambient sound begins playing at a sampled point.")]
        public UnityEvent onAmbientStarted;

        [Tooltip("Fired when the current ambient sound finishes playing.")]
        public UnityEvent onAmbientCompleted;

        // Runtime state
        private AmbientState currentState = AmbientState.Inactive;
        private Coroutine playbackLoopCoroutine;
        private Coroutine activePlayCoroutine;

        private GameObject emitterObject;
        private AudioSource emitterSource;

        private int lastClipIndex = -1;
        private Vector3 lastSampledPosition;
        private bool hasSampledPosition = false;
        private bool isPaused = false;

        public AmbientState CurrentState => currentState;
        public Vector3 LastSampledPosition => lastSampledPosition;

        private void Awake()
        {
            ResolveParentCollider();
            InitializeEmitter();
        }

        private void Start()
        {
            ConfigureMixerRouting();

            if (playOnStart)
            {
                StartAmbience();
            }
        }

        private void OnEnable()
        {
            ConfigureMixerRouting();
        }

        private void OnDisable()
        {
            StopAmbience();
        }

        private void OnDestroy()
        {
            if (emitterObject != null)
            {
                Destroy(emitterObject);
            }
        }

        private void Update()
        {
            if (!pauseWithGame) return;

            bool isGamePaused = Time.timeScale <= 0.0001f;
            if (isGamePaused && !isPaused && currentState != AmbientState.Inactive)
            {
                isPaused = true;
                if (emitterSource != null && emitterSource.isPlaying)
                {
                    emitterSource.Pause();
                }
            }
            else if (!isGamePaused && isPaused)
            {
                isPaused = false;
                if (emitterSource != null && currentState == AmbientState.Playing)
                {
                    emitterSource.UnPause();
                }
            }
        }

        #region Initialization & Collider Resolution

        private void ResolveParentCollider()
        {
            if (parentCollider == null)
            {
                parentCollider = GetComponentInParent<Collider>();
                if (parentCollider == null)
                {
                    parentCollider = GetComponent<Collider>();
                }
            }
        }

        private void InitializeEmitter()
        {
            if (emitterObject == null)
            {
                emitterObject = new GameObject($"{gameObject.name}_SpatialEmitter");
                emitterObject.transform.SetParent(transform);
                emitterObject.transform.localPosition = Vector3.zero;

                emitterSource = emitterObject.AddComponent<AudioSource>();
                emitterSource.playOnAwake = false;
                emitterSource.loop = false;
                emitterSource.spatialBlend = spatialBlend;
                emitterSource.minDistance = minDistance;
                emitterSource.maxDistance = maxDistance;
                emitterSource.rolloffMode = rolloffMode;
                emitterSource.dopplerLevel = 0f;
            }
        }

        /// <summary>
        /// Connects the AudioSource to AudioManager's resolved AudioMixer channel.
        /// Gracefully leaves unassigned if AudioManager is absent (safe for test scenes).
        /// </summary>
        public void ConfigureMixerRouting()
        {
            if (emitterSource == null) return;

            if (customMixerGroup != null)
            {
                emitterSource.outputAudioMixerGroup = customMixerGroup;
                return;
            }

            if (AudioManager.Instance != null)
            {
                var group = AudioManager.Instance.GetChannelGroup(channel);
                if (group != null)
                {
                    emitterSource.outputAudioMixerGroup = group;
                }
            }
        }

        #endregion

        #region Public Control Methods (UnityEvent Friendly)

        /// <summary>
        /// Starts the scheduled ambient playback loop.
        /// </summary>
        public void StartAmbience()
        {
            if (playbackLoopCoroutine != null)
            {
                StopCoroutine(playbackLoopCoroutine);
            }

            ConfigureMixerRouting();
            currentState = AmbientState.WaitingInterval;
            playbackLoopCoroutine = StartCoroutine(PlaybackLoopRoutine());
        }

        /// <summary>
        /// Stops active ambience immediately and cancels upcoming intervals.
        /// </summary>
        public void StopAmbience()
        {
            if (playbackLoopCoroutine != null)
            {
                StopCoroutine(playbackLoopCoroutine);
                playbackLoopCoroutine = null;
            }

            if (activePlayCoroutine != null)
            {
                StopCoroutine(activePlayCoroutine);
                activePlayCoroutine = null;
            }

            if (emitterSource != null && emitterSource.isPlaying)
            {
                emitterSource.Stop();
            }

            currentState = AmbientState.Inactive;
            isPaused = false;
        }

        /// <summary>
        /// Manually pauses ambient playback and scheduling.
        /// </summary>
        public void PauseAmbience()
        {
            if (currentState == AmbientState.Inactive || currentState == AmbientState.Paused) return;

            currentState = AmbientState.Paused;
            if (emitterSource != null && emitterSource.isPlaying)
            {
                emitterSource.Pause();
            }
        }

        /// <summary>
        /// Resumes ambient playback and scheduling if paused.
        /// </summary>
        public void ResumeAmbience()
        {
            if (currentState != AmbientState.Paused) return;

            if (emitterSource != null)
            {
                emitterSource.UnPause();
            }

            currentState = emitterSource != null && emitterSource.isPlaying ? AmbientState.Playing : AmbientState.WaitingInterval;
        }

        /// <summary>
        /// Immediately samples a new position and plays an ambient sound without waiting for the timer.
        /// </summary>
        public void TriggerRandomAmbient()
        {
            if (activePlayCoroutine != null)
            {
                StopCoroutine(activePlayCoroutine);
            }

            ConfigureMixerRouting();
            activePlayCoroutine = StartCoroutine(PlaySingleAmbientRoutine());
        }

        #endregion

        #region Playback Scheduling & Sampling

        private IEnumerator PlaybackLoopRoutine()
        {
            while (currentState != AmbientState.Inactive)
            {
                float waitDuration = UnityEngine.Random.Range(Mathf.Min(minInterval, maxInterval), Mathf.Max(minInterval, maxInterval));
                currentState = AmbientState.WaitingInterval;

                float elapsed = 0f;
                while (elapsed < waitDuration)
                {
                    if (pauseWithGame && isPaused)
                    {
                        yield return null;
                        continue;
                    }

                    if (currentState == AmbientState.Paused)
                    {
                        yield return null;
                        continue;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (currentState == AmbientState.Inactive) yield break;

                yield return PlaySingleAmbientRoutine();
            }
        }

        private IEnumerator PlaySingleAmbientRoutine()
        {
            AudioClip clip = SelectNextClip();
            if (clip == null)
            {
                yield break;
            }

            // Sample random 3D position within parent volume
            lastSampledPosition = SampleRandomPosition();
            hasSampledPosition = true;

            if (emitterObject != null)
            {
                emitterObject.transform.position = lastSampledPosition;
            }

            // Apply randomized pitch and volume
            float pitch = basePitch + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            float volume = Mathf.Clamp01(baseVolume + UnityEngine.Random.Range(-volumeJitter, volumeJitter));

            emitterSource.pitch = pitch;
            emitterSource.volume = volume;
            emitterSource.clip = clip;

            currentState = AmbientState.Playing;
            emitterSource.Play();
            onAmbientStarted?.Invoke();

            // Wait for sound to finish in unscaled time
            float clipDuration = clip.length / Mathf.Max(0.1f, Mathf.Abs(pitch));
            float playbackElapsed = 0f;

            while (playbackElapsed < clipDuration)
            {
                if (pauseWithGame && isPaused)
                {
                    yield return null;
                    continue;
                }

                if (currentState == AmbientState.Paused)
                {
                    yield return null;
                    continue;
                }

                if (currentState == AmbientState.Inactive)
                {
                    yield break;
                }

                playbackElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            onAmbientCompleted?.Invoke();
            currentState = AmbientState.WaitingInterval;
            activePlayCoroutine = null;
        }

        /// <summary>
        /// Samples a random 3D world coordinate strictly within the parent collider or fallback bounds.
        /// </summary>
        public Vector3 SampleRandomPosition()
        {
            ResolveParentCollider();

            if (parentCollider is BoxCollider box)
            {
                Vector3 size = box.size;
                Vector3 localPoint = box.center + new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f) * size.x,
                    UnityEngine.Random.Range(-0.5f, 0.5f) * size.y,
                    UnityEngine.Random.Range(-0.5f, 0.5f) * size.z
                );
                return box.transform.TransformPoint(localPoint);
            }
            else if (parentCollider is SphereCollider sphere)
            {
                Vector3 localPoint = sphere.center + UnityEngine.Random.insideUnitSphere * sphere.radius;
                return sphere.transform.TransformPoint(localPoint);
            }
            else if (parentCollider != null)
            {
                // General collider bounds fallback
                Bounds bounds = parentCollider.bounds;
                return new Vector3(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                    UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                );
            }
            else
            {
                // Fallback local volume bounds
                Vector3 localPoint = new Vector3(
                    UnityEngine.Random.Range(-0.5f, 0.5f) * fallbackVolumeSize.x,
                    UnityEngine.Random.Range(-0.5f, 0.5f) * fallbackVolumeSize.y,
                    UnityEngine.Random.Range(-0.5f, 0.5f) * fallbackVolumeSize.z
                );
                return transform.TransformPoint(localPoint);
            }
        }

        /// <summary>
        /// Selects an audio clip from local clips or AudioManager registered sounds without immediate repeats.
        /// </summary>
        private AudioClip SelectNextClip()
        {
            List<AudioClip> combinedClips = new List<AudioClip>();

            // 1. Add local inspector clips
            if (ambientClips != null)
            {
                for (int i = 0; i < ambientClips.Length; i++)
                {
                    if (ambientClips[i] != null)
                    {
                        combinedClips.Add(ambientClips[i]);
                    }
                }
            }

            // 2. Query AudioManager library by name if registered
            if (registeredSoundNames != null && AudioManager.Instance != null)
            {
                for (int i = 0; i < registeredSoundNames.Length; i++)
                {
                    string soundName = registeredSoundNames[i];
                    if (!string.IsNullOrEmpty(soundName))
                    {
                        AudioClip fetched = AudioManager.Instance.FindClip(soundName, channel);
                        if (fetched != null && !combinedClips.Contains(fetched))
                        {
                            combinedClips.Add(fetched);
                        }
                    }
                }
            }

            if (combinedClips.Count == 0)
            {
                return null;
            }

            if (combinedClips.Count == 1)
            {
                lastClipIndex = 0;
                return combinedClips[0];
            }

            // Pick random clip avoiding immediate repeat
            int nextIndex = UnityEngine.Random.Range(0, combinedClips.Count);
            if (nextIndex == lastClipIndex)
            {
                nextIndex = (nextIndex + 1) % combinedClips.Count;
            }

            lastClipIndex = nextIndex;
            return combinedClips[nextIndex];
        }

        #endregion

        #region Scene View Gizmos (Inspector First)

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.85f, 0.95f, 0.6f);

            ResolveParentCollider();

            if (parentCollider is BoxCollider box)
            {
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (parentCollider is SphereCollider sphere)
            {
                Gizmos.matrix = sphere.transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (parentCollider != null)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawWireCube(parentCollider.bounds.center, parentCollider.bounds.size);
            }
            else
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, fallbackVolumeSize);
            }

            // Draw last sampled emission position
            if (hasSampledPosition)
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.85f);
                Gizmos.DrawSphere(lastSampledPosition, 0.35f);
                Gizmos.DrawLine(transform.position, lastSampledPosition);
            }
        }

        #endregion
    }
}
