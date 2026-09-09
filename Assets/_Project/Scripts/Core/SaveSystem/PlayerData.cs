using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Tracks per-level progress: which tasks were completed, intro status, and whether the level itself is done.
    /// </summary>
    [System.Serializable]
    public class LevelProgress
    {
        public string sceneId;
        public string sequenceName;
        public bool isCompleted;
        public bool introWatched;
        public List<string> completedTaskIds = new List<string>();
        public List<string> activeTaskIds = new List<string>();
    }

    /// <summary>
    /// Root save data. Serialized to/from JSON on disk.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        /// <summary>
        /// The sequence asset name (e.g. "seq_Day0", "seq_Day1") the player should resume.
        /// </summary>
        public string currentSequence;

        /// <summary>
        /// The scene or level identifier (e.g. "scn_day0", "scn_Day1") the player should load into.
        /// Updated on every save so we know which scene a saved position belongs to.
        /// </summary>
        public string currentScene;

        /// <summary>
        /// Player world position (x, y, z). Stored as float[] for clean JSON output.
        /// Defaults to null to distinguish absence of saved position from Vector3.zero.
        /// </summary>
        public float[] playerPosition = null;

        /// <summary>
        /// Player rotation as euler angles (x, y, z). Stored as float[] for clean JSON output.
        /// </summary>
        public float[] playerRotation = null;

        /// <summary>
        /// Per-level completion records.
        /// </summary>
        public List<LevelProgress> levels = new List<LevelProgress>();

        // ── Transform Helpers (not serialized) ──

        /// <summary>
        /// Stores the player's current position and rotation into the save data.
        /// </summary>
        public void SetPlayerTransform(Vector3 position, Vector3 eulerAngles)
        {
            playerPosition = new float[] { position.x, position.y, position.z };
            playerRotation = new float[] { eulerAngles.x, eulerAngles.y, eulerAngles.z };
        }

        /// <summary>
        /// Clears the saved player transform so spawn anchor placement is used on next load.
        /// </summary>
        public void ClearPlayerTransform()
        {
            playerPosition = null;
            playerRotation = null;
        }

        /// <summary>
        /// Reads the saved position and rotation back as Vector3s.
        /// </summary>
        public (Vector3 position, Vector3 rotation) GetPlayerTransform()
        {
            if (playerPosition == null || playerPosition.Length < 3) return (Vector3.zero, Vector3.zero);
            Vector3 rot = (playerRotation != null && playerRotation.Length == 3)
                ? new Vector3(playerRotation[0], playerRotation[1], playerRotation[2])
                : Vector3.zero;
            return (
                new Vector3(playerPosition[0], playerPosition[1], playerPosition[2]),
                rot
            );
        }

        /// <summary>
        /// Returns true if a valid player position has been saved.
        /// </summary>
        public bool HasSavedPosition() => playerPosition != null && playerPosition.Length == 3;

        // ── Level Helpers (not serialized) ──

        /// <summary>
        /// Finds a LevelProgress entry matching by sceneId or sequenceName (case-insensitive).
        /// </summary>
        public LevelProgress GetLevel(string identifier)
        {
            if (levels == null || string.IsNullOrEmpty(identifier)) return null;
            return levels.Find(lp =>
                string.Equals(lp.sceneId, identifier, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(lp.sequenceName, identifier, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Finds or creates the LevelProgress entry for a given scene / sequence.
        /// </summary>
        public LevelProgress GetOrCreateLevel(string sceneId, string sequenceName = "")
        {
            if (levels == null) levels = new List<LevelProgress>();

            var lp = GetLevel(sceneId);
            if (lp != null)
            {
                if (!string.IsNullOrEmpty(sequenceName) && string.IsNullOrEmpty(lp.sequenceName))
                {
                    lp.sequenceName = sequenceName;
                }
                return lp;
            }

            var newLevel = new LevelProgress { sceneId = sceneId, sequenceName = sequenceName };
            levels.Add(newLevel);
            return newLevel;
        }

        /// <summary>
        /// Checks if the intro cutscene for the specified level or sequence was already watched.
        /// </summary>
        public bool IsIntroWatched(string sceneId)
        {
            var lp = GetLevel(sceneId);
            return lp != null && lp.introWatched;
        }

        /// <summary>
        /// Sets whether the intro cutscene for the specified level or sequence has been watched.
        /// </summary>
        public void SetIntroWatched(string sceneId, bool watched, string sequenceName = "")
        {
            var lp = GetOrCreateLevel(sceneId, sequenceName);
            lp.introWatched = watched;
        }

        /// <summary>
        /// Returns true if the given taskId is marked completed for the given scene.
        /// </summary>
        public bool IsTaskCompleted(string sceneId, string taskId)
        {
            var lp = GetLevel(sceneId);
            return lp != null && lp.completedTaskIds.Contains(taskId);
        }

        /// <summary>
        /// Returns true if the given taskId is marked as currently active for the given scene.
        /// </summary>
        public bool IsTaskActive(string sceneId, string taskId)
        {
            var lp = GetLevel(sceneId);
            return lp != null && lp.activeTaskIds.Contains(taskId);
        }

        /// <summary>
        /// Sets a task's active status.
        /// </summary>
        public void SetTaskActive(string sceneId, string taskId, bool isActive)
        {
            var level = GetOrCreateLevel(sceneId);
            if (isActive && !level.activeTaskIds.Contains(taskId))
            {
                level.activeTaskIds.Add(taskId);
            }
            else if (!isActive && level.activeTaskIds.Contains(taskId))
            {
                level.activeTaskIds.Remove(taskId);
            }
        }

        /// <summary>
        /// Returns true if the given taskId is marked completed in any scene.
        /// Useful for checking global prerequisites.
        /// </summary>
        public bool IsTaskCompletedGlobally(string taskId)
        {
            if (levels == null) return false;
            foreach (var lp in levels)
            {
                if (lp.completedTaskIds.Contains(taskId))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the level with the given sceneId or sequenceName is marked as completed.
        /// </summary>
        public bool IsLevelCompleted(string sceneId)
        {
            var lp = GetLevel(sceneId);
            return lp != null && lp.isCompleted;
        }
    }
}