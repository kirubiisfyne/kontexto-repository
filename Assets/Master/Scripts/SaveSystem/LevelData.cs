using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.SaveSystem
{
    /// <summary>
    /// Defines what to spawn when entering a gameplay scene.
    /// Each entry is a "task group" prefab (parent with Giver, Closer, KeyItems as children).
    /// </summary>
    [CreateAssetMenu(fileName = "New LevelData", menuName = "Levels/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Scene Identity")]
        [Tooltip("Must match the scene name exactly (e.g., 'scn_day1').")]
        public string sceneId;

        [Header("Document Data")]
        [Tooltip("The document data JSON required for this level (if any).")]
        public TextAsset documentData;

        [Header("Task Prefabs")]
        public List<TaskSpawnEntry> taskEntries = new List<TaskSpawnEntry>();
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
