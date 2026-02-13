using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    [CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation")]
    public class Conversation : ScriptableObject
    {
        public List<DialogueLine> lines;
    }

    [System.Serializable]
    public class DialogueLine
    {
        public SpeakerProfile speaker;
        [TextArea(3, 10)] public string text;
    }
}
