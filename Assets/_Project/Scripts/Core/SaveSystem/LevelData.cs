using System.Collections.Generic;
using Master.Scripts.TextEditorSystem;
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

        [Header("Filler NPCs")]
        [Tooltip("List of filler/ambient NPC prefabs to instantiate for this level.")]
        public List<NPCSpawnEntry> fillerNpcEntries = new List<NPCSpawnEntry>();

        [Header("Active Rooms")]
        [Tooltip("List of room IDs that should be enabled and accessible in this level.")]
        public List<string> activeRoomIds = new List<string>();

        [Header("Tutorial (Optional)")]
        [Tooltip("The editor lesson taught on this day, for example fonts and sizes. Leave it empty on days " +
                 "that teach nothing new; the editor then falls back to its own default sequence.")]
        public EditorTutorialSequence editorTutorial;

        [Header("Audio Configuration (Optional)")]
        [Tooltip("Optional background music track name to play during this level (e.g. 'GoldenGleam', 'GentleBreeze'). Defaults to 'GoldenGleam'.")]
        public string bgmTrackName;
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

    [System.Serializable]
    public class NPCSpawnEntry
    {
        [Tooltip("The NPC prefab to instantiate.")]
        public GameObject prefab;

        [Tooltip("World position to place the NPC.")]
        public Vector3 spawnPosition;

        [Tooltip("World rotation (euler angles) to apply to the NPC.")]
        public Vector3 spawnRotation;

        [Tooltip("If true, ignores the above position/rotation and uses the prefab's saved transform.")]
        public bool usePrefabTransform;
    }
}
