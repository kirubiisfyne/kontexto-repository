using System;
using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.UI
{
    [Serializable]
    public struct RoomMapAnchor
    {
        [Tooltip("Must match the roomId used in RoomController (e.g. 'canteen', 'upper_library', 'admin_office').")]
        public string roomId;

        [Tooltip("The RectTransform positioned over this room on the map notebook page.")]
        public RectTransform pinAnchor;
    }

    /// <summary>
    /// Manages the visual pins on the Map notebook page (player location and active objective).
    /// </summary>
    public class MapPageController : MonoBehaviour
    {
        [Header("Pins")]
        [Tooltip("UI pin indicating the player's current/last known location.")]
        [SerializeField] private RectTransform playerPin;

        [Tooltip("UI pin indicating the active objective destination.")]
        [SerializeField] private RectTransform objectivePin;

        [Header("Opacity Settings")]
        [Tooltip("Opacity of the player pin when outside of any room (hallways, courtyard).")]
        [Range(0f, 1f)]
        [SerializeField] private float outsideRoomAlpha = 0.35f;

        [Header("Room Anchors")]
        [SerializeField] private List<RoomMapAnchor> roomAnchors = new List<RoomMapAnchor>();

        private Dictionary<string, RectTransform> anchorLookup = new Dictionary<string, RectTransform>();
        private CanvasGroup playerPinCanvasGroup;
        private string lastKnownRoomId;
        private bool isInsideRoom = false;
        private string currentObjectiveRoomId;

        private void Awake()
        {
            EnsureCanvasGroup();
            BuildAnchorLookup();
            UpdatePlayerPinVisual();
            UpdateObjectivePinVisual();
        }

        private void OnEnable()
        {
            UpdatePlayerPinVisual();
            UpdateObjectivePinVisual();
        }

        private void EnsureCanvasGroup()
        {
            if (playerPin != null && playerPinCanvasGroup == null)
            {
                playerPinCanvasGroup = playerPin.GetComponent<CanvasGroup>();
                if (playerPinCanvasGroup == null)
                {
                    playerPinCanvasGroup = playerPin.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void BuildAnchorLookup()
        {
            anchorLookup.Clear();
            foreach (var anchor in roomAnchors)
            {
                if (!string.IsNullOrEmpty(anchor.roomId) && anchor.pinAnchor != null)
                {
                    anchorLookup[anchor.roomId] = anchor.pinAnchor;
                }
            }
        }

        /// <summary>
        /// Player entered a room: snap pin to the room anchor and set full opacity.
        /// </summary>
        public void SetPlayerLocation(string roomId)
        {
            lastKnownRoomId = roomId;
            isInsideRoom = true;
            UpdatePlayerPinVisual();
        }

        /// <summary>
        /// Player exited a room: keep pin at last known room but lower opacity.
        /// </summary>
        public void SetPlayerOutsideRoom()
        {
            isInsideRoom = false;
            UpdatePlayerPinVisual();
        }

        public void SetObjectiveLocation(string roomId)
        {
            currentObjectiveRoomId = roomId;
            UpdateObjectivePinVisual();
        }

        public void ClearObjectiveLocation()
        {
            currentObjectiveRoomId = null;
            if (objectivePin != null) objectivePin.gameObject.SetActive(false);
        }

        private void UpdatePlayerPinVisual()
        {
            if (playerPin == null) return;
            EnsureCanvasGroup();

            if (anchorLookup.Count == 0) BuildAnchorLookup();

            if (!string.IsNullOrEmpty(lastKnownRoomId) && anchorLookup.TryGetValue(lastKnownRoomId, out var anchor))
            {
                playerPin.gameObject.SetActive(true);

                // Use localPosition to avoid 0-scale division when notebook page is collapsed
                if (playerPin.parent == anchor.parent)
                {
                    playerPin.localPosition = anchor.localPosition;
                }
                else
                {
                    playerPin.position = anchor.position;
                }

                if (playerPinCanvasGroup != null)
                {
                    playerPinCanvasGroup.alpha = isInsideRoom ? 1.0f : outsideRoomAlpha;
                }
            }
            else
            {
                playerPin.gameObject.SetActive(false);
            }
        }

        private void UpdateObjectivePinVisual()
        {
            if (objectivePin == null) return;

            if (anchorLookup.Count == 0) BuildAnchorLookup();

            if (!string.IsNullOrEmpty(currentObjectiveRoomId) && anchorLookup.TryGetValue(currentObjectiveRoomId, out var anchor))
            {
                objectivePin.gameObject.SetActive(true);

                // Use localPosition to avoid 0-scale division when notebook page is collapsed
                if (objectivePin.parent == anchor.parent)
                {
                    objectivePin.localPosition = anchor.localPosition;
                }
                else
                {
                    objectivePin.position = anchor.position;
                }
            }
            else
            {
                objectivePin.gameObject.SetActive(false);
            }
        }
    }
}
