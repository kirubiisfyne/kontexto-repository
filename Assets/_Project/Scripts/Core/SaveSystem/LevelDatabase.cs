using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Central catalog and ordering sequence for all playable days/levels in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "New LevelDatabase", menuName = "Levels/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [Tooltip("Ordered list of all playable days/levels in progression order.")]
        public List<LevelData> levels = new List<LevelData>();

        public int Count => levels != null ? levels.Count : 0;

        public LevelData GetLevelByIndex(int index)
        {
            if (levels == null || index < 0 || index >= levels.Count) return null;
            return levels[index];
        }

        public LevelData GetLevelById(string sceneId)
        {
            if (levels == null || string.IsNullOrEmpty(sceneId)) return null;
            return levels.Find(l => l != null && l.sceneId == sceneId);
        }

        public int GetLevelIndex(LevelData data)
        {
            if (levels == null || data == null) return -1;
            return levels.IndexOf(data);
        }

        public LevelData GetNextLevel(LevelData current)
        {
            int currentIndex = GetLevelIndex(current);
            if (currentIndex >= 0 && currentIndex + 1 < levels.Count)
            {
                return levels[currentIndex + 1];
            }
            return null;
        }

        public LevelData GetFirstIncompleteLevel(PlayerData playerData)
        {
            if (levels == null || levels.Count == 0) return null;
            if (playerData == null) return levels[0];

            foreach (var level in levels)
            {
                if (level == null) continue;
                if (!playerData.IsLevelCompleted(level.sceneId))
                {
                    return level;
                }
            }

            return levels[levels.Count - 1];
        }
    }
}
