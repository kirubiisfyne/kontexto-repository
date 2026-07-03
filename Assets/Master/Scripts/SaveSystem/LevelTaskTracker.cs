using System;
using System.Collections.Generic;
using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Handles spawning and tracking tasks for a specific level.
    /// Extracted from LevelLoader to separate save/load concerns from task management.
    /// </summary>
    public class LevelTaskTracker : MonoBehaviour
    {
        [Header("Runtime (Read-Only)")]
        [Tooltip("All Giver/Both HostTaskManagers spawned by this loader.")]
        [SerializeField] private List<HostTaskManager> spawnedGivers = new List<HostTaskManager>();

        private string sceneId;
        private PlayerData playerData;
        private Action saveGameCallback;

        /// <summary>
        /// Initializes the tracker with required data and spawns the tasks.
        /// </summary>
        public void Initialize(LevelData data, PlayerData pData, Action saveCallback)
        {
            if (data == null) return;

            sceneId = data.sceneId;
            playerData = pData;
            saveGameCallback = saveCallback;

            SpawnAndRestoreTasks(data);
        }

        private void SpawnAndRestoreTasks(LevelData levelData)
        {
            foreach (var entry in levelData.taskEntries)
            {
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"LevelTaskTracker: A task entry in '{levelData.name}' has a null prefab. Skipping.");
                    continue;
                }

                GameObject instance;

                if (entry.usePrefabTransform)
                {
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

                var managers = instance.GetComponentsInChildren<HostTaskManager>();
                var keyItems = instance.GetComponentsInChildren<KeyItemInstance>();

                foreach (var mgr in managers)
                {
                    if (mgr.hostType == HostType.Giver || mgr.hostType == HostType.Both)
                    {
                        spawnedGivers.Add(mgr);

                        if (mgr.task == null)
                        {
                            Debug.LogWarning($"LevelTaskTracker: HostTaskManager on '{mgr.gameObject.name}' has no TaskData assigned.");
                            continue;
                        }

                        if (string.IsNullOrEmpty(mgr.task.taskId))
                        {
                            Debug.LogWarning($"LevelTaskTracker: TaskData '{mgr.task.taskName}' has an empty taskId. Save/load will not track this task.");
                            continue;
                        }

                        if (playerData.IsTaskCompleted(sceneId, mgr.task.taskId))
                        {
                            RestoreCompletedTask(mgr);

                            foreach (var item in keyItems)
                            {
                                if (item.targetGiver == mgr)
                                {
                                    item.Retire();
                                }
                            }
                        }
                        else
                        {
                            var capturedId = mgr.task.taskId;
                            mgr.events.onCompleted.AddListener(() => OnTaskCompleted(capturedId));
                        }
                    }
                }
            }

            Debug.Log($"LevelTaskTracker: Spawned {spawnedGivers.Count} task(s) for '{sceneId}'.");

            if (spawnedGivers.Count > 0 && AreAllTasksCompleted())
            {
                Debug.Log($"LevelTaskTracker: All tasks in '{sceneId}' are already COMPLETED (restored from save). Ready for level completion.");
            }
        }

        private void RestoreCompletedTask(HostTaskManager mgr)
        {
            if (mgr.task != null && mgr.task.objectives != null)
            {
                mgr.currentProgress = new List<int>();
                foreach (var obj in mgr.task.objectives)
                {
                    mgr.currentProgress.Add(obj.requiredAmount);
                }
            }

            mgr.UpdateStatus(TaskStatus.Active);
            mgr.UpdateStatus(TaskStatus.ReadyToComplete);
            mgr.UpdateStatus(TaskStatus.Completed);

            Debug.Log($"LevelTaskTracker: Restored '{mgr.task.taskId}' as Completed.");
        }

        private void OnTaskCompleted(string taskId)
        {
            var level = playerData.GetOrCreateLevel(sceneId);

            if (!level.completedTaskIds.Contains(taskId))
            {
                level.completedTaskIds.Add(taskId);
                saveGameCallback?.Invoke();

                Debug.Log($"LevelTaskTracker: Task '{taskId}' saved as completed.");

                if (AreAllTasksCompleted())
                {
                    Debug.Log($"LevelTaskTracker: All tasks in '{sceneId}' are now COMPLETED. Ready for level completion.");
                }
            }
        }

        public bool AreAllTasksCompleted()
        {
            foreach (var mgr in spawnedGivers)
            {
                if (mgr != null && mgr.status != TaskStatus.Completed)
                    return false;
            }
            return true;
        }

        public void CompleteLevel()
        {
            if (!AreAllTasksCompleted())
            {
                Debug.LogWarning($"LevelTaskTracker: Cannot complete level '{sceneId}'. Not all tasks are finished.");
                return;
            }

            var level = playerData.GetOrCreateLevel(sceneId);
            level.isCompleted = true;
            saveGameCallback?.Invoke();

            Debug.Log($"LevelTaskTracker: Level '{sceneId}' marked as COMPLETE.");
        }
    }
}
