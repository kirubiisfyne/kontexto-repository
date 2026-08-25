using System.Collections.Generic;
using Master.Scripts.DialogueSystem;
using Master.Scripts.SaveSystem;
using UnityEngine;
using Master.Scripts.TaskSystem;

namespace Master.Scripts
{
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

            // Current Level Data
            public LevelData currentLevelData;
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
        /// Sets the active level and updates the level index.
        /// </summary>
        public void SetLevel(LevelData levelData)
        {
            currentLevelData = levelData;
            if (levelDatabase != null && levelData != null)
            {
                currentLevel = levelDatabase.GetLevelIndex(levelData);
            }
        }

        /// <summary>
        /// Advances the stored level to the next day in the database.
        /// </summary>
        public LevelData AdvanceToNextLevel()
        {
            if (levelDatabase == null || currentLevelData == null) return null;

            LevelData nextLevel = levelDatabase.GetNextLevel(currentLevelData);
            if (nextLevel != null)
            {
                SetLevel(nextLevel);
            }
            return nextLevel;
        }
        #endregion
    }
}
