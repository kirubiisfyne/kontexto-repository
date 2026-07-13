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

    [System.Serializable]
    public class GradingDialogueScript
    {
        public string missingBlock = "Missing '{0}'.";
        public string sizeWrong = "Wrong size for '{0}'. Expected {1}.";
        public string boldMissing = "Missing bold on '{0}'.";
        public string boldExtra = "Extra bold on '{0}'.";
        public string italicMissing = "Missing italic on '{0}'.";
        public string italicExtra = "Extra italic on '{0}'.";
        public string alignmentWrong = "Wrong alignment on '{0}'.";
        public string styleWrong = "Wrong style '{0}' on '{1}'.";
        public string listWrong = "Wrong list on '{0}'.";
        public string spaceBeforeMissing = "Missing space before '{0}'.";
        public string spaceBeforeExtra = "Extra space before '{0}'.";
        public string spaceAfterMissing = "Missing space after '{0}'.";
        public string spaceAfterExtra = "Extra space after '{0}'.";
        public string praise1 = "Good job on '{0}'.";
        public string praise2 = "Good job on '{0}'.";
        public string praise3 = "Good job on '{0}'.";
        public string perfectDocument = "Perfect!";
        public string passedWithErrors = "Passed with errors.";
    }
}
