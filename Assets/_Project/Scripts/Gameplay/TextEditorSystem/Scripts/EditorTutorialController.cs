using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Master.Scripts.TextEditorSystem
{
    /// <summary>When the tutorial is allowed to start on its own.</summary>
    public enum TutorialTrigger
    {
        /// <summary>Starts once the player closes the instruction e-mail that covers the editor on entry.</summary>
        AfterInstructionEmailClosed,

        /// <summary>Starts on its own after a delay, whatever else is on screen.</summary>
        OnSceneStart,

        /// <summary>Never starts on its own; call <see cref="EditorTutorialController.StartTutorial"/>.</summary>
        ManualOnly
    }

    /// <summary>
    /// Runs the guided tour of the document editor: everything except the tool being explained is dimmed,
    /// the spotlight travels and resizes to that tool, and Penny stands next to it and explains what it does.
    /// Each step advances when the player clicks Penny's confirmation button.
    /// </summary>
    public class EditorTutorialController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The UIDocument that owns the editor UI.")]
        public UIDocument uiDocument;

        [Tooltip("Penny. She narrates the tour and her confirmation button advances it.")]
        public PennyAssistantController pennyAssistant;

        [Tooltip("The authored tour: which tools to spotlight and what Penny says about them.")]
        public EditorTutorialSequence sequence;

        [Header("Trigger")]
        public TutorialTrigger trigger = TutorialTrigger.AfterInstructionEmailClosed;

        [Tooltip("Element name of the editor window. Step targets are resolved inside it, so that " +
                 "same-named controls elsewhere (the e-mail's Exit button, for one) are never picked up.")]
        public string editorWindowElement = "Root";

        [Tooltip("Element name of the instruction e-mail panel, used by the AfterInstructionEmailClosed trigger.")]
        public string instructionEmailElement = "EmailRoot";

        [Tooltip("Seconds to wait after the trigger condition is met before the tour starts.")]
        public float startDelay = 0.35f;

        [Header("One-Time Playback")]
        [Tooltip("Remember that this lesson has been seen so the automatic triggers only fire on the player's " +
                 "first visit. Each lesson keeps its own flag. Calling StartTutorial() from a UnityEvent " +
                 "always plays.")]
        public bool playOnlyOnce = true;

        [Header("Debug")]
        [Tooltip("Testing only: ignore the seen flag so the tour replays on every visit.")]
        public bool replayForTesting;

        [Tooltip("Logs every step's resolved highlight rectangle in panel space, both before and after padding. " +
                 "Use it to read off the numbers for a step's hand-placed area.")]
        public bool logStepGeometry;

        /// <summary>Fired once the tour finishes or is skipped.</summary>
        public System.Action onTutorialCompleted;

        private VisualElement _panelRoot;
        private VisualElement _editorWindow;
        private VisualElement _tutorialRoot;
        private VisualElement _mask;
        private VisualElement _blocker;
        private VisualElement _dimTop;
        private VisualElement _dimBottom;
        private VisualElement _dimLeft;
        private VisualElement _dimRight;
        private Button _skipButton;

        private bool _isRunning;
        private bool _waitingForDismiss;
        private bool _finishRequested;
        private bool _skipped;
        private int _stepIndex = -1;
        private Rect _currentHole;
        private TutorialStep _currentStep;
        private Rect _currentStepHole;
        private Vector2 _lastAnchoredPennySize;

        /// <summary>True while the tour is on screen.</summary>
        public bool IsRunning => _isRunning;

        private static readonly PennyPlacement[] SideOrder =
        {
            PennyPlacement.Below, PennyPlacement.Right, PennyPlacement.Above, PennyPlacement.Left
        };

        private void OnEnable()
        {
            CacheElements();

            if (_skipButton != null) _skipButton.clicked += SkipTutorial;
            if (_tutorialRoot != null) _tutorialRoot.RegisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);

            HideOverlay();
        }

        private void OnDisable()
        {
            if (_skipButton != null) _skipButton.clicked -= SkipTutorial;
            if (_tutorialRoot != null) _tutorialRoot.UnregisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);

            StopAllCoroutines();
            _isRunning = false;
            _waitingForDismiss = false;

            if (pennyAssistant != null)
            {
                pennyAssistant.onFeedbackDismissed = null;
                pennyAssistant.ClearAnchor();
                pennyAssistant.SetIdleSuppressed(false);
            }
        }

        private void Start()
        {
            // Before the null checks below: the day being played may name the lesson it teaches.
            ResolveSequenceForLevel();

            if (sequence == null || sequence.steps == null || sequence.steps.Count == 0)
            {
                Debug.LogWarning("[EditorTutorial] No tutorial sequence assigned (or it has no steps); the tour will not run.", this);
                return;
            }

            if (pennyAssistant == null)
            {
                Debug.LogError("[EditorTutorial] No Penny assistant assigned; the tour cannot run.", this);
                return;
            }

            _editorWindow = _panelRoot?.Q<VisualElement>(editorWindowElement);
            if (_editorWindow == null)
            {
                Debug.LogError($"[EditorTutorial] Could not find the editor window '{editorWindowElement}'; the tour cannot run.", this);
                return;
            }

            ApplyStaticStyling();

            switch (trigger)
            {
                case TutorialTrigger.AfterInstructionEmailClosed:
                    StartCoroutine(WaitForInstructionEmailRoutine());
                    break;
                case TutorialTrigger.OnSceneStart:
                    StartCoroutine(StartAfterDelayRoutine());
                    break;
            }
        }

        private void CacheElements()
        {
            if (uiDocument == null) return;

            _panelRoot = uiDocument.rootVisualElement;
            if (_panelRoot == null) return;

            _tutorialRoot = _panelRoot.Q<VisualElement>("TutorialRoot");
            _mask = _panelRoot.Q<VisualElement>("TutorialMask");
            _blocker = _panelRoot.Q<VisualElement>("TutorialBlocker");
            _dimTop = _panelRoot.Q<VisualElement>("TutorialDimTop");
            _dimBottom = _panelRoot.Q<VisualElement>("TutorialDimBottom");
            _dimLeft = _panelRoot.Q<VisualElement>("TutorialDimLeft");
            _dimRight = _panelRoot.Q<VisualElement>("TutorialDimRight");
            _skipButton = _panelRoot.Q<Button>("TutorialSkip");

            if (_tutorialRoot == null)
                Debug.LogError("[EditorTutorial] The editor UXML has no 'TutorialRoot' element; the tour cannot run.", this);
        }

        #region Public API

        /// <summary>
        /// Plays the tour now. Safe to wire to a UnityEvent; it always plays, even if the seen flag is set.
        /// </summary>
        public void StartTutorial()
        {
            if (_isRunning) return;

            if (_tutorialRoot == null || _editorWindow == null)
            {
                Debug.LogError("[EditorTutorial] The editor UI is not ready; the tour cannot start.", this);
                return;
            }

            if (sequence == null || sequence.steps == null || sequence.steps.Count == 0) return;

            if (pennyAssistant == null || pennyAssistant.Root == null)
            {
                Debug.LogError("[EditorTutorial] Penny is not ready; the tour cannot start.", this);
                return;
            }

            StartCoroutine(RunTutorialRoutine());
        }

        /// <summary>Moves on to the next tool without waiting for Penny's confirmation button.</summary>
        public void NextStep()
        {
            if (!_isRunning) return;

            _waitingForDismiss = false;
            DismissPenny();
        }

        /// <summary>Ends the tour immediately, for example from the Skip button.</summary>
        public void SkipTutorial()
        {
            if (!_isRunning) return;

            _skipped = true;
            _finishRequested = true;
            _waitingForDismiss = false;
            DismissPenny();
        }

        /// <summary>
        /// Ends the tour immediately. Identical to <see cref="SkipTutorial"/> but without the skip bookkeeping,
        /// so the tour still counts as seen.
        /// </summary>
        public void EndTutorial()
        {
            if (!_isRunning) return;

            _finishRequested = true;
            _waitingForDismiss = false;
            DismissPenny();
        }

        /// <summary>Clears the seen flag of the lesson that would play here, so the tour runs again.</summary>
        [ContextMenu("Reset Tutorial Seen Flag")]
        public void ResetSeenFlag()
        {
            if (sequence == null)
            {
                Debug.LogWarning("[EditorTutorial] No lesson is assigned, so there is no seen flag to reset.", this);
                return;
            }

            sequence.ResetSeenFlag();
        }

        #endregion

        #region Triggering

        private IEnumerator StartAfterDelayRoutine()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, startDelay));
            TryAutoStart();
        }

        private IEnumerator WaitForInstructionEmailRoutine()
        {
            VisualElement email = _panelRoot.Q<VisualElement>(instructionEmailElement);

            // One frame first, so the panel has resolved the e-mail's display style; reading it
            // before the first layout pass can report a default and start the tour behind the e-mail.
            yield return null;

            // The e-mail covers the editor on entry, so wait until the player has closed it.
            while (email != null && email.resolvedStyle.display != DisplayStyle.None)
            {
                yield return new WaitForSecondsRealtime(0.2f);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, startDelay));
            TryAutoStart();
        }

        /// <summary>
        /// Lets the day being played choose the lesson it teaches. Each day covers a different tool and the
        /// lessons are kept as separate assets so that any one of them can be edited and debugged on its own,
        /// so the level data is what picks between them. A day that names no lesson falls back to the sequence
        /// assigned in the Inspector, which is also what plays when the editor scene is run on its own.
        /// </summary>
        private void ResolveSequenceForLevel()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.currentLevelData == null) return;

            EditorTutorialSequence lesson = gameManager.currentLevelData.editorTutorial;
            if (lesson == null || lesson == sequence) return;

            sequence = lesson;
        }

        private void TryAutoStart()
        {
            if (_isRunning) return;
            if (playOnlyOnce && !replayForTesting && HasSeenTutorial()) return;

            StartTutorial();
        }

        private bool HasSeenTutorial()
        {
            string key = SeenFlagKey();
            return !string.IsNullOrEmpty(key) && PlayerPrefs.GetInt(key, 0) == 1;
        }

        private void MarkTutorialSeen()
        {
            string key = SeenFlagKey();
            if (string.IsNullOrEmpty(key)) return;

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// The seen flag belongs to the lesson rather than to this component. One controller drives whichever
        /// lesson the day supplies, so a flag stored here would be shared by all of them and only the first
        /// day's lesson would ever play.
        /// </summary>
        private string SeenFlagKey()
        {
            return sequence != null ? sequence.ResolveSeenFlagKey() : null;
        }

        #endregion

        #region Playback

        private IEnumerator RunTutorialRoutine()
        {
            _isRunning = true;
            _finishRequested = false;
            _skipped = false;
            _currentStep = null;
            _currentStepHole = default;
            _lastAnchoredPennySize = Vector2.zero;
            _waitingForDismiss = false;

            pennyAssistant.SetIdleSuppressed(true);   // never let her idle natter overwrite a step
            ApplyStaticStyling();
            SetDimAlpha(0f);                          // start clear; the fade brings it up once the spotlight is placed

            // A hole is clamped to the overlay's own size, and the overlay has no layout at all while
            // it is display:none. So the overlay must be on screen and measured before the first step
            // is built: building one first clamps it to zero, which fails every step and silently
            // drops the entire tour.
            ShowOverlay();
            yield return WaitForLayoutRoutine(_tutorialRoot);

            bool spotlightPlaced = false;

            for (_stepIndex = 0; _stepIndex < sequence.steps.Count && !_finishRequested; _stepIndex++)
            {
                TutorialStep step = sequence.steps[_stepIndex];
                if (!TryBuildHole(step, out Rect hole)) continue;

                if (!spotlightPlaced)
                {
                    // Snap rather than travel: the dim fades in around an already-placed highlight.
                    ApplyHole(hole);
                    yield return FadeDimRoutine(0f, sequence.dimColor.a);
                    spotlightPlaced = true;
                }
                else
                {
                    yield return AnimateHoleRoutine(_currentHole, hole);
                }

                if (_finishRequested) break;

                yield return RunStepRoutine(step, hole);
            }

            yield return FinishRoutine(spotlightPlaced);
        }

        private IEnumerator RunStepRoutine(TutorialStep step, Rect hole)
        {
            // Dismissing her bubble is what advanced us, so she is already fading out while the
            // spotlight travels above. Wait for the fade to land before re-showing her.
            yield return WaitForPennyToHideRoutine();

            _currentStep = step;
            _currentStepHole = hole;

            // Tools that would end the tour or change the screen when clicked get a shield over the hole too.
            SetBlocker(step.holeIsClickThrough);

            pennyAssistant.SetMessage(step.message);
            yield return WaitForLayoutRoutine(pennyAssistant.Root);
            AnchorPenny(step, hole);

            _waitingForDismiss = true;
            pennyAssistant.onFeedbackDismissed = OnFeedbackDismissed;
            pennyAssistant.ShowFeedback(step.message, false);

            bool becameVisible = false;
            while (_waitingForDismiss && !_finishRequested)
            {
                if (pennyAssistant.IsShowing) becameVisible = true;

                // Safety net: if something else dismissed her (or replaced the callback), move on
                // rather than stranding the player in a tour that cannot advance.
                else if (becameVisible) break;

                yield return null;
            }

            ClearDismissCallback();
            _waitingForDismiss = false;
        }

        private IEnumerator FinishRoutine(bool anythingShown)
        {
            // Cleared first, so a Skip arriving during the outro is ignored.
            _isRunning = false;
            _waitingForDismiss = false;

            ClearDismissCallback();
            SetBlocker(true);

            // Lift the dim and let Penny finish fading out, so the exit is not a hard cut.
            if (sequence != null) yield return FadeDimRoutine(sequence.dimColor.a, 0f);
            yield return WaitForPennyToHideRoutine();

            if (pennyAssistant != null)
            {
                pennyAssistant.ClearAnchor();   // she is invisible by now, so the snap cannot be seen
                pennyAssistant.SetIdleSuppressed(false);
            }

            HideOverlay();
            _currentStep = null;

            if (!anythingShown)
            {
                // Every step failed to resolve, so the player saw nothing. Marking the tour seen here
                // would burn the one-time flag on a run that never happened, disabling it for good.
                Debug.LogError($"[EditorTutorial] No step could be shown, so the tour did nothing. The seen " +
                               $"flag was left unset so it can run again. Check that every step's target " +
                               $"names match elements inside '{editorWindowElement}'.", this);
            }
            else if (playOnlyOnce && !replayForTesting && !_skipped)
            {
                MarkTutorialSeen();
            }

            onTutorialCompleted?.Invoke();
        }

        private void OnFeedbackDismissed()
        {
            _waitingForDismiss = false;
        }

        /// <summary>
        /// Takes back the dismissal handler, but only if it is still ours: another system may have
        /// claimed it in the meantime, and clearing theirs would silently break their flow.
        /// </summary>
        private void ClearDismissCallback()
        {
            if (pennyAssistant == null) return;

            if (pennyAssistant.onFeedbackDismissed == (System.Action)OnFeedbackDismissed)
                pennyAssistant.onFeedbackDismissed = null;
        }

        private void DismissPenny()
        {
            if (pennyAssistant != null && pennyAssistant.IsShowing) pennyAssistant.HideFeedback();
        }

        private IEnumerator WaitForPennyToHideRoutine()
        {
            if (pennyAssistant == null) yield break;

            // Her own fade runs on scaled time, so poll with a ceiling instead of trusting it to finish.
            float timeout = Mathf.Max(0f, pennyAssistant.fadeDuration) + 0.5f;
            float elapsed = 0f;

            while (pennyAssistant.IsShowing && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        #endregion

        #region Geometry

        /// <summary>
        /// Resolves a step's targets and returns the highlight rect, in the overlay's coordinate space.
        /// A step highlights the union of its targets, so "Bold" + "Italic" reads as one group.
        /// </summary>
        private bool TryBuildHole(TutorialStep step, out Rect hole)
        {
            hole = default;

            if (step == null || _editorWindow == null) return false;

            Rect union;

            if (step.useExplicitArea)
            {
                // Hand-placed: the designer measured this rectangle themselves, so nothing is looked up.
                union = step.area;

                if (union.width <= 0f || union.height <= 0f)
                {
                    Debug.LogWarning($"[EditorTutorial] Step '{step.label}' is set to use a hand-placed area, but " +
                                     $"its size is {union.width}x{union.height}; skipping it.", this);
                    return false;
                }
            }
            else
            {
                if (step.targets == null || step.targets.Count == 0) return false;

                bool found = false;
                union = default;

                foreach (string targetName in step.targets)
                {
                    if (string.IsNullOrWhiteSpace(targetName)) continue;

                    VisualElement target = _editorWindow.Q<VisualElement>(targetName.Trim());
                    if (target == null)
                    {
                        Debug.LogWarning($"[EditorTutorial] Step '{step.label}' targets '{targetName}', which was not found inside '{editorWindowElement}'.", this);
                        continue;
                    }

                    Rect bounds = ToOverlaySpace(target);
                    if (bounds.width <= 0f || bounds.height <= 0f) continue;   // hidden or not laid out yet

                    union = found ? Union(union, bounds) : bounds;
                    found = true;
                }

                if (!found)
                {
                    Debug.LogWarning($"[EditorTutorial] Step '{step.label}' has no targets that are currently visible; skipping it.", this);
                    return false;
                }

                WarnIfUnionIsTooWide(step, union);
            }

            float padding = sequence.GetPadding(step);
            hole = ClampToPanel(new Rect(union.xMin - padding, union.yMin - padding,
                                         union.width + padding * 2f, union.height + padding * 2f));

            if (logStepGeometry)
            {
                Vector2 panel = OverlaySize();
                Debug.Log($"[EditorTutorial] '{step.label}': area x={Mathf.RoundToInt(union.xMin)} " +
                          $"y={Mathf.RoundToInt(union.yMin)} w={Mathf.RoundToInt(union.width)} " +
                          $"h={Mathf.RoundToInt(union.height)} -> hole x={Mathf.RoundToInt(hole.xMin)} " +
                          $"y={Mathf.RoundToInt(hole.yMin)} w={Mathf.RoundToInt(hole.width)} " +
                          $"h={Mathf.RoundToInt(hole.height)} | panel {Mathf.RoundToInt(panel.x)}x{Mathf.RoundToInt(panel.y)}",
                          this);
            }

            return hole.width > 1f && hole.height > 1f;
        }

        /// <summary>
        /// Targets that sit far apart produce a highlight covering most of the screen, which dims
        /// nothing and so teaches nothing. Almost always an authoring slip.
        /// </summary>
        private void WarnIfUnionIsTooWide(TutorialStep step, Rect union)
        {
            Vector2 panel = OverlaySize();
            float panelArea = panel.x * panel.y;
            if (panelArea <= 0f) return;

            float coverage = (union.width * union.height) / panelArea;
            if (coverage <= 0.6f) return;

            Debug.LogWarning($"[EditorTutorial] Step '{step.label}' highlights {Mathf.RoundToInt(coverage * 100f)}% of " +
                             "the screen. Group only tools that sit next to each other, or split the step.", this);
        }

        /// <summary>
        /// Converts a target's bounds into the overlay's coordinate space. Both rects are already in
        /// panel space and the overlay is axis-aligned and untransformed, so a translation is all that
        /// separates them: no DPI, resolution or panel-scale maths is involved.
        /// </summary>
        private Rect ToOverlaySpace(VisualElement element)
        {
            Rect bounds = element.worldBound;
            Vector2 origin = _tutorialRoot.worldBound.position;

            return new Rect(bounds.position - origin, bounds.size);
        }

        private Vector2 OverlaySize()
        {
            Vector2 size = _tutorialRoot.layout.size;
            return new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
        }

        private Rect ClampToPanel(Rect rect)
        {
            Vector2 panel = OverlaySize();
            float xMin = Mathf.Clamp(rect.xMin, 0f, panel.x);
            float yMin = Mathf.Clamp(rect.yMin, 0f, panel.y);
            float xMax = Mathf.Clamp(rect.xMax, 0f, panel.x);
            float yMax = Mathf.Clamp(rect.yMax, 0f, panel.y);

            return new Rect(xMin, yMin, Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }

        /// <summary>Derives the four dim panels and the highlight mask from a single rect.</summary>
        private void ApplyHole(Rect hole)
        {
            // Whole pixels: the four panels share edges, and a fractional one rasterises as a
            // seam where two half-covered pixels blend to double the intended alpha.
            hole = new Rect(Mathf.Round(hole.x), Mathf.Round(hole.y),
                            Mathf.Round(hole.width), Mathf.Round(hole.height));

            _currentHole = hole;

            Vector2 panel = OverlaySize();
            float holeHeight = Mathf.Max(0f, hole.height);

            SetRect(_dimTop, 0f, 0f, panel.x, Mathf.Max(0f, hole.yMin));
            SetRect(_dimBottom, 0f, hole.yMax, panel.x, Mathf.Max(0f, panel.y - hole.yMax));
            SetRect(_dimLeft, 0f, hole.yMin, Mathf.Max(0f, hole.xMin), holeHeight);
            SetRect(_dimRight, hole.xMax, hole.yMin, Mathf.Max(0f, panel.x - hole.xMax), holeHeight);
            SetRect(_mask, hole.xMin, hole.yMin, Mathf.Max(0f, hole.width), holeHeight);
        }

        private static void SetRect(VisualElement element, float left, float top, float width, float height)
        {
            if (element == null) return;

            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.height = height;
        }

        private static Rect Union(Rect a, Rect b)
        {
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);

            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static Rect LerpRect(Rect from, Rect to, float t)
        {
            return new Rect(Mathf.Lerp(from.x, to.x, t),
                            Mathf.Lerp(from.y, to.y, t),
                            Mathf.Lerp(from.width, to.width, t),
                            Mathf.Lerp(from.height, to.height, t));
        }

        private static float EaseOutCubic(float t)
        {
            float inverted = 1f - t;
            return 1f - inverted * inverted * inverted;
        }

        /// <summary>
        /// Waits for the panel to finish laying out after a content change, so sizes read afterwards are
        /// the real ones. Bounded, so a change that produces no geometry event can never stall the tour.
        /// </summary>
        private IEnumerator WaitForLayoutRoutine(VisualElement element)
        {
            if (element == null) yield break;

            for (int i = 0; i < 3; i++)
            {
                bool changed = false;
                EventCallback<GeometryChangedEvent> callback = _ => changed = true;

                element.RegisterCallback(callback);
                yield return null;
                element.UnregisterCallback(callback);

                if (!changed) continue;

                // A geometry change means the panel re-laid out; one more frame settles it.
                yield return null;
                yield break;
            }
        }

        #endregion

        #region Penny

        private void AnchorPenny(TutorialStep step, Rect hole)
        {
            if (pennyAssistant?.Root == null) return;

            Vector2 pennySize = pennyAssistant.Root.layout.size;
            if (pennySize.x <= 1f || pennySize.y <= 1f) return;

            _lastAnchoredPennySize = pennySize;
            pennyAssistant.SetAnchor(ComputePennyPosition(step, hole, pennySize));
        }

        /// <summary>
        /// Places Penny next to the highlight. The step's chosen side wins as long as it fits without
        /// being shoved and does not bury the spotlight; otherwise the other sides are tried in turn.
        /// </summary>
        private Vector2 ComputePennyPosition(TutorialStep step, Rect hole, Vector2 pennySize)
        {
            Vector2 panel = OverlaySize();
            float gap = Mathf.Max(0f, sequence.pennyGap);
            const float margin = 8f;

            PennyPlacement preferred = step?.placement ?? PennyPlacement.Below;
            Vector2 nudge = step?.pennyOffset ?? Vector2.zero;

            Vector2 best = Vector2.zero;
            float bestScore = float.NegativeInfinity;

            // Pass 0 offers only the requested side; pass 1 offers the rest as fallbacks.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < SideOrder.Length; i++)
                {
                    PennyPlacement side = SideOrder[i];
                    if (pass == 0 && preferred != PennyPlacement.Auto && side != preferred) continue;

                    Vector2 want = SidePosition(side, hole, pennySize, gap) + nudge;
                    Vector2 got = ClampPenny(want, pennySize, panel, margin);
                    Rect rect = new Rect(got, pennySize);

                    float score = -(pass * 1000f + i);
                    score -= (got - want).magnitude * 10f;                 // how far it had to be shoved to fit
                    if (rect.Overlaps(hole)) score -= 100000f;             // never sit on the spotlight

                    if (score <= bestScore) continue;

                    best = got;
                    bestScore = score;
                }
            }

            // SideOrder always offers at least one candidate, so `best` is always filled in.
            return best;
        }

        private static Vector2 SidePosition(PennyPlacement side, Rect hole, Vector2 pennySize, float gap)
        {
            switch (side)
            {
                case PennyPlacement.Above:
                    return new Vector2(hole.center.x - pennySize.x * 0.5f, hole.yMin - gap - pennySize.y);
                case PennyPlacement.Left:
                    return new Vector2(hole.xMin - gap - pennySize.x, hole.center.y - pennySize.y * 0.5f);
                case PennyPlacement.Right:
                    return new Vector2(hole.xMax + gap, hole.center.y - pennySize.y * 0.5f);
                default:   // Below
                    return new Vector2(hole.center.x - pennySize.x * 0.5f, hole.yMax + gap);
            }
        }

        private static Vector2 ClampPenny(Vector2 position, Vector2 pennySize, Vector2 panel, float margin)
        {
            position.x = Mathf.Clamp(position.x, margin, Mathf.Max(margin, panel.x - pennySize.x - margin));
            position.y = Mathf.Clamp(position.y, margin, Mathf.Max(margin, panel.y - pennySize.y - margin));

            return position;
        }

        private void ReanchorPenny()
        {
            if (pennyAssistant?.Root == null || _currentStep == null) return;

            Vector2 pennySize = pennyAssistant.Root.layout.size;
            if (pennySize == _lastAnchoredPennySize) return;

            _lastAnchoredPennySize = pennySize;
            pennyAssistant.SetAnchor(ComputePennyPosition(_currentStep, _currentStepHole, pennySize));
        }

        #endregion

        #region Overlay

        private void ShowOverlay()
        {
            if (_tutorialRoot == null) return;

            _tutorialRoot.style.display = DisplayStyle.Flex;
            SetDimPickable(sequence != null && sequence.blockInputOutsideHighlight);
        }

        private void HideOverlay()
        {
            if (_tutorialRoot == null) return;

            _tutorialRoot.style.display = DisplayStyle.None;
            SetDimPickable(false);
        }

        /// <summary>
        /// When pickable, the dim panels swallow the player's clicks everywhere except the highlight,
        /// leaving the spotlighted tool usable.
        /// </summary>
        private void SetDimPickable(bool pickable)
        {
            PickingMode mode = pickable ? PickingMode.Position : PickingMode.Ignore;

            if (_dimTop != null) _dimTop.pickingMode = mode;
            if (_dimBottom != null) _dimBottom.pickingMode = mode;
            if (_dimLeft != null) _dimLeft.pickingMode = mode;
            if (_dimRight != null) _dimRight.pickingMode = mode;
        }

        /// <summary>Shields the highlight from clicks when a step's tool must not be used mid-tour.</summary>
        private void SetBlocker(bool clickThrough)
        {
            if (_blocker == null) return;

            bool active = !clickThrough && sequence != null && sequence.blockInputOutsideHighlight;

            _blocker.pickingMode = active ? PickingMode.Position : PickingMode.Ignore;
            _blocker.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ApplyStaticStyling()
        {
            if (sequence == null) return;

            if (_mask != null)
            {
                float ringWidth = sequence.showRing ? Mathf.Max(0f, sequence.ringWidth) : 0f;
                _mask.style.borderTopWidth = ringWidth;
                _mask.style.borderRightWidth = ringWidth;
                _mask.style.borderBottomWidth = ringWidth;
                _mask.style.borderLeftWidth = ringWidth;
                _mask.style.borderTopColor = sequence.ringColor;
                _mask.style.borderRightColor = sequence.ringColor;
                _mask.style.borderBottomColor = sequence.ringColor;
                _mask.style.borderLeftColor = sequence.ringColor;
            }

            if (_skipButton != null)
            {
                _skipButton.text = string.IsNullOrEmpty(sequence.skipLabel) ? "Skip Tutorial" : sequence.skipLabel;
                _skipButton.style.display = sequence.allowSkip ? DisplayStyle.Flex : DisplayStyle.None;
            }

            SetDimAlpha(sequence.dimColor.a);
        }

        private void SetDimAlpha(float alpha)
        {
            if (sequence == null) return;

            Color color = sequence.dimColor;
            color.a = Mathf.Clamp01(alpha);

            if (_dimTop != null) _dimTop.style.backgroundColor = color;
            if (_dimBottom != null) _dimBottom.style.backgroundColor = color;
            if (_dimLeft != null) _dimLeft.style.backgroundColor = color;
            if (_dimRight != null) _dimRight.style.backgroundColor = color;
        }

        private void OnOverlayGeometryChanged(GeometryChangedEvent evt)
        {
            // The overlay stretches with the panel, so this is the panel being resized. Keep the
            // spotlight and Penny pinned to the tool they are pointing at.
            if (!_isRunning || _currentStep == null) return;

            if (!TryBuildHole(_currentStep, out Rect hole)) return;

            ApplyHole(hole);
            _currentStepHole = hole;
            ReanchorPenny();
        }

        #endregion

        #region Animation

        private IEnumerator FadeDimRoutine(float fromAlpha, float toAlpha)
        {
            float duration = Mathf.Max(0f, sequence.fadeInDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetDimAlpha(Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetDimAlpha(toAlpha);
        }

        private IEnumerator AnimateHoleRoutine(Rect from, Rect to)
        {
            float duration = Mathf.Max(0.01f, sequence.moveDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (_finishRequested) break;

                elapsed += Time.unscaledDeltaTime;
                ApplyHole(LerpRect(from, to, EaseOutCubic(Mathf.Clamp01(elapsed / duration))));
                yield return null;
            }

            ApplyHole(to);
        }

        #endregion
    }
}
