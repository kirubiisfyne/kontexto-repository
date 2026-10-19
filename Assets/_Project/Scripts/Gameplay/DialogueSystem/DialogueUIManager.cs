using System.Collections;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// Handles the visual representation of the dialogue on the UI.
    /// </summary>
    public class DialogueUIManager : MonoBehaviour
    {
        public static DialogueUIManager Instance;
        
        [Header("UI")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerNameText;
        [SerializeField] private TMP_Text dialogueText;
        
        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private float transitionDuration = 0.5f;
        
        [Header("Settings")]
        [Range(0.1f, 1.0f)] [SerializeField] private float textSpeed = 0.5f; 
        
        [Header("Data")]
        [Tooltip("Drag and drop your SpeakerDatabase ScriptableObject here")]
        [SerializeField] private SpeakerDatabase speakerDatabase;
        [SerializeField] private UnityEngine.UI.Image speakerProfileImage;


        [Header("Audio Settings")]
        [Tooltip("Optional speech blip audio clip played as letters appear.")]
        [SerializeField] private AudioClip typingAudioClip;
        [Tooltip("AudioSource used for speech blip. If unassigned, one will be fetched or created.")]
        [SerializeField] private AudioSource typingAudioSource;
        [Range(1, 5)] [SerializeField] private int blipCharacterFrequency = 2;
        [SerializeField] private bool randomPitch = true;
        [Range(0.8f, 1.2f)] [SerializeField] private float minPitch = 0.95f;
        [Range(0.8f, 1.2f)] [SerializeField] private float maxPitch = 1.05f;
        [Range(0f, 1f)] [SerializeField] private float blipVolume = 0.25f;

        [Header("Dialogue Vocal / Voice SFX")]
        [Tooltip("Default voice SFX ('hmm, ahh!' vocal cue) played on dialogue trigger, skip, or next line if speaker profile has no custom clip.")]
        [SerializeField] private AudioClip defaultVoiceSFX;
        [Tooltip("Optional default GruntSet asset used if speaker has no profile or matching gender.")]
        [SerializeField] private GruntSet defaultGruntSet;
        [Tooltip("Optional specific SFX played when skipping text. If unassigned, falls back to the speaker voice clip.")]
        [SerializeField] private AudioClip skipSFX;
        [Tooltip("Optional specific SFX played when advancing to the next line. If unassigned, falls back to the speaker voice clip.")]
        [SerializeField] private AudioClip nextLineSFX;
        [Tooltip("Optional SFX played when dialogue panel closes.")]
        [SerializeField] private AudioClip dialogueCloseSFX;
        [SerializeField] private bool randomVoicePitch = true;
        [Range(0.8f, 1.2f)] [SerializeField] private float minVoicePitch = 0.95f;
        [Range(0.8f, 1.2f)] [SerializeField] private float maxVoicePitch = 1.05f;
        [Range(0f, 1f)] [SerializeField] private float voiceVolume = 0.8f;

        public bool IsTyping { get; private set; }
        
        private string currentFullText;
        private string currentSpeaker;
        private Coroutine typingCoroutine;
        private Coroutine hideCoroutine;
        
        private readonly StringBuilder _sb = new StringBuilder();
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
            
            dialoguePanel.SetActive(false);
            
            if (speakerNameText != null) speakerNameText.raycastTarget = false;
            if (dialogueText != null) dialogueText.raycastTarget = false;
            if (speakerProfileImage != null) speakerProfileImage.raycastTarget = false;

            if (typingAudioSource == null)
            {
                typingAudioSource = GetComponent<AudioSource>();
                if (typingAudioSource == null)
                {
                    typingAudioSource = gameObject.AddComponent<AudioSource>();
                    typingAudioSource.playOnAwake = false;
                }
            }

            if (typingAudioSource != null && AudioManager.Instance != null && typingAudioSource.outputAudioMixerGroup == null)
            {
                typingAudioSource.outputAudioMixerGroup = AudioManager.Instance.GetChannelGroup(AudioChannel.SFX);
            }
        }

        private void Start()
        {
            if (typingAudioSource != null && typingAudioSource.outputAudioMixerGroup == null && AudioManager.Instance != null)
            {
                typingAudioSource.outputAudioMixerGroup = AudioManager.Instance.GetChannelGroup(AudioChannel.SFX);
            }
        }

        public void Show()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            dialoguePanel.SetActive(true);
            if (animator != null) 
            {
                animator.ResetTrigger("Out");
                animator.SetTrigger("In");
            }
        }

        public void Hide()
        {
            PlayCloseSFX();

            if (gameObject.activeInHierarchy)
            {
                if (animator != null) 
                {
                    animator.ResetTrigger("In");
                    animator.SetTrigger("Out");
                }
                hideCoroutine = StartCoroutine(HideRoutine());
            }
            else
            {
                dialoguePanel.SetActive(false);
            }
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(transitionDuration);
            dialoguePanel.SetActive(false);
            hideCoroutine = null;
        }

        /// <summary>
        /// Updates the UI with new dialogue content.
        /// </summary>
        public void UpdateDialogueView(DialogueLine line)
        {
            currentSpeaker = line != null ? line.speaker : null;

            bool isAdvancing = dialoguePanel.activeSelf && hideCoroutine == null;
            if (isAdvancing)
            {
                PlayNextLineSFX(currentSpeaker);
            }
            else
            {
                PlayVoiceSFX(currentSpeaker);
            }

            Show(); 
            _sb.Clear();
            _sb.Append(!string.IsNullOrEmpty(line.speaker) ? line.speaker : "");
            speakerNameText.SetText(_sb);

            if (speakerProfileImage != null)
            {
                if (speakerDatabase != null)
                {
                    var profile = speakerDatabase.GetProfile(line.speaker);
                    if (profile != null && profile.portraitSprite != null)
                    {
                        speakerProfileImage.sprite = profile.portraitSprite;
                        speakerProfileImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        speakerProfileImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    speakerProfileImage.gameObject.SetActive(false);
                }
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeSentence(line.text));
        }

        private IEnumerator TypeSentence(string sentence)
        {
            IsTyping = true;
            currentFullText = sentence;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.text = sentence;
            _sb.Clear();
            _sb.Append(sentence);
            dialogueText.SetText(_sb);
            
            // Force mesh update so the full text geometry is pre-calculated once, 
            // preventing array resizing during the character reveal.
            dialogueText.ForceMeshUpdate();

            float waitTime = 0.02f / textSpeed;
            // Cache WaitForSeconds to prevent generating garbage every character loop
            WaitForSeconds wait = new WaitForSeconds(waitTime);
            
            int visibleCount = 0;
            foreach (char letter in sentence)
            {
                dialogueText.maxVisibleCharacters++;
                visibleCount++;

                if (!char.IsWhiteSpace(letter) && visibleCount % blipCharacterFrequency == 0)
                {
                    PlayTypingBlip();
                }

                yield return wait;
            }
            
            IsTyping = false;
        }

        /// <summary>
        /// Immediately shows the full text if currently typing.
        /// </summary>
        public void FinishTyping()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.maxVisibleCharacters = currentFullText != null ? currentFullText.Length : 0;
            IsTyping = false;
            PlaySkipSFX(currentSpeaker);
        }
        private void PlayTypingBlip()
        {
            if (typingAudioClip != null && typingAudioSource != null)
            {
                typingAudioSource.pitch = randomPitch ? UnityEngine.Random.Range(minPitch, maxPitch) : 1f;
                typingAudioSource.PlayOneShot(typingAudioClip, blipVolume);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("UIPop0", randomPitch, minPitch, maxPitch, blipVolume);
            }
        }

        #region Vocal & UI SFX Playback

        /// <summary>
        /// Resolves the active vocal clip for a given speaker using:
        /// 1. DialogueManager.LastInteracted NPC override
        /// 2. SpeakerDatabase profile voice clips
        /// 3. defaultVoiceSFX on DialogueUIManager
        /// </summary>
        public AudioClip ResolveVoiceClip(string speakerName, out float pitchMultiplier)
        {
            pitchMultiplier = 1f;

            // 1. Direct NPC override on DialogueManager
            if (DialogueManager.LastInteracted != null && DialogueManager.LastInteracted.NpcVoiceClip != null)
            {
                return DialogueManager.LastInteracted.NpcVoiceClip;
            }

            // 2. Speaker profile / GruntSet in SpeakerDatabase
            if (!string.IsNullOrEmpty(speakerName) && speakerDatabase != null)
            {
                var clip = speakerDatabase.ResolveVoiceClip(speakerName, out float resolvedPitch);
                if (clip != null)
                {
                    pitchMultiplier = resolvedPitch;
                    return clip;
                }
            }

            // 3. Fallback to default GruntSet on DialogueUIManager
            if (defaultGruntSet != null)
            {
                pitchMultiplier = defaultGruntSet.basePitch;
                var clip = defaultGruntSet.GetRandomClip();
                if (clip != null) return clip;
            }

            // 4. Fallback to default voice SFX on DialogueUIManager
            return defaultVoiceSFX;
        }

        /// <summary>
        /// Plays the speaker's vocal SFX ("hmm, ahh!" voice grunt) or default dialogue voice cue.
        /// </summary>
        public void PlayVoiceSFX(string speakerName = null)
        {
            AudioClip clip = ResolveVoiceClip(speakerName, out float pitchMultiplier);
            PlayDialogueSound(clip, pitchMultiplier);
        }

        /// <summary>
        /// Plays skip SFX when skipping typewriter text, falling back to the speaker voice cue if unassigned.
        /// </summary>
        public void PlaySkipSFX(string speakerName = null)
        {
            if (skipSFX != null)
            {
                PlayDialogueSound(skipSFX, 1f);
            }
            else
            {
                PlayVoiceSFX(speakerName);
            }
        }

        /// <summary>
        /// Plays progression SFX when advancing to the next dialogue line, falling back to the speaker voice cue if unassigned.
        /// </summary>
        public void PlayNextLineSFX(string speakerName = null)
        {
            if (nextLineSFX != null)
            {
                PlayDialogueSound(nextLineSFX, 1f);
            }
            else
            {
                PlayVoiceSFX(speakerName);
            }
        }

        /// <summary>
        /// Plays optional SFX when the dialogue panel closes.
        /// </summary>
        public void PlayCloseSFX()
        {
            if (dialogueCloseSFX != null)
            {
                PlayDialogueSound(dialogueCloseSFX, 1f);
            }
        }

        private void PlayDialogueSound(AudioClip clip, float pitchMultiplier)
        {
            if (clip == null) return;

            float minP = minVoicePitch * pitchMultiplier;
            float maxP = maxVoicePitch * pitchMultiplier;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip, randomVoicePitch, minP, maxP, voiceVolume);
            }
            else if (typingAudioSource != null)
            {
                typingAudioSource.pitch = randomVoicePitch ? UnityEngine.Random.Range(minP, maxP) : pitchMultiplier;
                typingAudioSource.PlayOneShot(clip, voiceVolume);
            }
        }

        #endregion
    }
}
