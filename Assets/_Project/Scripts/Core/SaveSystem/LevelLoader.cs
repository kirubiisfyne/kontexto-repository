using UnityEngine;
using Master.Scripts.RoomSystem;

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

            // 3. Apply room active/inactive states & door angles based on LevelData
            ApplyRoomStates();
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
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 1. Restore saved player transform if save data exists for this scene
            if (playerData != null && playerData.currentScene == sceneId && playerData.HasSavedPosition())
            {
                var (pos, rot) = playerData.GetPlayerTransform();
                player.transform.position = pos;
                player.transform.eulerAngles = rot;
            }
            // 2. Move player to playerSpawnAnchorPrefab transform if assigned in LevelData
            else if (levelData != null && levelData.playerSpawnAnchorPrefab != null)
            {
                player.transform.position = levelData.playerSpawnAnchorPrefab.transform.position;
                player.transform.rotation = levelData.playerSpawnAnchorPrefab.transform.rotation;
            }

            if (cc != null) cc.enabled = true;
        }

        /// <summary>
        /// Finds all RoomControllers in the scene and activates/deactivates props and doors
        /// based on levelData.activeRoomIds.
        /// </summary>
        public void ApplyRoomStates()
        {
            if (levelData == null || levelData.activeRoomIds == null) return;

            var rooms = FindObjectsOfType<RoomController>(true);
            foreach (var room in rooms)
            {
                if (room == null || string.IsNullOrEmpty(room.roomId)) continue;

                bool isActive = levelData.activeRoomIds.Contains(room.roomId);
                room.SetRoomActive(isActive);
            }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
