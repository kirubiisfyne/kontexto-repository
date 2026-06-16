using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.DialogueSystem
{
    /// <summary>
    /// Root object for JSON dialogue mapping.
    /// </summary>
    [System.Serializable]
    public class ConversationMap 
    {
        public List<Conversation> conversationMap;
    }
    
    /// <summary>
    /// Represents a single conversation branch.
    /// </summary>
    [System.Serializable]
    public class Conversation
    {
        public string name;
        public List<DialogueLine> lines;
    }

    /// <summary>
    /// A single line of dialogue with a speaker name and text.
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        public string speaker;
        public string text;
    }
}
