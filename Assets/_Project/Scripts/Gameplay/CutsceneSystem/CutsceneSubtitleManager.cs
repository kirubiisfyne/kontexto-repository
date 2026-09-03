using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Master.Scripts.CutsceneSystem
{
    /// <summary>
    /// Simplified, JSON-driven subtitle player designed for cutscenes, timelines, and intros/outros.
    /// Based on the DialogueManager architecture without NPC interaction overhead.
    /// </summary>
    public class CutsceneSubtitleManager : MonoBehaviour
    {
        [Header("Data Assets")]
        [SerializeField] private TextAsset subtitleJson;

        [Header("Playback Settings")]
        [Tooltip("If true, automatically advances through subtitle lines based on duration.")]
        [SerializeField] private bool autoAdvance = true;
        [Tooltip("Default seconds a line is displayed if no specific duration is set in JSON.")]
        [SerializeField] private float defaultLineDuration = 3.5f;
        [Tooltip("Allow the player to skip to the next subtitle line with a key press.")]
        [SerializeField] private bool allowInputSkip = true;
        [SerializeField] private KeyCode skipKey = KeyCode.Space;

        [Header("UI Reference")]
        [SerializeField] private CutsceneSubtitleUI subtitleUI;

        [Header("Inspector Events")]
        public UnityEvent onCutsceneStarted;
        public UnityEvent onCutsceneEnded;

        // C# Events for code subscribers
        public event Action<string> OnCutsceneStarted;
        public event Action<string> OnCutsceneEnded;
        public event Action<CutsceneLine> OnLineStarted;

        public List<CutsceneSequence> sequences = new List<CutsceneSequence>();
        public int currentShotIndex { get; private set; } = -1;
        public bool IsPlaying { get; private set; }

        private readonly Queue<CutsceneLine> lineQueue = new Queue<CutsceneLine>();
        private Coroutine playbackCoroutine;
        private bool skipRequested;

        private void Awake()
        {
            if (subtitleJson != null)
            {
                LoadFromJSON(subtitleJson.text);
            }
        }

        private void Update()
        {
            if (IsPlaying && allowInputSkip && (Input.GetKeyDown(skipKey) || Input.GetKeyDown(KeyCode.F)))
            {
                skipRequested = true;
            }
        }

        #region Public API

        /// <summary>
        /// Replaces the active JSON asset at runtime and parses sequences.
        /// Useful when reusing a single manager for both Intro and Outro in the same scene.
        /// </summary>
        public void SetSubtitleJson(TextAsset newJson)
        {
            subtitleJson = newJson;
            if (subtitleJson != null)
            {
                LoadFromJSON(subtitleJson.text);
            }
        }

        /// <summary>
        /// Plays the cutscene or shot at the specified sequence index.
        /// </summary>
        public void PlayCutscene(int sequenceIndex = 0)
        {
            if (sequenceIndex < 0 || sequenceIndex >= sequences.Count)
            {
                Debug.LogWarning($"[CutsceneSubtitleManager] Sequence index {sequenceIndex} out of range on {gameObject.name}.");
                return;
            }

            currentShotIndex = sequenceIndex;
            StartSequence(sequences[sequenceIndex]);
        }

        /// <summary>
        /// Plays a sequence matching the given name from the loaded JSON.
        /// </summary>
        public void PlayCutscene(string sequenceName)
        {
            int index = sequences.FindIndex(s => s.name.Equals(sequenceName, StringComparison.OrdinalIgnoreCase));
            if (index == -1)
            {
                Debug.LogWarning($"[CutsceneSubtitleManager] Sequence '{sequenceName}' not found on {gameObject.name}.");
                return;
            }

            currentShotIndex = index;
            StartSequence(sequences[index]);
        }

        /// <summary>
        /// Helper method alias for Timeline signals or shot-based animation events.
        /// </summary>
        public void PlayShot(string shotName) => PlayCutscene(shotName);

        /// <summary>
        /// Helper method alias for shot-based animation events using integer index.
        /// </summary>
        public void PlayShot(int shotIndex) => PlayCutscene(shotIndex);

        /// <summary>
        /// Automatically advances to and plays the next shot in sequence.
        /// </summary>
        public void PlayNextShot()
        {
            int nextIndex = currentShotIndex + 1;
            if (nextIndex < sequences.Count)
            {
                PlayCutscene(nextIndex);
            }
        }

        /// <summary>
        /// Instantly stops subtitle playback and clears the UI.
        /// </summary>
        public void StopCutscene()
        {
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }

            lineQueue.Clear();
            IsPlaying = false;
            if (subtitleUI != null) subtitleUI.Hide();
        }

        /// <summary>
        /// Parses the JSON content into playable cutscene sequences.
        /// </summary>
        public void LoadFromJSON(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return;

            try
            {
                var map = JsonUtility.FromJson<CutsceneSubtitleMap>(jsonContent);
                if (map != null)
                {
                    sequences = map.GetSequences();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CutsceneSubtitleManager] Failed parsing JSON on {gameObject.name}: {e.Message}");
            }
        }

        #endregion

        #region Playback Execution

        private void StartSequence(CutsceneSequence sequence)
        {
            StopCutscene();

            if (sequence.lines == null || sequence.lines.Count == 0) return;

            lineQueue.Clear();
            foreach (var line in sequence.lines)
            {
                lineQueue.Enqueue(line);
            }

            IsPlaying = true;
            OnCutsceneStarted?.Invoke(sequence.name);
            onCutsceneStarted?.Invoke();

            playbackCoroutine = StartCoroutine(PlaybackRoutine(sequence.name));
        }

        private IEnumerator PlaybackRoutine(string sequenceName)
        {
            while (lineQueue.Count > 0)
            {
                var line = lineQueue.Dequeue();
                skipRequested = false;

                OnLineStarted?.Invoke(line);

                if (subtitleUI != null)
                {
                    subtitleUI.DisplayLine(line);
                }

                float duration = line.duration > 0 ? line.duration : defaultLineDuration;
                float elapsed = 0f;

                if (autoAdvance)
                {
                    while (elapsed < duration)
                    {
                        if (skipRequested)
                        {
                            skipRequested = false;
                            // If typewriter effect is still running, finish it first; otherwise advance to next line
                            if (subtitleUI != null && subtitleUI.IsTyping)
                            {
                                subtitleUI.FinishTyping();
                                yield return null;
                            }
                            else
                            {
                                break;
                            }
                        }

                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                }
                else
                {
                    // Manual progression mode: wait until skip key is pressed
                    while (!skipRequested)
                    {
                        yield return null;
                    }
                    skipRequested = false;
                }
            }

            EndCutscene(sequenceName);
        }

        private void EndCutscene(string sequenceName)
        {
            IsPlaying = false;
            playbackCoroutine = null;

            if (subtitleUI != null)
            {
                subtitleUI.Hide();
            }

            OnCutsceneEnded?.Invoke(sequenceName);
            onCutsceneEnded?.Invoke();
        }

        #endregion
    }
}
