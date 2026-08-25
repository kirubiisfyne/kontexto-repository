using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Master.Scripts.SaveSystem;

namespace Master.Scripts
{
    /// <summary>
    /// Controls cutscene playback in scn_cutscene by cycling through an array
    /// of shot prefabs provided by the active LevelSequenceData.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        [Header("Scene Routing")]
        [SerializeField] private string gameplaySceneName = "scn_campus";
        [SerializeField] private string cutsceneSceneName = "scn_cutscene";
        [SerializeField] private string mainMenuSceneName = "scn_main-menu";

        [Header("Editor Testing Override")]
        [Tooltip("If assigned, overrides GameManager for instant testing in Editor.")]
        public LevelSequenceData editorOverrideSequence;
        public CutsceneMode editorCutsceneMode = CutsceneMode.Intro;

        [Header("Input Settings")]
        public KeyCode primaryAdvanceKey = KeyCode.Space;
        public KeyCode secondaryAdvanceKey = KeyCode.Return;
        public KeyCode interactAdvanceKey = KeyCode.F;
        public bool advanceOnMouseClick = false;
        [Tooltip("Minimum time (in seconds) between shot advances to prevent accidental skipping.")]
        public float inputCooldown = 0.25f;

        [Header("Hierarchy Container")]
        [Tooltip("Optional parent transform to spawn shot prefabs under.")]
        public Transform shotSpawnContainer;

        [Header("UI Controls (Optional)")]
        public Button nextButton;
        public Button skipButton;

        [Header("Runtime State")]
        [SerializeField] private LevelSequenceData activeSequence;
        [SerializeField] private CutsceneMode activeMode = CutsceneMode.Intro;
        [SerializeField] private int currentShotIndex = 0;
        [SerializeField] private GameObject currentShotInstance;

        private List<GameObject> activeShots = new List<GameObject>();
        private bool isTransitioning = false;
        private float lastAdvanceTime = 0f;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(AdvanceShot);
            if (skipButton != null) skipButton.onClick.AddListener(SkipCutscene);
        }

        private void Start()
        {
            // Ensure cursor is freed and visible for cutscene interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            lastAdvanceTime = Time.unscaledTime;

            ResolveActiveSequence();
            InitializeShotList();

            if (activeShots.Count == 0)
            {
                Debug.Log($"[CutscenePlayer] No {activeMode} shots found for sequence {(activeSequence != null ? activeSequence.name : "None")}. Proceeding...");
                StartCoroutine(CompleteCutsceneRoutine());
                return;
            }

            SpawnCurrentShot();
        }

        private void Update()
        {
            if (isTransitioning || Time.unscaledTime < lastAdvanceTime + inputCooldown) return;

            if (Input.GetKeyDown(primaryAdvanceKey) ||
                Input.GetKeyDown(secondaryAdvanceKey) ||
                Input.GetKeyDown(interactAdvanceKey) ||
                (advanceOnMouseClick && Input.GetMouseButtonDown(0)))
            {
                AdvanceShot();
            }
        }

        private void ResolveActiveSequence()
        {
            #if UNITY_EDITOR
            if (editorOverrideSequence != null)
            {
                activeSequence = editorOverrideSequence;
                activeMode = editorCutsceneMode;
                return;
            }
            #endif

            if (GameManager.Instance != null)
            {
                activeSequence = GameManager.Instance.currentLevelSequence;
                activeMode = GameManager.Instance.cutsceneMode;

                // Fallback: If currentLevelSequence wasn't set, try resolving from currentLevelData
                if (activeSequence == null && GameManager.Instance.levelDatabase != null && GameManager.Instance.currentLevelData != null)
                {
                    activeSequence = GameManager.Instance.levelDatabase.GetSequenceForLevelData(GameManager.Instance.currentLevelData);
                }
            }
        }

        private void InitializeShotList()
        {
            activeShots.Clear();
            currentShotIndex = 0;

            if (activeSequence == null) return;

            var sourceList = (activeMode == CutsceneMode.Intro)
                ? activeSequence.introCutscenePrefabs
                : activeSequence.outroCutscenePrefabs;

            if (sourceList != null)
            {
                foreach (var shot in sourceList)
                {
                    if (shot != null) activeShots.Add(shot);
                }
            }
        }

        private void SpawnCurrentShot()
        {
            if (currentShotInstance != null)
            {
                Destroy(currentShotInstance);
            }

            if (currentShotIndex >= 0 && currentShotIndex < activeShots.Count)
            {
                GameObject prefab = activeShots[currentShotIndex];
                if (prefab != null)
                {
                    currentShotInstance = (shotSpawnContainer != null)
                        ? Instantiate(prefab, shotSpawnContainer)
                        : Instantiate(prefab, Vector3.zero, Quaternion.identity);
                }
            }
        }

        public void AdvanceShot()
        {
            if (isTransitioning || Time.unscaledTime < lastAdvanceTime + inputCooldown) return;
            lastAdvanceTime = Time.unscaledTime;

            currentShotIndex++;

            if (currentShotIndex < activeShots.Count)
            {
                SpawnCurrentShot();
            }
            else
            {
                StartCoroutine(CompleteCutsceneRoutine());
            }
        }

        public void SkipCutscene()
        {
            if (isTransitioning) return;
            StartCoroutine(CompleteCutsceneRoutine());
        }

        private IEnumerator CompleteCutsceneRoutine()
        {
            isTransitioning = true;

            if (TransitionManager.Instance != null)
            {
                yield return TransitionManager.Instance.PlayTransitionAndWait("transition");
            }

            if (activeMode == CutsceneMode.Intro)
            {
                // Finished Intro -> Load Gameplay scene
                SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
            }
            else
            {
                // Finished Outro -> Advance to next level or return to Main Menu
                HandlePostOutroProgression();
            }
        }

        private void HandlePostOutroProgression()
        {
            if (GameManager.Instance == null || GameManager.Instance.levelDatabase == null)
            {
                SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
                return;
            }

            var nextSequence = GameManager.Instance.levelDatabase.GetNextLevel(activeSequence);

            if (nextSequence != null)
            {
                // Prepare next level
                GameManager.Instance.SetLevel(nextSequence, CutsceneMode.Intro);

                // Reset player position in save data so they spawn at the new day's spawn anchor
                PlayerData playerData = SaveManager.Load();
                if (playerData != null && nextSequence.levelData != null)
                {
                    playerData.currentScene = nextSequence.levelData.sceneId;
                    playerData.playerPosition = null;
                    SaveManager.Save(playerData);
                }

                if (nextSequence.HasIntro)
                {
                    // Reload Cutscene scene for the next level's Intro
                    SceneManager.LoadScene(cutsceneSceneName, LoadSceneMode.Single);
                }
                else
                {
                    // No intro cutscene, load directly into gameplay
                    SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
                }
            }
            else
            {
                Debug.Log("<color=gold>[CutscenePlayer]</color> All levels completed! Returning to Main Menu.");
                SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Single);
            }
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(AdvanceShot);
            if (skipButton != null) skipButton.onClick.RemoveListener(SkipCutscene);
        }
    }
}
