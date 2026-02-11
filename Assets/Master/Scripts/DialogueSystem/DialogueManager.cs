using UnityEngine;
using UnityEngine.UI;
using TMPro; // Ensure you have TextMeshPro imported
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI Components")]
    [SerializeField] public GameObject dialoguePanel;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    private Queue<DialogueLine> linesQueue;
    private Conversation currentConversation;
    private bool isTyping;
    private string currentFullText;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep across scenes
        }
        else
        {
            Destroy(gameObject);
        }

        linesQueue = new Queue<DialogueLine>();
        dialoguePanel.SetActive(false); // Hide UI at start
    }

    public void StartConversation(Conversation conversation)
    {
        currentConversation = conversation;
        dialoguePanel.SetActive(true);
        linesQueue.Clear();

        // Enqueue all lines from the ScriptableObject
        foreach (DialogueLine line in conversation.lines)
        {
            linesQueue.Enqueue(line);
        }

        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // If typing, finish the line instantly
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = currentFullText;
            isTyping = false;
            return;
        }

        if (linesQueue.Count == 0)
        {
            EndConversation();
            return;
        }

        DialogueLine line = linesQueue.Dequeue();

        // Update UI
        if (line.speaker != null)
        {
            speakerNameText.text = line.speaker.characterName;
            speakerNameText.color = line.speaker.nameColor;
            speakerPortrait.sprite = line.speaker.portrait;
            
            // Toggle portrait visibility if null
            speakerPortrait.gameObject.SetActive(line.speaker.portrait != null);
        }

        // Invoke Line Events
        line.onLineStart?.Invoke();

        // Start Typing Effect
        StartCoroutine(TypeSentence(line.text));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        currentFullText = sentence;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return null; // Wait one frame per letter
        }

        isTyping = false;
    }

    void EndConversation()
    {
        dialoguePanel.SetActive(false);
        
        // Invoke End Events
        currentConversation.onConversationEnd?.Invoke();
        currentConversation = null;
    }
}