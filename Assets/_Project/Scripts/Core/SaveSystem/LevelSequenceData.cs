using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Encapsulates a full playable day/level, bundling its gameplay LevelData
    /// with ordered lists of Intro and Outro cutscene shot prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "New LevelSequenceData", menuName = "Levels/Level Sequence Data")]
    public class LevelSequenceData : ScriptableObject
    {
        [Header("Gameplay Configuration")]
        [Tooltip("The core gameplay configuration (tasks, rooms, spawn anchor) for this day/level.")]
        public LevelData levelData;

        [Header("Cutscene Sequences")]
        [Tooltip("Ordered prefabs instantiated in sequence for the level's Intro cutscene.")]
        public List<GameObject> introCutscenePrefabs = new List<GameObject>();

        [Tooltip("Ordered prefabs instantiated in sequence for the level's Outro cutscene.")]
        public List<GameObject> outroCutscenePrefabs = new List<GameObject>();

        [Header("Dialogue Configuration")]
        [Tooltip("Optional cutscene dialogue JSON asset for this day/level containing intro and outro dialogue.")]
        public TextAsset cutsceneDialogueJson;

        public string SceneId => levelData != null ? levelData.sceneId : string.Empty;
        public bool HasIntro => introCutscenePrefabs != null && introCutscenePrefabs.Count > 0;
        public bool HasOutro => outroCutscenePrefabs != null && outroCutscenePrefabs.Count > 0;
    }
}
