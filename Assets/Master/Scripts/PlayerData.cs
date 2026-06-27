using System.Collections.Generic;

namespace Master.Scripts
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
        /// Updated when a level is completed (set to the NEXT level).
        /// </summary>
        public string currentScene;

        /// <summary>
        /// Per-level completion records.
        /// </summary>
        public List<LevelProgress> levels = new List<LevelProgress>();

        // ── Helpers (not serialized) ──

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
    }
}