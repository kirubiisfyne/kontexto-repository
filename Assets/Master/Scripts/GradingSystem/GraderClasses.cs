using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.GradingSystem
{
    [System.Serializable]
    public class DocumentData
    {
        public List<string> startingTextBlocks; 
        public List<Requirement> answerKey;
        public string instructionString;
    }

    [System.Serializable]
    public class Requirement
    {
        public string targetTextSnippet; 
        public string requiredStyle; 

        public int? requiredSize;
        public int? requiredListType;
        
        public bool? requireBold;
        public bool? requireItalic;
        public bool? requireSpaceBefore;
        public bool? requireSpaceAfter;

        public TextAnchor? requiredAlignment;
    }
}
