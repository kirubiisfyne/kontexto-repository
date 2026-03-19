using System.Collections.Generic;
using Master.Scripts.DialogueSystem;
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
            // Loading Documents
            public List<TextAsset> documentData;
            
            // Save and Load
            public PlayerData currentPlayerData;
            
            public int currentLevel = 0;
            public List<TaskData> availableTasks;
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

        public TextAsset GetNextDocumentData(int index)
        {
            return documentData[index];
        }
    }
}
