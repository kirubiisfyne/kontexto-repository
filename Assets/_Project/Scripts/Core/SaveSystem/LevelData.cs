using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Defines level configuration for entering a gameplay scene.
    /// Each entry is a "task group" prefab (parent with Giver, Closer, KeyItems as children).
    /// </summary>
    [CreateAssetMenu(fileName = "New LevelData", menuName = "Levels/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Scene Identity")]
        [Tooltip("Must match the scene name exactly (e.g., 'scn_day1').")]
        public string sceneId;

        [Header("Player Initial Spawn Anchor")]
        [Tooltip("Assign the spawn anchor prefab. The player will be moved to this prefab's position and rotation on level start.")]
        public GameObject playerSpawnAnchorPrefab;

        [Header("Task Prefabs")]
        public List<TaskSpawnEntry> taskEntries = new List<TaskSpawnEntry>();

        [Header("Active Rooms")]
        [Tooltip("List of room IDs that should be enabled and accessible in this level.")]
        public List<string> activeRoomIds = new List<string>();
    }

    [System.Serializable]
    public class TaskSpawnEntry
    {
        [Tooltip("The parent prefab containing the full task group (Giver, Closer, KeyItems).")]
        public GameObject prefab;

        [Tooltip("World position to place the prefab.")]
        public Vector3 spawnPosition;

        [Tooltip("World rotation (euler angles) to apply to the prefab.")]
        public Vector3 spawnRotation;

        [Tooltip("If true, ignores the above position/rotation and uses the prefab's saved transform.")]
        public bool usePrefabTransform;
    }
}
