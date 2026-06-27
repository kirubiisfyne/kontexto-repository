using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts
{
    /// <summary>
    /// Scene-local loader. Reads a LevelData asset, instantiates task prefabs,
    /// and restores completed tasks from the save file.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        /// <summary>
        /// Convenience accessor for the current scene's loader.
        /// Set in Awake, cleared in OnDestroy. NOT a singleton.
        /// </summary>
        public static LevelLoader Current { get; private set; }

        [Header("Configuration")]
        [Tooltip("The LevelData asset for this scene.")]
        public LevelData levelData;

        [Header("Runtime (Read-Only)")]
        [Tooltip("All Giver/Both HostTaskManagers spawned by this loader.")]
        [SerializeField] private List<HostTaskManager> spawnedGivers = new List<HostTaskManager>();

        private PlayerData playerData;
        private string sceneId;

        private void Awake()
        {
            Current = this;

            if (levelData == null)
            {
                Debug.LogWarning($"LevelLoader on {gameObject.name}: No LevelData assigned. Nothing to spawn.");
                return;
            }

            sceneId = levelData.sceneId;

            // 1. Load save data
            playerData = SaveManager.Load();

            // Keep GameManager in sync
            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentPlayerData = playerData;
            }

            // 2. Instantiate all task prefabs
            foreach (var entry in levelData.taskEntries)
            {
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"LevelLoader: A task entry in '{levelData.name}' has a null prefab. Skipping.");
                    continue;
                }

                GameObject instance;

                if (entry.usePrefabTransform)
                {
                    // Prefab keeps its own saved position/rotation
                    instance = Instantiate(entry.prefab);
                }
                else
                {
                    instance = Instantiate(
                        entry.prefab,
                        entry.spawnPosition,
                        Quaternion.Euler(entry.spawnRotation)
                    );
                }

                // 3. Gather all Giver/Both managers in this prefab tree
                var managers = instance.GetComponentsInChildren<HostTaskManager>();

                foreach (var mgr in managers)
                {
                    if (mgr.hostType == HostType.Giver || mgr.hostType == HostType.Both)
                    {
                        spawnedGivers.Add(mgr);

                        if (mgr.task == null)
                        {
                            Debug.LogWarning($"LevelLoader: HostTaskManager on '{mgr.gameObject.name}' has no TaskData assigned.");
                            continue;
                        }

                        if (string.IsNullOrEmpty(mgr.task.taskId))
                        {
                            Debug.LogWarning($"LevelLoader: TaskData '{mgr.task.taskName}' has an empty taskId. Save/load will not track this task.");
                            continue;
                        }

                        // 4. If this task was previously completed, fast-forward it
                        if (playerData.IsTaskCompleted(sceneId, mgr.task.taskId))
                        {
                            RestoreCompletedTask(mgr);
                        }
                        else
                        {
                            // 5. Subscribe to future completion for saving
                            var capturedId = mgr.task.taskId;
                            mgr.events.onCompleted.AddListener(() => OnTaskCompleted(capturedId));
                        }
                    }
                }
            }

            Debug.Log($"LevelLoader: Spawned {spawnedGivers.Count} task(s) for '{sceneId}'.");

            // Post-spawn check: if all tasks were restored as completed, log it immediately
            if (spawnedGivers.Count > 0 && AreAllTasksCompleted())
            {
                Debug.Log($"LevelLoader: All tasks in '{sceneId}' are already COMPLETED (restored from save). Ready for level completion.");
            }
        }

        /// <summary>
        /// Fast-forwards a task to Completed status.
        /// Fires the full status chain (Active → ReadyToComplete → Completed)
        /// so all intermediate Inspector-wired events execute in order
        /// (e.g., dialogue index updates).
        /// </summary>
        private void RestoreCompletedTask(HostTaskManager mgr)
        {
            // Initialize progress as fully met (prevents guard clause failures)
            if (mgr.task != null && mgr.task.objectives != null)
            {
                mgr.currentProgress = new List<int>();
                foreach (var obj in mgr.task.objectives)
                {
                    mgr.currentProgress.Add(obj.requiredAmount);
                }
            }

            // Fire the full status chain so all per-status events fire in order
            mgr.UpdateStatus(TaskStatus.Active);
            mgr.UpdateStatus(TaskStatus.ReadyToComplete);
            mgr.UpdateStatus(TaskStatus.Completed);

            Debug.Log($"LevelLoader: Restored '{mgr.task.taskId}' as Completed.");
        }

        /// <summary>
        /// Called when a task is completed during gameplay.
        /// Saves immediately to disk.
        /// </summary>
        private void OnTaskCompleted(string taskId)
        {
            var level = playerData.GetOrCreateLevel(sceneId);

            if (!level.completedTaskIds.Contains(taskId))
            {
                level.completedTaskIds.Add(taskId);
                SaveManager.Save(playerData);

                Debug.Log($"LevelLoader: Task '{taskId}' saved as completed.");

                if (AreAllTasksCompleted())
                {
                    Debug.Log($"LevelLoader: All tasks in '{sceneId}' are now COMPLETED. Ready for level completion.");
                }
            }
        }

        /// <summary>
        /// Returns true if every Giver/Both task in this scene is Completed.
        /// </summary>
        public bool AreAllTasksCompleted()
        {
            foreach (var mgr in spawnedGivers)
            {
                if (mgr != null && mgr.status != TaskStatus.Completed)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Call this AFTER the outro cutscene.
        /// Marks the level as complete and saves, but ONLY if all tasks are done.
        /// </summary>
        public void CompleteLevel()
        {
            if (!AreAllTasksCompleted())
            {
                Debug.LogWarning($"LevelLoader: Cannot complete level '{sceneId}'. Not all tasks are finished.");
                return;
            }

            var level = playerData.GetOrCreateLevel(sceneId);
            level.isCompleted = true;
            SaveManager.Save(playerData);

            Debug.Log($"LevelLoader: Level '{sceneId}' marked as COMPLETE.");
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
