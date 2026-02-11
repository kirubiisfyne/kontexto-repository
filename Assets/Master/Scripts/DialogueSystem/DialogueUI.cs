using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Settings")]
    [Range(0.1f, 1.0f)] [SerializeField] private float textSpeed = 0.5f; 

    public bool IsTyping { get; private set; }
    private string currentFullText;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        Hide();
    }

    public void Show() => dialoguePanel.SetActive(true);
    public void Hide() => dialoguePanel.SetActive(false);

    public void UpdateDialogueView(DialogueLine line)
    {
        Show(); 

        if (line.speaker != null)
        {
            speakerNameText.text = line.speaker.characterName;
            speakerNameText.color = line.speaker.nameColor;
            speakerPortrait.sprite = line.speaker.portrait;
            speakerPortrait.gameObject.SetActive(line.speaker.portrait != null);
        }
        else
        {
            speakerNameText.text = "";
            speakerPortrait.gameObject.SetActive(false);
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(line.text));
    }

    IEnumerator TypeSentence(string sentence)
    {
        IsTyping = true;
        currentFullText = sentence;
        dialogueText.text = "";

        float waitTime = 0.02f / textSpeed;

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(waitTime);
        }

        IsTyping = false;
    }

    public void FinishTyping()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        dialogueText.text = currentFullText;
        IsTyping = false;
    }
}