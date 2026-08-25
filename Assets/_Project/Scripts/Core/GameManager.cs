using System.Collections.Generic;
using Master.Scripts.DialogueSystem;
using Master.Scripts.SaveSystem;
using UnityEngine;
using Master.Scripts.TaskSystem;

namespace Master.Scripts
{
    public enum CutsceneMode
    {
        Intro,
        Outro
    }

    [System.Serializable]
    public struct NpcTaskAsignment
    {
        public string taskName;
        public List<GameObject> Npcs;
    }
    
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        #region Setup Data
            [Header("Level Configuration")]
            [Tooltip("The central database of all days/levels in the game.")]
            public LevelDatabase levelDatabase;

            // Current Level Sequence & Data
            public LevelSequenceData currentLevelSequence;
            public LevelData currentLevelData;
            public CutsceneMode cutsceneMode = CutsceneMode.Intro;
            public int currentLevel = 0;
            
            // Save and Load
            public PlayerData currentPlayerData;
            public List<TaskData> availableTasks;
            
            // Cross-Scene Data
            public TextAsset activeDocumentData;
            public bool pendingDocumentSuccess = false;
            public List<string> pendingAdviserFeedback = new List<string>();
        #endregion
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM("GoldenGleam");
            }
        }

        #region Level Progression Helpers
        /// <summary>
        /// Sets the active level sequence and synchronized level data.
        /// </summary>
        public void SetLevel(LevelSequenceData sequence, CutsceneMode mode = CutsceneMode.Intro)
        {
            currentLevelSequence = sequence;
            currentLevelData = sequence != null ? sequence.levelData : null;
            cutsceneMode = mode;

            if (levelDatabase != null && sequence != null)
            {
                currentLevel = levelDatabase.GetLevelIndex(sequence);
            }
        }

        /// <summary>
        /// Overload for direct LevelData references.
        /// </summary>
        public void SetLevel(LevelData levelData, CutsceneMode mode = CutsceneMode.Intro)
        {
            currentLevelData = levelData;
            cutsceneMode = mode;
            if (levelDatabase != null && levelData != null)
            {
                currentLevelSequence = levelDatabase.GetSequenceForLevelData(levelData);
                currentLevel = levelDatabase.GetLevelIndex(levelData);
            }
        }

        /// <summary>
        /// Advances the stored level to the next day in the database.
        /// </summary>
        public LevelSequenceData AdvanceToNextLevel()
        {
            if (levelDatabase == null) return null;

            LevelSequenceData nextSequence = (currentLevelSequence != null)
                ? levelDatabase.GetNextLevel(currentLevelSequence)
                : levelDatabase.GetNextLevel(currentLevelData);

            if (nextSequence != null)
            {
                SetLevel(nextSequence, CutsceneMode.Intro);
            }
            return nextSequence;
        }
        #endregion
    }
}

