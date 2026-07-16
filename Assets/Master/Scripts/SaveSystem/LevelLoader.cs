using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Scene-local loader. Reads a LevelData asset, initializes the task tracker,
    /// and handles saving/loading player state.
    /// </summary>
    [RequireComponent(typeof(LevelTaskTracker))]
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Current { get; private set; }

        [Header("Configuration")]
        [Tooltip("The LevelData asset for this scene.")]
        public LevelData levelData;



        private PlayerData playerData;
        private string sceneId;
        
        private LevelTaskTracker taskTracker;

        private void Awake()
        {
            Current = this;

            if (levelData == null)
            {
                //Debug.LogWarning($"LevelLoader on {gameObject.name}: No LevelData assigned. Nothing to spawn.");
                return;
            }

            sceneId = levelData.sceneId;

            // 1. Load save data
            playerData = SaveManager.Load();

            // Keep GameManager in sync
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentPlayerData = playerData;
                GameManager.Instance.currentLevelData = levelData;
            }

            // 2. Initialize Task Tracker
            taskTracker = GetComponent<LevelTaskTracker>();
            taskTracker.Initialize(levelData, playerData, SaveGame);

            // Restore player position if save belongs to this scene
            RestorePlayerPosition();
        }

        /// <summary>
        /// Wrapper mapping to the Task Tracker, maintaining compatibility with events/editors.
        /// </summary>
        public bool AreAllTasksCompleted()
        {
            return taskTracker != null && taskTracker.AreAllTasksCompleted();
        }

        /// <summary>
        /// Call this AFTER the outro cutscene. Wrapper mapped to the Task Tracker.
        /// </summary>
        public void CompleteLevel()
        {
            if (taskTracker != null)
            {
                taskTracker.CompleteLevel();
            }
        }

        // ── Public Save / Load ──

        public void SaveGame()
        {


            CapturePlayerTransform();
            playerData.currentScene = sceneId;
            SaveManager.Save(playerData);

            //Debug.Log($"LevelLoader: Game saved in '{sceneId}'.");
        }

        public void LoadGame()
        {
            playerData = SaveManager.Load();

            if (GameManager.Instance != null)
                GameManager.Instance.currentPlayerData = playerData;

            RestorePlayerPosition();

            //Debug.Log($"LevelLoader: Game loaded in '{sceneId}'.");
        }

        // ── Player Transform Persistence ──

        private void CapturePlayerTransform()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerData.SetPlayerTransform(player.transform.position, player.transform.eulerAngles);
            }
        }

        private void RestorePlayerPosition()
        {
            if (playerData.currentScene != sceneId || !playerData.HasSavedPosition())
                return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            var (pos, rot) = playerData.GetPlayerTransform();
            player.transform.position = pos;
            player.transform.eulerAngles = rot;

            if (cc != null) cc.enabled = true;

            //Debug.Log($"LevelLoader: Restored player position to {pos} in '{sceneId}'.");
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
