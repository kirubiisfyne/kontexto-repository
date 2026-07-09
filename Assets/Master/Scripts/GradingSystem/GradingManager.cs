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
}

public class GradingManager : MonoBehaviour
{
    [Range(0f, 1f)]
    public float passingGradeThreshold = 0.5f;
    public GradeReport GradeDocument(VisualElement documentPage, DocumentData currentDocument)
    {
        GradeReport report = new GradeReport();
        report.score = 0;
        report.maxScore = 0;

        // 1. Get all player blocks
        var allPlayerBlocks = documentPage.Query<TextField>().ToList();

        // 2. Loop through Answer Key
        foreach (var req in currentDocument.answerKey)
        {
            string searchTarget = req.targetTextSnippet.Trim().ToLower();
            TextField matchedBlock = allPlayerBlocks.FirstOrDefault(b => b.value.Trim().ToLower().Contains(searchTarget));

            if (matchedBlock == null)
            {
                report.adviserFeedback.Add($"Oh no, '{req.targetTextSnippet}' is missing!");
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
                string[] praises = {
                    $"Awesome job on the '{req.targetTextSnippet}' section!",
                    $"'{req.targetTextSnippet}' looks absolutely perfect!",
                    $"Spot on with the '{req.targetTextSnippet}' formatting!"
                };
                report.adviserFeedback.Add(praises[Random.Range(0, praises.Length)]);
            }
        }

        // 5. Finalize
        report.passedPerfectly = (report.score == report.maxScore) && (report.maxScore > 0);
        report.passedLevel = (report.maxScore == 0) || ((float)report.score / report.maxScore >= passingGradeThreshold);
        if (report.passedPerfectly)
        {
            report.adviserFeedback.Add("Perfect formatting! You nailed the whole document.");
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
        
        report.adviserFeedback.Add($"Let's resize '{req.targetTextSnippet}' to {req.requiredSize.Value}!");
        return false;
    }

    private bool CheckBold(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        bool isBold = style.unityFontStyleAndWeight == FontStyle.Bold || style.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        if (isBold == req.requireBold.Value) 
        { report.score++; return true; }

        report.adviserFeedback.Add(req.requireBold.Value ? $"Try bolding '{req.targetTextSnippet}'!" : $"Let's un-bold '{req.targetTextSnippet}'.");
        return false;
    }

    private bool CheckItalic(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        bool isItalic = style.unityFontStyleAndWeight == FontStyle.Italic || style.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        if (isItalic == req.requireItalic.Value) 
        { report.score++; return true; }

        report.adviserFeedback.Add(req.requireItalic.Value ? $"Try italicizing '{req.targetTextSnippet}'!" : $"Let's un-italicize '{req.targetTextSnippet}'.");
        return false;
    }

    private bool CheckAlignment(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;

        if (style.unityTextAlign == req.requiredAlignment.Value) 
        { report.score++; return true; }

        report.adviserFeedback.Add($"Let's fix the alignment for '{req.targetTextSnippet}'!");
        return false;
    }

    private bool CheckStyle(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;

        if (block.ClassListContains(req.requiredStyle)) 
        { report.score++; return true; }

        report.adviserFeedback.Add($"Try the '{req.requiredStyle}' style on '{req.targetTextSnippet}'!");
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

        report.adviserFeedback.Add($"Let's check the bullets or numbers on '{req.targetTextSnippet}'.");
        return false;
    }

    private bool CheckSpaceBefore(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        bool hasSpace = block.style.marginTop.value.value > 0.1f;

        if (hasSpace == req.requireSpaceBefore.Value) 
        { report.score++; return true; }

        report.adviserFeedback.Add(req.requireSpaceBefore.Value ? $"Add a little Space Before '{req.targetTextSnippet}'." : $"Let's remove the Space Before '{req.targetTextSnippet}'.");
        return false;
    }

    private bool CheckSpaceAfter(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        bool hasSpace = block.style.marginBottom.value.value > 0.1f;

        if (hasSpace == req.requireSpaceAfter.Value) 
        { report.score++; return true; }

        report.adviserFeedback.Add(req.requireSpaceAfter.Value ? $"Add a little Space After '{req.targetTextSnippet}'." : $"Let's remove the Space After '{req.targetTextSnippet}'.");
        return false;
    }
}