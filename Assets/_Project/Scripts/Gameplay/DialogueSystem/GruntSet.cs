using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// ScriptableObject defining a reusable collection of dialogue vocal sound effects (e.g. Male or Female "hmm, ahh!" grunts).
    /// </summary>
    [CreateAssetMenu(fileName = "NewGruntSet", menuName = "Dialogue System/Grunt Set")]
    public class GruntSet : ScriptableObject
    {
        [Tooltip("Descriptive label (e.g. Male, Female).")]
        public string setName;

        [Header("Vocal Audio Clips")]
        [Tooltip("Vocal sound effects ('hmm, ahh!' audio clips) for this set. A random clip is selected on initiation, skip, or line progression.")]
        public AudioClip[] clips;

        [Header("Pitch Tuning")]
        [Tooltip("Base pitch multiplier for this grunt set.")]
        [Range(0.5f, 1.5f)] public float basePitch = 1.0f;

        /// <summary>
        /// Selects a random AudioClip from this set, or null if unassigned.
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
