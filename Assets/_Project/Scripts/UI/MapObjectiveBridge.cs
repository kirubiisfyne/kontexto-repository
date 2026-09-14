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
            yield return new WaitForEndOfFrame();
            DetectInitialPlayerRoom();
            RefreshActiveObjective();
        }

        private void OnEnable()
        {
            MapLocationTrigger.OnPlayerEnteredRoom += HandlePlayerEnteredRoom;
            MapLocationTrigger.OnPlayerExitedRoom += HandlePlayerExitedRoom;
            HostTaskManager.OnTaskStartedGlobal += HandleTaskEvent;
            HostTaskManager.OnProgressReportedGlobal += HandleProgressEvent;

            if (mapPage == null)
            {
                mapPage = FindFirstObjectByType<MapPageController>(FindObjectsInactive.Include);
            }

            if (mapPage != null)
            {
                mapPage.OnMapOpened += HandleMapOpened;
            }
        }

        private void OnDisable()
        {
            MapLocationTrigger.OnPlayerEnteredRoom -= HandlePlayerEnteredRoom;
            MapLocationTrigger.OnPlayerExitedRoom -= HandlePlayerExitedRoom;
            HostTaskManager.OnTaskStartedGlobal -= HandleTaskEvent;
            HostTaskManager.OnProgressReportedGlobal -= HandleProgressEvent;

            if (mapPage != null)
            {
                mapPage.OnMapOpened -= HandleMapOpened;
            }
        }

        private void HandleMapOpened()
        {
            DetectInitialPlayerRoom();
            RefreshActiveObjective();
        }

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

        private void HandleTaskEvent(string uniqueId, string displayName, float maxProgress)
        {
            RefreshActiveObjective();
        }

        private void HandleProgressEvent(string uniqueId, string displayName, float currentProgress, float maxProgress)
        {
            RefreshActiveObjective();
        }

        /// <summary>
        /// Finds the currently active task and points the pin to its active objective's room.
        /// </summary>
        public void RefreshActiveObjective()
        {
            if (mapPage == null) return;

            var allManagers = FindObjectsByType<HostTaskManager>(FindObjectsSortMode.None);
            foreach (var mgr in allManagers)
            {
                if (mgr == null || mgr.task == null || mgr.task.requirements == null) continue;

                if (mgr.status == TaskStatus.Active || mgr.status == TaskStatus.ReadyToComplete)
                {
                    var objectives = mgr.task.requirements.objectives;
                    if (objectives != null && objectives.Count > 0)
                    {
                        // 1. Find the first uncompleted objective
                        for (int i = 0; i < objectives.Count; i++)
                        {
                            int progress = (mgr.currentProgress != null && i < mgr.currentProgress.Count) ? mgr.currentProgress[i] : 0;
                            if (progress < objectives[i].requiredAmount)
                            {
                                // Point to this specific objective's room!
                                if (!string.IsNullOrEmpty(objectives[i].targetRoomId))
                                {
                                    mapPage.SetObjectiveLocation(objectives[i].targetRoomId);
                                    return;
                                }
                                break;
                            }
                        }
                    }

                    // 2. Fallback to task-level targetRoomId (if objective-level is left empty)
                    if (!string.IsNullOrEmpty(mgr.task.targetRoomId))
                    {
                        mapPage.SetObjectiveLocation(mgr.task.targetRoomId);
                        return;
                    }
                }
            }

            mapPage.ClearObjectiveLocation();
        }
    }
}
