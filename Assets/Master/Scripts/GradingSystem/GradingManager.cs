using System.Collections.Generic;
using System.Linq;
using Master.Scripts.GradingSystem;
using UnityEngine;
using UnityEngine.UIElements;

public class GradeReport
{
    public int score;
    public int maxScore;
    public bool passedPerfectly;
    public bool passedLevel;
    public List<string> adviserFeedback = new List<string>();
    public List<string> pennyFeedback = new List<string>();
}

public class GradingManager : MonoBehaviour
{
    [Range(0f, 1f)]
    public float passingGradeThreshold = 0.5f;

    [Header("Dialogue Scripts (JSON)")]
    public TextAsset pennyScriptAsset;
    public TextAsset adviserScriptAsset;

    public GradingDialogueScript PennyScript => pennyScript;

    private GradingDialogueScript pennyScript;
    private GradingDialogueScript adviserScript;

    private void Awake()
    {
#if UNITY_EDITOR
        if (pennyScriptAsset == null) pennyScriptAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Master/Data/Dialogue/penny_grading_dialogue.json");
        if (adviserScriptAsset == null) adviserScriptAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Master/Data/Dialogue/adviser_grading_dialogue.json");
#endif
        pennyScript = pennyScriptAsset != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<GradingDialogueScript>(pennyScriptAsset.text) : new GradingDialogueScript();
        adviserScript = adviserScriptAsset != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<GradingDialogueScript>(adviserScriptAsset.text) : new GradingDialogueScript();
    }

    public GradeReport GradeDocument(VisualElement documentPage, DocumentData currentDocument)
    {
        GradeReport report = new GradeReport();
        report.score = 0;
        report.maxScore = 0;

        List<string> pPraises = new List<string>();
        List<string> aPraises = new List<string>();

        // 1. Get all player blocks
        var allPlayerBlocks = documentPage.Query<TextField>().ToList();

        // 2. Loop through Answer Key
        foreach (var req in currentDocument.answerKey)
        {
            if (string.IsNullOrEmpty(req.targetTextSnippet))
            {
                Debug.LogWarning("GradingManager: Skipping a requirement because targetTextSnippet is null or empty. Please check the JSON.");
                continue;
            }

            string searchTarget = req.targetTextSnippet.Trim().ToLower();
            TextField matchedBlock = allPlayerBlocks.FirstOrDefault(b => b.value.Trim().ToLower().Contains(searchTarget));

            if (matchedBlock == null)
            {
                report.pennyFeedback.Add(string.Format(pennyScript.missingBlock, req.targetTextSnippet));
                report.adviserFeedback.Add(string.Format(adviserScript.missingBlock, req.targetTextSnippet));
                // Penalize heavily for missing a required block entirely (e.g., 4 points)
                report.maxScore += 4; 
                continue;
            }

            bool blockPerfect = true;
            bool anyCheckRan = false; // Prevents praising a block that had no requirements

            // --- 3. THE OPT-IN DISPATCHER ---
            // Only run the checks that the JSON explicitly asks for!

            if (req.requiredSize.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckSize(matchedBlock, req, report); }

            if (req.requireBold.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckBold(matchedBlock, req, report); }

            if (req.requireItalic.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckItalic(matchedBlock, req, report); }

            if (req.requiredAlignment.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckAlignment(matchedBlock, req, report); }

            if (!string.IsNullOrEmpty(req.requiredStyle)) 
            { anyCheckRan = true; blockPerfect &= CheckStyle(matchedBlock, req, report); }

            if (req.requiredListType.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckList(matchedBlock, req, report); }

            if (req.requireSpaceBefore.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckSpaceBefore(matchedBlock, req, report); }

            if (req.requireSpaceAfter.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckSpaceAfter(matchedBlock, req, report); }

            // 4. Positive Reinforcement
            if (anyCheckRan && blockPerfect)
            {
                string[] aPraisesArr = { string.Format(adviserScript.praise1, req.targetTextSnippet), string.Format(adviserScript.praise2, req.targetTextSnippet), string.Format(adviserScript.praise3, req.targetTextSnippet) };
                
                aPraises.Add(aPraisesArr[Random.Range(0, aPraisesArr.Length)]);
            }
        }

        // Add praises at the end so errors are always first
        report.pennyFeedback.AddRange(pPraises);
        report.adviserFeedback.AddRange(aPraises);

        // 5. Finalize
        report.passedPerfectly = (report.score == report.maxScore) && (report.maxScore > 0);
        report.passedLevel = (report.maxScore == 0) || ((float)report.score / report.maxScore >= passingGradeThreshold);
        
        if (report.passedPerfectly)
        {
            report.pennyFeedback.Insert(0, pennyScript.perfectDocument);
            report.adviserFeedback.Insert(0, adviserScript.perfectDocument);
        }
        else if (report.passedLevel)
        {
            report.pennyFeedback.Insert(0, pennyScript.passedWithErrors);
            report.adviserFeedback.Insert(0, adviserScript.passedWithErrors);
        }

        return report;
    }

