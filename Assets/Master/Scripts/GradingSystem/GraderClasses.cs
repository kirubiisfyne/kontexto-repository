using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.GradingSystem
{
    [System.Serializable]
    public class DocumentData
    {
        public List<string> startingTextBlocks; 
        public List<BlockRequirement> answerKey; 
    }

    [System.Serializable]
    public class BlockRequirement
    {
        public string targetTextSnippet; 
        public int requiredSize;
        public bool requireBold;
        public bool requireItalic;
        public TextAnchor requiredAlignment;
    }
}
