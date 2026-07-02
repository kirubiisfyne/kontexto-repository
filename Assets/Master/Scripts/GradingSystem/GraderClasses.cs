using System.Collections.Generic;
using UnityEngine;

namespace Master.Scripts.GradingSystem
{
    [System.Serializable]
    public class StartingTextBlock
    {
        public string text;
        public string styleClass; // e.g. "Title", "Heading 1", etc.
        public int? fontSize;
        public bool? isBold;
        public bool? isItalic;
        public TextAnchor? alignment;
    }

    [System.Serializable]
    public class DocumentData
    {
        public List<StartingTextBlock> startingTextBlocks; 
        public List<Requirement> answerKey;
        public string instructionString;

        public string sender;
        public string subject;
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
