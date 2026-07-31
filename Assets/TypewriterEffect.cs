using TMPro;
using UnityEngine;
using System.Collections;

public class TypewriterEffect : MonoBehaviour
{
    public TMP_Text subtitleText;

    [TextArea(2,5)]
    public string[] dialogue;

    public float typingSpeed = 0.04f;
    public float sentenceDelay = 1.5f;

    private void Start()
    {
        StartCoroutine(TypeDialogue());
    }

    IEnumerator TypeDialogue()
    {
        foreach (string sentence in dialogue)
        {
            subtitleText.text = "";

            foreach (char letter in sentence)
            {
                subtitleText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(sentenceDelay);
        }

        subtitleText.text = "";
    }
}