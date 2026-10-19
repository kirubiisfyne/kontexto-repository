using UnityEngine;
using System.Collections.Generic;

namespace Master.Scripts.DialogueSystem
{
    public enum SpeakerGender
    {
        Unspecified,
        Male,
        Female
    }

    [System.Serializable]
    public class SpeakerProfile
    {
        [Tooltip("The name as it appears in the JSON (e.g. 'Adviser', 'Kiko')")]
        public string characterName;
        
        [Tooltip("The portrait image to display in the UI")]
        public Sprite portraitSprite;

        [Header("Voice / Grunt Settings")]
        [Tooltip("Speaker gender (Male or Female) to automatically use the corresponding GruntSet on the SpeakerDatabase.")]
        public SpeakerGender gender = SpeakerGender.Unspecified;

        [Tooltip("Optional custom GruntSet asset for this character. If assigned, overrides the global Male/Female GruntSet.")]
        public GruntSet gruntSet;

        [Tooltip("Additional pitch multiplier for this speaker (stacks with GruntSet pitch).")]
        [Range(0.5f, 1.5f)] public float voicePitch = 1.0f;

        /// <summary>
        /// Selects a vocal clip from the character's GruntSet, or falls back to the database Male/Female GruntSet.
        /// </summary>
        public AudioClip GetRandomVoiceClip(SpeakerDatabase db = null)
        {
            if (gruntSet != null)
            {
                var clip = gruntSet.GetRandomClip();
                if (clip != null) return clip;
            }

            if (db != null)
            {
                if (gender == SpeakerGender.Male && db.maleGruntSet != null)
                    return db.maleGruntSet.GetRandomClip();
                if (gender == SpeakerGender.Female && db.femaleGruntSet != null)
                    return db.femaleGruntSet.GetRandomClip();
            }

            return null;
        }

        /// <summary>
        /// Calculates total pitch multiplier combining character pitch and GruntSet pitch.
        /// </summary>
        public float GetTotalPitch(SpeakerDatabase db = null)
        {
            float pitch = voicePitch;
            if (gruntSet != null)
            {
                pitch *= gruntSet.basePitch;
            }
            else if (db != null)
            {
                if (gender == SpeakerGender.Male && db.maleGruntSet != null)
                    pitch *= db.maleGruntSet.basePitch;
                else if (gender == SpeakerGender.Female && db.femaleGruntSet != null)
                    pitch *= db.femaleGruntSet.basePitch;
            }
            return pitch;
        }
    }

    [CreateAssetMenu(fileName = "SpeakerDatabase", menuName = "Dialogue System/Speaker Database")]
    public class SpeakerDatabase : ScriptableObject
    {
        [Header("Global Grunt Sets")]
        [Tooltip("Shared GruntSet asset for male speakers.")]
        public GruntSet maleGruntSet;

        [Tooltip("Shared GruntSet asset for female speakers.")]
        public GruntSet femaleGruntSet;

        [Header("Profiles")]
        public List<SpeakerProfile> profiles = new List<SpeakerProfile>();

        /// <summary>
        /// A quick helper method to search the database by character name.
        /// </summary>
        public SpeakerProfile GetProfile(string name)
        {
            return profiles.Find(p => p.characterName == name);
        }

        /// <summary>
        /// Resolves the voice clip and total pitch for a named speaker.
        /// </summary>
        public AudioClip ResolveVoiceClip(string name, out float pitchMultiplier)
        {
            pitchMultiplier = 1.0f;
            var profile = GetProfile(name);
            if (profile != null)
            {
                pitchMultiplier = profile.GetTotalPitch(this);
                return profile.GetRandomVoiceClip(this);
            }
            return null;
        }
    }
}
