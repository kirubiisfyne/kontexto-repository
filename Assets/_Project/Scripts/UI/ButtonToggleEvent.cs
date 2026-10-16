using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Toggles between a default Cinemachine camera and a zoomed Cinemachine camera.
    /// Can be attached directly to a UI Button or triggered via OnClick / custom event calls.
    /// </summary>
    [AddComponentMenu("UI/Camera Zoom Toggle")]
    public class ButtonToggleEvent : MonoBehaviour
    {
        [Header("Cinemachine Cameras")]
        [Tooltip("The default / unzoomed Cinemachine camera.")]
        [SerializeField] private CinemachineCamera defaultCamera;

        [Tooltip("The zoomed Cinemachine camera to switch to.")]
        [SerializeField] private CinemachineCamera zoomedCamera;

        [Header("Priority Settings")]
        [Tooltip("Priority given to the active camera.")]
        [SerializeField] private int activePriority = 20;

        [Tooltip("Priority given to the inactive camera.")]
        [SerializeField] private int inactivePriority = 0;

        [Header("State")]
        [Tooltip("Current zoom state. When false, triggering will switch to the zoomed camera. When true, triggering will return to the default camera.")]
        [SerializeField] private bool isZoomed = false;

        [Header("Initialization")]
        [Tooltip("If true, ensures camera priorities reflect the starting isZoomed state on Start.")]
        [SerializeField] private bool applyOnStart = true;

        [Header("Auto-Binding")]
        [Tooltip("If true and a Button component is on this GameObject, automatically registers ToggleZoom() to its onClick event. Disable if manually wiring OnClick in the Inspector.")]
        [SerializeField] private bool autoBindToButton = true;

        [Header("Optional Events")]
        [FormerlySerializedAs("onFirstPress")]
        [Tooltip("Optional event triggered when zooming in.")]
        [SerializeField] private UnityEvent onZoomIn = new UnityEvent();

        [FormerlySerializedAs("onSecondPress")]
        [Tooltip("Optional event triggered when zooming out.")]
        [SerializeField] private UnityEvent onZoomOut = new UnityEvent();

        private Button _button;

        public bool IsZoomed => isZoomed;
        public CinemachineCamera DefaultCamera { get => defaultCamera; set => defaultCamera = value; }
        public CinemachineCamera ZoomedCamera { get => zoomedCamera; set => zoomedCamera = value; }
        public UnityEvent OnZoomIn => onZoomIn;
        public UnityEvent OnZoomOut => onZoomOut;

        private void Awake()
        {
            if (autoBindToButton)
            {
                _button = GetComponent<Button>();
            }
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyPriorities(isZoomed);
            }
        }

        private void OnEnable()
        {
            if (autoBindToButton && _button != null)
            {
                _button.onClick.AddListener(ToggleZoom);
            }
        }

        private void OnDisable()
        {
            if (autoBindToButton && _button != null)
            {
                _button.onClick.RemoveListener(ToggleZoom);
            }
        }

        /// <summary>
        /// Main toggle function. Checks isZoomed:
        /// If not zoomed -> switches Cinemachine priority to the zoomed camera.
        /// If zoomed -> switches Cinemachine priority to the default camera.
        /// </summary>
        public void ToggleZoom()
        {
            if (!isZoomed)
            {
                // Switch priority to the zoomed Cinemachine
                ApplyPriorities(true);
                isZoomed = true;
                onZoomIn?.Invoke();
            }
            else
            {
                // Switch priority to the default Cinemachine
                ApplyPriorities(false);
                isZoomed = false;
                onZoomOut?.Invoke();
            }
        }

        /// <summary>
        /// Backward-compatible alias for ToggleZoom().
        /// </summary>
        public void Toggle() => ToggleZoom();

        /// <summary>
        /// Explicitly zoom in (switches priority to zoomed camera).
        /// </summary>
        public void ZoomIn()
        {
            if (!isZoomed)
            {
                ToggleZoom();
            }
        }

        /// <summary>
        /// Explicitly zoom out (switches priority to default camera).
        /// </summary>
        public void ZoomOut()
        {
            if (isZoomed)
            {
                ToggleZoom();
            }
        }

        /// <summary>
        /// Resets the isZoomed state and optionally applies camera priorities immediately.
        /// </summary>
        public void ResetZoom(bool defaultState = false, bool applyPriority = true)
        {
            isZoomed = defaultState;
            if (applyPriority)
            {
                ApplyPriorities(isZoomed);
            }
        }

        private void ApplyPriorities(bool zoomed)
        {
            if (zoomed)
            {
                if (zoomedCamera != null)
                {
                    zoomedCamera.Priority = activePriority;
                }

                if (defaultCamera != null)
                {
                    defaultCamera.Priority = inactivePriority;
                }
            }
            else
            {
                if (zoomedCamera != null)
                {
                    zoomedCamera.Priority = inactivePriority;
                }

                if (defaultCamera != null)
                {
                    defaultCamera.Priority = activePriority;
                }
            }
        }
    }
}
