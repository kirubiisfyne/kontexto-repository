using UnityEngine;

namespace Master.Scripts.RoomSystem
{
    /// <summary>
    /// Attached to a trigger collider enclosing a room or zone.
    /// Broadcasts when the player enters or exits this area.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MapLocationTrigger : MonoBehaviour
    {
        [Tooltip("Unique roomId matching RoomController and MapPageController (e.g. 'canteen', 'multipurpose_hall', 'admin_office').")]
        public string roomId;

        public static event System.Action<string> OnPlayerEnteredRoom;
        public static event System.Action<string> OnPlayerExitedRoom;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPlayer(other))
            {
                OnPlayerEnteredRoom?.Invoke(roomId);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
            {
                OnPlayerExitedRoom?.Invoke(roomId);
            }
        }

        private bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponentInParent<PlayerController>() != null;
        }

        /// <summary>
        /// Checks if a 3D point is inside this trigger collider volume.
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            var col = GetComponent<Collider>();
            if (col == null) return false;
            // ClosestPoint returns the exact point if worldPoint is inside the volume
            return (col.ClosestPoint(worldPoint) - worldPoint).sqrMagnitude < 0.001f;
        }
    }
}
