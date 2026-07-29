using UnityEngine;
using System.Collections.Generic;

namespace Master.Scripts.DialogueSystem
{
    [System.Serializable]
    public class SpeakerProfile
    {
        [Tooltip("The name as it appears in the JSON (e.g. 'Adviser', 'Kiko')")]
        public string characterName;
        
        [Tooltip("The portrait image to display in the UI")]
        public Sprite portraitSprite;
    }

    [CreateAssetMenu(fileName = "SpeakerDatabase", menuName = "Dialogue System/Speaker Database")]
    public class SpeakerDatabase : ScriptableObject
    {
        public List<SpeakerProfile> profiles = new List<SpeakerProfile>();

        /// <summary>
        /// A quick helper method to search the database by character name.
        /// </summary>
        public SpeakerProfile GetProfile(string name)
        {
            return profiles.Find(p => p.characterName == name);
        }
    }
}
