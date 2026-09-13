using System.Collections;
using Master.Scripts.RoomSystem;
using Master.Scripts.TaskSystem;
using UnityEngine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Connects Player room entry/exit and Task lifecycle events to the MapPageController.
    /// </summary>
    public class MapObjectiveBridge : MonoBehaviour
    {
        [SerializeField] private MapPageController mapPage;

        private void Awake()
        {
            if (mapPage == null)
            {
                mapPage = FindFirstObjectByType<MapPageController>(FindObjectsInactive.Include);
            }
        }

        private IEnumerator Start()
        {
            // Wait for level loader to position player and initialize scene
            yield return new WaitForEndOfFrame();
            DetectInitialPlayerRoom();
            RefreshActiveObjective();
        }

        private void OnEnable()
        {
            MapLocationTrigger.OnPlayerEnteredRoom += HandlePlayerEnteredRoom;
            MapLocationTrigger.OnPlayerExitedRoom += HandlePlayerExitedRoom;
            HostTaskManager.OnTaskStartedGlobal += HandleTaskStarted;
        }

        private void OnDisable()
        {
            MapLocationTrigger.OnPlayerEnteredRoom -= HandlePlayerEnteredRoom;
            MapLocationTrigger.OnPlayerExitedRoom -= HandlePlayerExitedRoom;
            HostTaskManager.OnTaskStartedGlobal -= HandleTaskStarted;
        }

        /// <summary>
        /// Checks on level load which room the player initially spawned inside.
        /// </summary>
        public void DetectInitialPlayerRoom()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null || mapPage == null) return;

            var triggers = FindObjectsByType<MapLocationTrigger>(FindObjectsSortMode.None);
            foreach (var trigger in triggers)
            {
                if (trigger.ContainsPoint(player.transform.position))
                {
                    mapPage.SetPlayerLocation(trigger.roomId);
                    return;
                }
            }

            // Player spawned outside any room trigger
            mapPage.SetPlayerOutsideRoom();
        }

        private void HandlePlayerEnteredRoom(string roomId)
        {
            if (mapPage != null)
            {
                mapPage.SetPlayerLocation(roomId);
            }
        }

        private void HandlePlayerExitedRoom(string roomId)
        {
            if (mapPage != null)
            {
                mapPage.SetPlayerOutsideRoom();
            }
        }

        private void HandleTaskStarted(string uniqueId, string displayName, float maxProgress)
        {
            RefreshActiveObjective();
        }

        public void RefreshActiveObjective()
        {
            if (mapPage == null) return;

            var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
            foreach (var mgr in allManagers)
            {
                if (mgr != null && mgr.status == TaskStatus.Active && mgr.task != null)
                {
                    if (!string.IsNullOrEmpty(mgr.task.targetRoomId))
                    {
                        mapPage.SetObjectiveLocation(mgr.task.targetRoomId);
                        return;
                    }

                    var room = mgr.GetComponentInParent<RoomController>();
                    if (room != null && !string.IsNullOrEmpty(room.roomId))
                    {
                        mapPage.SetObjectiveLocation(room.roomId);
                        return;
                    }
                }
            }

            mapPage.ClearObjectiveLocation();
        }
    }
}
