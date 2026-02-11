using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewConversation", menuName = "Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    // A list of lines makes up the conversation
    public List<DialogueLine> lines;
    
    // Event triggered when this specific conversation ends
    public UnityEvent onConversationEnd;
}

[System.Serializable]
public class DialogueLine
{
    public SpeakerProfile speaker;
    [TextArea(3, 10)] public string text;
    
    // Optional: Trigger an event when this specific line starts (e.g., play sound, shake camera)
    public UnityEvent onLineStart;
}