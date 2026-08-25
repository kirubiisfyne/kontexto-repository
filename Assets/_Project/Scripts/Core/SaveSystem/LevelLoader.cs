using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Master.Scripts.RoomSystem;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Scene-local loader. Resolves the active LevelData dynamically,
    /// initializes the task tracker, manages room states, and handles save/load & level transitions.
    /// </summary>
    [RequireComponent(typeof(LevelTaskTracker))]
    public class LevelLoader : MonoBehaviour
    {
        public static LevelLoader Current { get; private set; }

        [Header("Database & Defaults")]
        [Tooltip("The central database containing all days/levels in order.")]
        public LevelDatabase levelDatabase;

        [Tooltip("Fallback LevelData if no database or save data exists.")]
        public LevelData defaultLevelData;

        [Header("Editor Debugging")]
        [Tooltip("If assigned, overrides save data and GameManager for instant testing in Editor.")]
        public LevelData editorOverrideLevel;

        [Header("Runtime State")]
        [SerializeField] private LevelData currentLevelData;
        public LevelData ActiveLevelData => currentLevelData;

        private PlayerData playerData;
        private string sceneId;
        private LevelTaskTracker taskTracker;

        private void Awake()
        {
            Current = this;
            taskTracker = GetComponent<LevelTaskTracker>();

            ResolveAndLoadLevel();
        }

        /// <summary>
        /// Resolves the target level from editor override, GameManager, save data, or database defaults.
        /// </summary>
        public void ResolveAndLoadLevel()
        {
            playerData = SaveManager.Load();

            LevelData targetLevel = ResolveLevelData();
            if (targetLevel == null)
            {
                Debug.LogWarning($"[LevelLoader] No LevelData could be resolved on {gameObject.name}.");
                return;
            }

            LoadLevel(targetLevel);
        }

        private LevelData ResolveLevelData()
        {
            #if UNITY_EDITOR
            if (editorOverrideLevel != null)
            {
                Debug.Log($"<color=yellow>[LevelLoader]</color> Using Editor Override Level: {editorOverrideLevel.name}");
                return editorOverrideLevel;
            }
            #endif

            // 1. Cross-scene explicit assignment from GameManager
            if (GameManager.Instance != null && GameManager.Instance.currentLevelData != null)
            {
                return GameManager.Instance.currentLevelData;
            }

            // 2. Resolve from Save Data and Database
            if (levelDatabase != null)
            {
                return levelDatabase.GetFirstIncompleteLevel(playerData);
            }

            // 3. Fallback
            return defaultLevelData != null ? defaultLevelData : currentLevelData;
        }

        /// <summary>
        /// Applies the specified LevelData to the scene.
        /// </summary>
        public void LoadLevel(LevelData data)
        {
            currentLevelData = data;
            sceneId = data.sceneId;

            // Keep GameManager synchronized
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentPlayerData = playerData;
                GameManager.Instance.currentLevelData = data;
                if (levelDatabase != null)
                {
                    GameManager.Instance.currentLevel = levelDatabase.GetLevelIndex(data);
                }
            }

            // 1. Initialize Task Tracker (spawns prefabs and restores state)
            taskTracker.Initialize(currentLevelData, playerData, SaveGame);

            // 2. Restore player transform or place at spawn anchor
            RestorePlayerPosition();

            // 3. Apply room active/inactive states
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

        /// <summary>
        /// Marks the current day completed, updates save data, queries the next day,
        /// and transitions into the new day cleanly behind the screen transition.
        /// </summary>
        public void AdvanceToNextLevel()
        {
            if (levelDatabase == null || currentLevelData == null)
            {
                Debug.LogWarning("[LevelLoader] Cannot advance: LevelDatabase or CurrentLevelData is missing.");
                return;
            }

            // 1. Mark current level completed in save data
            CompleteLevel();

            // 2. Look up the next day in the database (e.g. Day 1 -> Day 2)
            LevelData nextLevel = levelDatabase.GetNextLevel(currentLevelData);

            if (nextLevel != null)
            {
                Debug.Log($"<color=green>[LevelLoader]</color> Advancing from {currentLevelData.name} to {nextLevel.name}!");

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.currentLevelData = nextLevel;
                    GameManager.Instance.currentLevel = levelDatabase.GetLevelIndex(nextLevel);
                }

                // Clear mid-day position in save file so player spawns at the next day's spawn anchor
                playerData.currentScene = nextLevel.sceneId;
                playerData.playerPosition = null;
                SaveManager.Save(playerData);

                // Play transition and reload scene
                StartCoroutine(TransitionToNextDayRoutine());
            }
            else
            {
                Debug.Log("<color=gold>[LevelLoader]</color> All levels in the database have been completed!");
            }
        }

        private IEnumerator TransitionToNextDayRoutine()
        {
            if (TransitionManager.Instance != null)
            {
                yield return TransitionManager.Instance.PlayTransitionAndWait("transition");
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // ── Public Save / Load ──

        public void SaveGame()
        {
            CapturePlayerTransform();
            playerData.currentScene = sceneId;
            SaveManager.Save(playerData);
        }

        public void LoadGame()
        {
            playerData = SaveManager.Load();

            if (GameManager.Instance != null)
                GameManager.Instance.currentPlayerData = playerData;

            RestorePlayerPosition();
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
            else if (currentLevelData != null && currentLevelData.playerSpawnAnchorPrefab != null)
            {
                player.transform.position = currentLevelData.playerSpawnAnchorPrefab.transform.position;
                player.transform.rotation = currentLevelData.playerSpawnAnchorPrefab.transform.rotation;
            }

            if (cc != null) cc.enabled = true;
        }

        /// <summary>
        /// Finds all RoomControllers in the scene and activates/deactivates props and doors
        /// based on currentLevelData.activeRoomIds.
        /// </summary>
        public void ApplyRoomStates()
        {
            if (currentLevelData == null || currentLevelData.activeRoomIds == null) return;

            var rooms = FindObjectsOfType<RoomController>(true);
            foreach (var room in rooms)
            {
                if (room == null || string.IsNullOrEmpty(room.roomId)) continue;

                bool isActive = currentLevelData.activeRoomIds.Contains(room.roomId);
                room.SetRoomActive(isActive);
            }
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