    // ==========================================
    // ISOLATED CHECKER FUNCTIONS
    // ==========================================

    private bool CheckSize(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        
        if (Mathf.RoundToInt(style.fontSize) == req.requiredSize.Value) 
        { report.score++; return true; }
        
        report.pennyFeedback.Add(string.Format(pennyScript.sizeWrong, req.targetTextSnippet, req.requiredSize.Value));
        report.adviserFeedback.Add(string.Format(adviserScript.sizeWrong, req.targetTextSnippet, req.requiredSize.Value));
        return false;
    }

    private bool CheckBold(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        bool isBold = style.unityFontStyleAndWeight == FontStyle.Bold || style.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        if (isBold == req.requireBold.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireBold.Value ? string.Format(pennyScript.boldMissing, req.targetTextSnippet) : string.Format(pennyScript.boldExtra, req.targetTextSnippet));
        report.adviserFeedback.Add(req.requireBold.Value ? string.Format(adviserScript.boldMissing, req.targetTextSnippet) : string.Format(adviserScript.boldExtra, req.targetTextSnippet));
        return false;
    }

    private bool CheckItalic(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        bool isItalic = style.unityFontStyleAndWeight == FontStyle.Italic || style.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        if (isItalic == req.requireItalic.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireItalic.Value ? string.Format(pennyScript.italicMissing, req.targetTextSnippet) : string.Format(pennyScript.italicExtra, req.targetTextSnippet));
        report.adviserFeedback.Add(req.requireItalic.Value ? string.Format(adviserScript.italicMissing, req.targetTextSnippet) : string.Format(adviserScript.italicExtra, req.targetTextSnippet));
        return false;
    }

    private bool CheckAlignment(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;

        if (style.unityTextAlign == req.requiredAlignment.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(string.Format(pennyScript.alignmentWrong, req.targetTextSnippet));
        report.adviserFeedback.Add(string.Format(adviserScript.alignmentWrong, req.targetTextSnippet));
        return false;
    }

    private bool CheckStyle(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;

        if (block.ClassListContains(req.requiredStyle)) 
        { report.score++; return true; }

        report.pennyFeedback.Add(string.Format(pennyScript.styleWrong, req.requiredStyle, req.targetTextSnippet));
        report.adviserFeedback.Add(string.Format(adviserScript.styleWrong, req.requiredStyle, req.targetTextSnippet));
        return false;
    }

    private bool CheckList(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        int currentListType = 0;

        if (block.style.paddingLeft.value.value > 0) // It has an indent
        {
            if (block.value.StartsWith("• ")) currentListType = 1;
            else if (System.Text.RegularExpressions.Regex.IsMatch(block.value, @"^\d+\.\s")) currentListType = 2;
        }

        if (currentListType == req.requiredListType.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(string.Format(pennyScript.listWrong, req.targetTextSnippet));
        report.adviserFeedback.Add(string.Format(adviserScript.listWrong, req.targetTextSnippet));
        return false;
    }

    private bool CheckSpaceBefore(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        bool hasSpace = block.style.marginTop.value.value > 0.1f;

        if (hasSpace == req.requireSpaceBefore.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireSpaceBefore.Value ? string.Format(pennyScript.spaceBeforeMissing, req.targetTextSnippet) : string.Format(pennyScript.spaceBeforeExtra, req.targetTextSnippet));
        report.adviserFeedback.Add(req.requireSpaceBefore.Value ? string.Format(adviserScript.spaceBeforeMissing, req.targetTextSnippet) : string.Format(adviserScript.spaceBeforeExtra, req.targetTextSnippet));
        return false;
    }

    private bool CheckSpaceAfter(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        bool hasSpace = block.style.marginBottom.value.value > 0.1f;

        if (hasSpace == req.requireSpaceAfter.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireSpaceAfter.Value ? string.Format(pennyScript.spaceAfterMissing, req.targetTextSnippet) : string.Format(pennyScript.spaceAfterExtra, req.targetTextSnippet));
        report.adviserFeedback.Add(req.requireSpaceAfter.Value ? string.Format(adviserScript.spaceAfterMissing, req.targetTextSnippet) : string.Format(adviserScript.spaceAfterExtra, req.targetTextSnippet));
        return false;
    }
}