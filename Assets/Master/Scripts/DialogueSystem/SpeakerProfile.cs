using UnityEngine;

[CreateAssetMenu(fileName = "NewSpeaker", menuName = "Dialogue/Speaker Profile")]
public class SpeakerProfile : ScriptableObject
{
    public string characterName;
    public Sprite portrait;
    public Color nameColor = Color.white;
}