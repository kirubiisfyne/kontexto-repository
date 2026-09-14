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

        [Header("Default Outdoor Spawn")]
        [Tooltip("Optional preset anchor where the player pin appears when spawned in the open (e.g., Campus Entrance or Courtyard).")]
        [SerializeField] private RectTransform defaultOutdoorAnchor;

        [Header("Room Anchors")]
        [SerializeField] private List<RoomMapAnchor> roomAnchors = new List<RoomMapAnchor>();

        private Dictionary<string, RectTransform> anchorLookup = new Dictionary<string, RectTransform>();
        private CanvasGroup playerPinCanvasGroup;
        private string lastKnownRoomId;
        private bool isInsideRoom = false;
        private string currentObjectiveRoomId;
        private bool wasVisibleLastFrame = false;

        public event Action OnMapOpened;

        private void Awake()
        {
            EnsureCanvasGroup();
            BuildAnchorLookup();
            UpdatePlayerPinVisual();
            UpdateObjectivePinVisual();
        }

        private void Update()
        {
            // Detect when MapPage is scaled up / opened by the notebook animator
            bool isCurrentlyVisible = transform.localScale.x > 0.1f;
            if (isCurrentlyVisible && !wasVisibleLastFrame)
            {
                // Map was just opened: notify bridge to re-check player position & objectives
                OnMapOpened?.Invoke();
                UpdatePlayerPinVisual();
                UpdateObjectivePinVisual();
            }
            wasVisibleLastFrame = isCurrentlyVisible;
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

        public void SetPlayerLocation(string roomId)
        {
            lastKnownRoomId = roomId;
            isInsideRoom = true;
            UpdatePlayerPinVisual();
        }

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

        private bool TryGetAnchor(string roomId, out RectTransform anchor)
        {
            anchor = null;
            if (string.IsNullOrEmpty(roomId)) return false;

            if (anchorLookup.Count == 0) BuildAnchorLookup();

            if (anchorLookup.TryGetValue(roomId, out anchor)) return true;

            foreach (var kvp in anchorLookup)
            {
                if (string.Equals(kvp.Key, roomId, StringComparison.OrdinalIgnoreCase))
                {
                    anchor = kvp.Value;
                    return true;
                }

                if (string.Equals(kvp.Key.TrimEnd('s'), roomId.TrimEnd('s'), StringComparison.OrdinalIgnoreCase))
                {
                    anchor = kvp.Value;
                    return true;
                }
            }

            return false;
        }

        private Vector3 GetLocalPositionInMapPage(RectTransform target)
        {
            Vector3 localPos = target.localPosition;
            Transform curr = target.parent;
            while (curr != null && curr != transform)
            {
                localPos += curr.localPosition;
                curr = curr.parent;
            }
            return localPos;
        }

        private void UpdatePlayerPinVisual()
        {
            if (playerPin == null) return;
            EnsureCanvasGroup();

            // 1. Player is in or has visited a room
            if (!string.IsNullOrEmpty(lastKnownRoomId) && TryGetAnchor(lastKnownRoomId, out var anchor))
            {
                playerPin.gameObject.SetActive(true);
                playerPin.localPosition = GetLocalPositionInMapPage(anchor);

                if (playerPinCanvasGroup != null)
                {
                    playerPinCanvasGroup.alpha = isInsideRoom ? 1.0f : outsideRoomAlpha;
                }
            }
            // 2. Player spawned in the open with no prior room, but a preset outdoor anchor exists
            else if (defaultOutdoorAnchor != null)
            {
                playerPin.gameObject.SetActive(true);
                playerPin.localPosition = GetLocalPositionInMapPage(defaultOutdoorAnchor);

                if (playerPinCanvasGroup != null)
                {
                    playerPinCanvasGroup.alpha = outsideRoomAlpha; // Dimmed to signal they are outdoors
                }
            }
            // 3. Fallback: keep hidden
            else
            {
                playerPin.gameObject.SetActive(false);
            }
        }

        private void UpdateObjectivePinVisual()
        {
            if (objectivePin == null) return;

            if (!string.IsNullOrEmpty(currentObjectiveRoomId) && TryGetAnchor(currentObjectiveRoomId, out var anchor))
            {
                objectivePin.gameObject.SetActive(true);
                objectivePin.localPosition = GetLocalPositionInMapPage(anchor);
            }
            else
            {
                objectivePin.gameObject.SetActive(false);
            }
        }
    }
}
