using System;
using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.TextEditorSystem
{
    /// <summary>
    /// Where Penny should stand relative to the spotlighted tool.
    /// </summary>
    public enum PennyPlacement
    {
        Below,
        Above,
        Left,
        Right,

        /// <summary>Picks whichever side has the most room on screen.</summary>
        Auto
    }

    /// <summary>
    /// One highlighted tool (or group of tools) and what Penny says about it.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        [Tooltip("Designer-facing name for this step. Only shown in the Inspector.")]
        public string label;

        [Tooltip("Names of the VisualElements to spotlight, e.g. Bold, Italic. They are resolved inside the editor " +
                 "window, so list two or more to highlight them together as one group. Ignored when Use Explicit " +
                 "Area is on.")]
        public List<string> targets = new List<string>();

        [Tooltip("Place this step's highlight by hand instead of looking up elements. Use this when a control's " +
                 "measured bounds do not line up with the tool you want to point at.")]
        public bool useExplicitArea;

        [Tooltip("The hand-placed highlight, in panel space: (0,0) is the top-left of the whole UI, not of the " +
                 "editor window, and the space is the 1920x1080 reference resolution, so these numbers hold at " +
                 "any window size. X/Y is the top-left corner, W/H the size, both measured BEFORE this step's " +
                 "padding is added. Turn on Log Step Geometry on the controller to read the numbers off a run, " +
                 "or set Override Padding with a padding of 0 to use this rectangle exactly as typed.")]
        public Rect area;

        [TextArea(2, 5)]
        [Tooltip("What Penny says while this tool is highlighted.")]
        public string message;

        [Tooltip("Use a custom highlight padding for this step instead of the sequence default.")]
        public bool overridePadding;

        [Tooltip("Extra pixels added on every side of the tool's bounds. A 32x64 tool with a padding of 8 " +
                 "becomes a 48x80 highlight.")]
        public float padding = 8f;

        [Tooltip("Where Penny stands relative to the highlight. Auto picks whichever side has the most room.")]
        public PennyPlacement placement = PennyPlacement.Below;

        [Tooltip("Extra nudge applied after Penny is placed, for fine-tuning a specific step.")]
        public Vector2 pennyOffset;

        [Tooltip("Let the player click the highlighted tool while this step is up. Turn this off for tools " +
                 "that change the screen or end the tour when clicked, such as Print.")]
        public bool holeIsClickThrough = true;
    }

    /// <summary>
    /// An ordered tour of the document editor: which control to spotlight and what Penny says about it.
    /// The tour dims everything except the highlighted tool and moves Penny to the highlighted control.
    /// </summary>
    [CreateAssetMenu(fileName = "EditorTutorialSequence", menuName = "Text Editor System/Editor Tutorial Sequence")]
    public class EditorTutorialSequence : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Leave this empty and the lesson derives its own 'seen' flag from the asset's name, so every " +
                 "lesson remembers being played separately. Set it only to make two assets share one flag.")]
        public string seenFlagKey;

        [Header("Steps")]
        [Tooltip("Played in order. Each step advances when the player clicks Penny's confirmation button.")]
        public List<TutorialStep> steps = new List<TutorialStep>();

        [Header("Timing")]
        [Tooltip("Seconds the spotlight takes to travel and resize between tools.")]
        public float moveDuration = 0.35f;

        [Tooltip("Seconds the dim takes to fade in when the tour starts.")]
        public float fadeInDuration = 0.4f;

        [Header("Spotlight")]
        [Tooltip("Colour of the dim layer that covers everything except the highlighted tool.")]
        public Color dimColor = new Color(0f, 0f, 0f, 0.65f);

        [Tooltip("Extra pixels added on every side of every highlight, unless a step overrides it.")]
        public float defaultPadding = 8f;

        [Tooltip("Draw a ring around the highlight to pull the eye to it.")]
        public bool showRing = true;
        public Color ringColor = Color.white;
        public float ringWidth = 2f;

        [Header("Penny")]
        [Tooltip("Gap in pixels between the highlight and Penny.")]
        public float pennyGap = 16f;

        [Header("Input")]
        [Tooltip("Block the player's clicks everywhere except inside the highlight while the tour runs. " +
                 "The highlighted tool stays clickable so the player can try it.")]
        public bool blockInputOutsideHighlight = true;

        [Header("Skip")]
        [Tooltip("Offer a Skip button so the player can leave the tour early.")]
        public bool allowSkip = true;
        public string skipLabel = "Skip Tutorial";

        /// <summary>
        /// Padding to use for a step, falling back to the sequence default.
        /// </summary>
        public float GetPadding(TutorialStep step)
        {
            if (step == null) return defaultPadding;
            return step.overridePadding ? step.padding : defaultPadding;
        }

        /// <summary>
        /// PlayerPrefs key remembering that this lesson has been played. It defaults to a key derived from the
        /// asset name, which is what keeps each lesson's one-time flag independent of every other lesson.
        /// </summary>
        public string ResolveSeenFlagKey()
        {
            if (!string.IsNullOrEmpty(seenFlagKey)) return seenFlagKey;
            return $"kontexto.editorTutorial.{name}.seen";
        }

        /// <summary>Forgets that this lesson was played, so it can run again.</summary>
        [ContextMenu("Reset Seen Flag")]
        public void ResetSeenFlag()
        {
            string key = ResolveSeenFlagKey();
            if (string.IsNullOrEmpty(key)) return;

            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[EditorTutorial] Cleared the seen flag '{key}' for lesson '{name}'.", this);
        }
    }
}
