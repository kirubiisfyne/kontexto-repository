using System;
using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.CutsceneSystem
{
    /// <summary>
    /// Simplified, JSON-driven subtitle player designed for cutscenes.
    /// Supports multiple lines per shot with reader-paced progression.
    /// </summary>
    public class CutsceneSubtitleManager : MonoBehaviour
    {
        [Header("Data Asset")]
        [SerializeField] private TextAsset subtitleJson;

        [Header("UI Reference")]
        [SerializeField] private CutsceneSubtitleUI subtitleUI;

        [Header("Runtime State")]
        public CutsceneDialogueData dialogueData;
        public bool isIntroMode = true;
        public int currentShotIndex = -1;

        private List<string> currentShotLines = new List<string>();
        private int currentLineIndex = -1;

        private void Awake()
        {
            if (subtitleUI == null)
            {
                subtitleUI = GetComponentInChildren<CutsceneSubtitleUI>();
            }

            if (subtitleJson != null)
            {
                LoadFromJSON(subtitleJson.text);
            }
        }

        #region Public API

        public bool IsTyping => subtitleUI != null && subtitleUI.IsTyping;

        public bool HasMoreLinesForCurrentShot => 
            currentShotLines != null && currentLineIndex < currentShotLines.Count - 1;

        public void FinishTyping()
        {
            if (subtitleUI != null) subtitleUI.FinishTyping();
        }

        /// <summary>
        /// Assigns and parses a new dialogue JSON at runtime.
        /// </summary>
        public void SetSubtitleJson(TextAsset newJson)
        {
            subtitleJson = newJson;
            if (subtitleJson != null)
            {
                LoadFromJSON(subtitleJson.text);
            }
        }

        /// <summary>
        /// Sets whether current cutscene playback is Intro or Outro.
        /// </summary>
        public void SetMode(bool isIntro)
        {
            isIntroMode = isIntro;
        }

        /// <summary>
        /// Loads and displays the first line for the specified shot index.
        /// </summary>
        public void LoadShotDialogue(bool isIntro, int shotIndex)
        {
            isIntroMode = isIntro;
            currentShotIndex = shotIndex;
            currentShotLines = dialogueData != null ? dialogueData.GetLinesForShot(isIntro, shotIndex) : null;
            currentLineIndex = -1;

            if (currentShotLines != null && currentShotLines.Count > 0)
            {
                AdvanceLine();
            }
            else
            {
                HideDialogue();
            }
        }

        /// <summary>
        /// Advances to the next line within the current shot.
        /// </summary>
        public void AdvanceLine()
        {
            currentLineIndex++;
            if (currentShotLines != null && currentLineIndex >= 0 && currentLineIndex < currentShotLines.Count)
            {
                if (subtitleUI != null)
                {
                    subtitleUI.DisplayLine(currentShotLines[currentLineIndex]);
                }
            }
            else
            {
                HideDialogue();
            }
        }

        /// <summary>
        /// Compatibility overload for LoadShotDialogue.
        /// </summary>
        public void DisplayShotDialogue(bool isIntro, int shotIndex)
        {
            LoadShotDialogue(isIntro, shotIndex);
        }

        public void DisplayShotDialogue(int shotIndex)
        {
            LoadShotDialogue(isIntroMode, shotIndex);
        }

        /// <summary>
        /// Hides the subtitle display.
        /// </summary>
        public void HideDialogue()
        {
            if (subtitleUI != null)
            {
                subtitleUI.Hide();
            }
        }

        /// <summary>
        /// Parses the JSON text into CutsceneDialogueData supporting multi-line shot lists.
        /// </summary>
        public void LoadFromJSON(string jsonContent)
        {
            if (string.IsNullOrEmpty(jsonContent)) return;

            try
            {
                dialogueData = CutsceneDialogueData.Parse(jsonContent);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CutsceneSubtitleManager] Failed parsing cutscene dialogue JSON: {e.Message}");
            }
        }

        #endregion
    }
}
