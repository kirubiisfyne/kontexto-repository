using System;
using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.CutsceneSystem
{
    /// <summary>
    /// Root object for cutscene subtitle JSON mapping.
    /// Supports both "cutscenes" and "conversationMap" keys for JSON consistency with DialogueManager.
    /// </summary>
    [Serializable]
    public class CutsceneSubtitleMap
    {
        public List<CutsceneSequence> cutscenes;
        public List<CutsceneSequence> conversationMap;

        /// <summary>
        /// Returns the loaded sequences regardless of whether "cutscenes" or "conversationMap" key was used.
        /// </summary>
        public List<CutsceneSequence> GetSequences()
        {
            if (cutscenes != null && cutscenes.Count > 0) return cutscenes;
            if (conversationMap != null && conversationMap.Count > 0) return conversationMap;
            return new List<CutsceneSequence>();
        }
    }

    /// <summary>
    /// Represents a single cutscene shot or sequence of lines.
    /// </summary>
    [Serializable]
    public class CutsceneSequence
    {
        public string name;
        public List<CutsceneLine> lines = new List<CutsceneLine>();
    }

    /// <summary>
    /// A single line of cutscene subtitle with optional duration.
    /// </summary>
    [Serializable]
    public class CutsceneLine
    {
        [TextArea(2, 4)]
        public string text;
        [Tooltip("Optional custom duration in seconds. If 0 or negative, manager uses default duration.")]
        public float duration;
    }
}
