using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Master.Scripts.UI
{
    /// <summary>
    /// Automatically enforces correct CanvasScaler settings on all Canvases
    /// across every loaded scene. Attach to a persistent GameObject or use
    /// alongside DontDestroyOnLoad for cross-scene enforcement.
    /// </summary>
    public class UIScalingEnforcer : MonoBehaviour
    {
        public static UIScalingEnforcer Instance { get; private set; }

        [Header("Scaling Configuration")]
        [Tooltip("The resolution your UI was designed at.")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Tooltip("0 = match width, 1 = match height, 0.5 = balanced.")]
        [Range(0f, 1f)]
        [SerializeField] private float matchWidthOrHeight = 0.5f;

        [Tooltip("If true, logs every Canvas it corrects (disable in release builds).")]
        [SerializeField] private bool enableLogging = true;

        private void Awake()
        {
            // Singleton: if an instance already exists, destroy this duplicate
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Fix all Canvases in the current scene immediately
            EnforceScalingOnAllCanvases();
        }

        private void OnEnable()
        {
            // Re-apply whenever a new scene loads (additive or single)
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnforceScalingOnAllCanvases();
        }

        /// <summary>
        /// Finds every CanvasScaler in the scene and corrects its settings.
        /// </summary>
        private void EnforceScalingOnAllCanvases()
        {
            // FindObjectsByType is the modern, non-deprecated replacement
            // for FindObjectsOfType (Unity 2022.2+). Use FindObjectsOfType
            // if you're on an older Unity version.
            var scalers = FindObjectsByType<CanvasScaler>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var scaler in scalers)
            {
                // Skip World Space canvases — they don't use screen scaling
                var canvas = scaler.GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                bool wasChanged = false;

                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    wasChanged = true;
                }

                if (scaler.referenceResolution != referenceResolution)
                {
                    scaler.referenceResolution = referenceResolution;
                    wasChanged = true;
                }

                if (scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
                {
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    wasChanged = true;
                }

                if (!Mathf.Approximately(scaler.matchWidthOrHeight, matchWidthOrHeight))
                {
                    scaler.matchWidthOrHeight = matchWidthOrHeight;
                    wasChanged = true;
                }

                if (wasChanged && enableLogging)
                {
                    /* Debug.Log(
                        $"[UIScalingEnforcer] Corrected CanvasScaler on " +
                        $"'{scaler.gameObject.name}' in scene '{scaler.gameObject.scene.name}' → " +
                        $"ScaleWithScreenSize @ {referenceResolution.x}×{referenceResolution.y}, " +
                        $"match={matchWidthOrHeight}",
                        scaler
                    ); */
                }
            }
        }
    }
}
