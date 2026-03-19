using System.Collections.Generic;
using System.Linq;
using Master.Scripts.GradingSystem;
using UnityEngine;
using UnityEngine.UIElements;

// A clean data container to send the results back to your UI/GameManager
public class GradeReport
{
    public int score;
    public int maxScore;
    public bool passedPerfectly;
    public List<string> adviserFeedback = new List<string>();
}

public class GradingManager : MonoBehaviour
{
    // Call this from your GameManager or Submit Button
    public GradeReport GradeDocument(VisualElement documentPage, DocumentData currentDocument)
    {
        GradeReport report = new GradeReport();
        report.maxScore = currentDocument.answerKey.Count * 4; // 4 formatting checks per required block
        report.score = 0;

        // 1. Get all the blocks the player currently has on the page
        var allPlayerBlocks = documentPage.Query<TextField>().ToList();

        // 2. Loop through the Answer Key for this specific level
        foreach (var requirement in currentDocument.answerKey)
        {
            // Find the block that contains the target text (ignoring case and extra spaces)
            string searchTarget = requirement.targetTextSnippet.Trim().ToLower();
            
            TextField matchedBlock = allPlayerBlocks.FirstOrDefault(b => 
                b.value.Trim().ToLower().Contains(searchTarget));

            if (matchedBlock == null)
            {
                report.adviserFeedback.Add($"Oops! You seem to have deleted or heavily altered the section about: '{requirement.targetTextSnippet}'.");
                continue; // Skip grading this block since we can't find it
            }

            // We found the text! Now grab the inner input element where the styles actually live.
            var innerInput = matchedBlock.Q(className: "unity-text-field__input");
            if (innerInput == null) continue;

            var currentStyle = innerInput.resolvedStyle;
            bool blockPerfect = true; // Track if this specific block was done perfectly

            // Check 1: Size
            int currentSize = Mathf.RoundToInt(currentStyle.fontSize);
            if (currentSize == requirement.requiredSize)
            {
                report.score++;
            }
            else
            {
                report.adviserFeedback.Add($"The text '{requirement.targetTextSnippet}' should be size {requirement.requiredSize}.");
                blockPerfect = false;
            }

            // Check 2 & 3: Bold & Italic
            bool isBold = currentStyle.unityFontStyleAndWeight == FontStyle.Bold || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;
            bool isItalic = currentStyle.unityFontStyleAndWeight == FontStyle.Italic || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

            if (isBold == requirement.requireBold) 
            {
                report.score++;
            }
            else 
            {
                report.adviserFeedback.Add(requirement.requireBold ? $"Don't forget to Bold '{requirement.targetTextSnippet}'!" : $"'{requirement.targetTextSnippet}' shouldn't be Bold.");
                blockPerfect = false;
            }

            if (isItalic == requirement.requireItalic) 
            {
                report.score++;
            }
            else 
            {
                report.adviserFeedback.Add(requirement.requireItalic ? $"Don't forget to Italicize '{requirement.targetTextSnippet}'!" : $"'{requirement.targetTextSnippet}' shouldn't be Italicized.");
                blockPerfect = false;
            }

            // Check 4: Alignment
            if (currentStyle.unityTextAlign == requirement.requiredAlignment)
            {
                report.score++;
            }
            else
            {
                report.adviserFeedback.Add($"The alignment is incorrect for '{requirement.targetTextSnippet}'.");
                blockPerfect = false;
            }

            // Optional: Positive reinforcement!
            if (blockPerfect)
            {
                Debug.Log($"Great job on the '{requirement.targetTextSnippet}' section!");
            }
        }

        // 3. Finalize the report
        report.passedPerfectly = (report.score == report.maxScore);
        
        if (report.passedPerfectly)
        {
            report.adviserFeedback.Add("Perfect formatting! You nailed it.");
        }

        return report;
    }
}