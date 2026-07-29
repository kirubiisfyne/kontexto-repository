using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.RoomSystem
{
    /// <summary>
    /// Attached to a parent room GameObject. Manages room prop visibility and door states
    /// based on whether the room is active in the current LevelData.
    /// Supports rooms with multiple doors or open rooms with no doors at all.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("Room Identity")]
        [Tooltip("Unique identifier for this room (must match entries in LevelData.activeRoomIds).")]
        public string roomId;

        [Header("Props & Optimization")]
        [Tooltip("Parent GameObject containing all props/decorations in this room to enable/disable.")]
        public GameObject propsContainer;

        [Header("Doors Configuration")]
        [Tooltip("List of doors belonging to this room. Leave empty for rooms with no doors (e.g. Multipurpose Hall).")]
        public List<DoorData> doors = new List<DoorData>();

        private void Awake()
        {
            // Cache default closed rotations on startup (if doors exist)
            if (doors != null)
            {
                foreach (var door in doors)
                {
                    if (door != null) door.CacheClosedState();
                }
            }
        }

        /// <summary>
        /// Activates or deactivates room props and sets all door rotations.
        /// </summary>
        /// <param name="isActive">True if the room is assigned/active in LevelData.</param>
        public void SetRoomActive(bool isActive)
        {
            // 1. Enable/disable props container for performance optimization
            if (propsContainer != null)
            {
                propsContainer.SetActive(isActive);
            }

            // 2. Open or close doors (safely skipped if no doors are assigned)
            if (doors != null)
            {
                foreach (var door in doors)
                {
                    if (door != null) door.SetState(isActive);
                }
            }
        }
    }

    [System.Serializable]
    public class DoorData
    {
        [Tooltip("The door Transform to rotate.")]
        public Transform doorTransform;

        [Tooltip("Target local Euler rotation when the door is OPEN (e.g., X: 0, Y: 90, Z: 0).")]
        public Vector3 openEulerAngles = new Vector3(0f, 90f, 0f);

        // Internal cache of default closed rotation
        private Quaternion closedRotation;
        private bool isInitialized;

        public void CacheClosedState()
        {
            if (doorTransform != null && !isInitialized)
            {
                closedRotation = doorTransform.localRotation;
                isInitialized = true;
            }
        }

        public void SetState(bool isOpen)
        {
            if (doorTransform == null) return;
            if (!isInitialized) CacheClosedState();

            if (isOpen)
            {
                doorTransform.localEulerAngles = openEulerAngles;
            }
            else
            {
                doorTransform.localRotation = closedRotation;
            }
        }
    }
}
