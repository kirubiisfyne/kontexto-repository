using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    [CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/DialogueClasses")]
    public class DialogueClasses : ScriptableObject
    {
        public List<DialogueLine> lines;
    }

    [System.Serializable]
    public class DialogueLine
    {
        public SpeakerProfile speaker;
        [TextArea(3, 10)] public string text;
    }
    
    [CreateAssetMenu(fileName = "NewSpeaker", menuName = "Dialogue/Speaker Profile")]
    public class SpeakerProfile : ScriptableObject
    {
        public string characterName;
        public Sprite portrait;
        public Color nameColor = Color.white;
    }
}
