using System.Collections.Generic;
using UnityEngine;

namespace Kontexto.UI
{
    public class ObjectiveNotificationController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ObjectiveNotificationUI prefab to instantiate.")]
        [SerializeField] private ObjectiveNotificationUI notificationPrefab;
        
        [Tooltip("The Vertical Layout Group container where notifications will spawn.")]
        [SerializeField] private Transform verticalContainer;

        // Tracks which UI belongs to which unique objective ID
        private Dictionary<string, ObjectiveNotificationUI> activeNotifications = new Dictionary<string, ObjectiveNotificationUI>();

        private void OnEnable()
        {
            // Automatically listen to the global task events!
            Master.Scripts.TaskSystem.HostTaskManager.OnTaskStartedGlobal += OnTaskActivated;
            Master.Scripts.TaskSystem.HostTaskManager.OnProgressReportedGlobal += OnTaskProgressed;
        }

        private void OnDisable()
        {
            // Unsubscribe when disabled to prevent memory leaks
            Master.Scripts.TaskSystem.HostTaskManager.OnTaskStartedGlobal -= OnTaskActivated;
            Master.Scripts.TaskSystem.HostTaskManager.OnProgressReportedGlobal -= OnTaskProgressed;
        }

        /// <summary>
        /// Automatically triggered when an objective starts. Forces current progress to 0.
        /// </summary>
        private void OnTaskActivated(string uniqueId, string taskName, float maxProgress)
        {
            OnTaskProgressed(uniqueId, taskName, 0f, maxProgress);
        }

        /// <summary>
        /// Automatically triggered when an objective makes progress.
        /// </summary>
        private void OnTaskProgressed(string uniqueId, string taskName, float currentProgress, float maxProgress)
        {
            if (notificationPrefab == null || verticalContainer == null) return;

            ObjectiveNotificationUI uiInstance;

            // 1. Check if we already spawned a UI for this specific objective
            if (activeNotifications.ContainsKey(uniqueId))
            {
                uiInstance = activeNotifications[uniqueId];
            }
            else
            {
                // 2. Instantiate a new one and save it in the dictionary
                uiInstance = Instantiate(notificationPrefab, verticalContainer);
                activeNotifications.Add(uniqueId, uiInstance);
            }

            // 3. Update and show the UI
            uiInstance.ShowNotification(taskName, currentProgress, maxProgress);

            // 4. Cleanup tracking when the objective is completely finished
            if (currentProgress >= maxProgress)
            {
                // We remove it from the dictionary and tell the UI to gracefully hide and destroy itself.
                uiInstance.CompleteAndDestroy();
                activeNotifications.Remove(uniqueId);
            }
        }
    }
}
