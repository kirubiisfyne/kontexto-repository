using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Tracks per-level progress: which tasks were completed and whether the level itself is done.
    /// </summary>
    [System.Serializable]
    public class LevelProgress
    {
        public string sceneId;
        public bool isCompleted;
        public List<string> completedTaskIds = new List<string>();
    }

    /// <summary>
    /// Root save data. Serialized to/from JSON on disk.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        /// <summary>
        /// The scene the player should load into on "Continue".
        /// Updated on every save so we know which scene a saved position belongs to.
        /// </summary>
        public string currentScene;

        /// <summary>
        /// Player world position (x, y, z). Stored as float[] for clean JSON output.
        /// </summary>
        public float[] playerPosition = new float[3];

        /// <summary>
        /// Player rotation as euler angles (x, y, z). Stored as float[] for clean JSON output.
        /// </summary>
        public float[] playerRotation = new float[3];

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
        /// Reads the saved position and rotation back as Vector3s.
        /// </summary>
        public (Vector3 position, Vector3 rotation) GetPlayerTransform()
        {
            return (
                new Vector3(playerPosition[0], playerPosition[1], playerPosition[2]),
                new Vector3(playerRotation[0], playerRotation[1], playerRotation[2])
            );
        }

        /// <summary>
        /// Returns true if a valid player position has been saved.
        /// </summary>
        public bool HasSavedPosition() => playerPosition != null && playerPosition.Length == 3;

        // ── Level Helpers (not serialized) ──

        /// <summary>
        /// Finds or creates the LevelProgress entry for a given scene.
        /// </summary>
        public LevelProgress GetOrCreateLevel(string sceneId)
        {
            foreach (var lp in levels)
            {
                if (lp.sceneId == sceneId) return lp;
            }

            var newLevel = new LevelProgress { sceneId = sceneId };
            levels.Add(newLevel);
            return newLevel;
        }

        /// <summary>
        /// Returns true if the given taskId is marked completed for the given scene.
        /// </summary>
        public bool IsTaskCompleted(string sceneId, string taskId)
        {
            foreach (var lp in levels)
            {
                if (lp.sceneId == sceneId)
                    return lp.completedTaskIds.Contains(taskId);
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given taskId is marked completed in any scene.
        /// Useful for checking global prerequisites.
        /// </summary>
        public bool IsTaskCompletedGlobally(string taskId)
        {
            foreach (var lp in levels)
            {
                if (lp.completedTaskIds.Contains(taskId))
                    return true;
            }
            return false;
        }
    }
}