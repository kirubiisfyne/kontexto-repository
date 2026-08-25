using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Central catalog and ordering sequence for all playable days/level sequences in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "New LevelDatabase", menuName = "Levels/Level Database")]
    public class LevelDatabase : ScriptableObject
    {
        [Tooltip("Ordered list of all playable day sequences in progression order.")]
        public List<LevelSequenceData> levels = new List<LevelSequenceData>();

        public int Count => levels != null ? levels.Count : 0;

        public LevelSequenceData GetLevelByIndex(int index)
        {
            if (levels == null || index < 0 || index >= levels.Count) return null;
            return levels[index];
        }

        public LevelSequenceData GetLevelById(string sceneId)
        {
            if (levels == null || string.IsNullOrEmpty(sceneId)) return null;
            return levels.Find(l => l != null && l.levelData != null && l.levelData.sceneId == sceneId);
        }

        public int GetLevelIndex(LevelSequenceData sequence)
        {
            if (levels == null || sequence == null) return -1;
            return levels.IndexOf(sequence);
        }

        public int GetLevelIndex(LevelData data)
        {
            if (levels == null || data == null) return -1;
            return levels.FindIndex(l => l != null && l.levelData == data);
        }

        public LevelSequenceData GetSequenceForLevelData(LevelData data)
        {
            if (levels == null || data == null) return null;
            return levels.Find(l => l != null && l.levelData == data);
        }

        public LevelSequenceData GetNextLevel(LevelSequenceData current)
        {
            int currentIndex = GetLevelIndex(current);
            if (currentIndex >= 0 && currentIndex + 1 < levels.Count)
            {
                return levels[currentIndex + 1];
            }
            return null;
        }

        public LevelSequenceData GetNextLevel(LevelData current)
        {
            int currentIndex = GetLevelIndex(current);
            if (currentIndex >= 0 && currentIndex + 1 < levels.Count)
            {
                return levels[currentIndex + 1];
            }
            return null;
        }

        public LevelSequenceData GetFirstIncompleteLevel(PlayerData playerData)
        {
            if (levels == null || levels.Count == 0) return null;
            if (playerData == null) return levels[0];

            foreach (var sequence in levels)
            {
                if (sequence == null || sequence.levelData == null) continue;
                if (!playerData.IsLevelCompleted(sequence.levelData.sceneId))
                {
                    return sequence;
                }
            }

            return levels[levels.Count - 1];
        }
    }
}

