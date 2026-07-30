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

    [Header("Feedback Settings")]
    public int maxSnippetLength = 25;

    [Header("Dialogue Scripts (JSON)")]
    public TextAsset pennyScriptAsset;
    public TextAsset adviserScriptAsset;

    public GradingDialogueScript PennyScript => pennyScript;

    private GradingDialogueScript pennyScript;
    private GradingDialogueScript adviserScript;
    
    private Master.Scripts.TextEditorSystem.FontMappingsData _fontMappingsData;

    private void Awake()
    {
#if UNITY_EDITOR
        if (pennyScriptAsset == null) pennyScriptAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Master/Data/Dialogue/penny_grading_dialogue.json");
        if (adviserScriptAsset == null) adviserScriptAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Master/Data/Dialogue/adviser_grading_dialogue.json");
#endif
        pennyScript = pennyScriptAsset != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<GradingDialogueScript>(pennyScriptAsset.text) : new GradingDialogueScript();
        adviserScript = adviserScriptAsset != null ? Newtonsoft.Json.JsonConvert.DeserializeObject<GradingDialogueScript>(adviserScriptAsset.text) : new GradingDialogueScript();
        
        TextAsset fontMappingJson = Resources.Load<TextAsset>("FontMappings");
        if (fontMappingJson != null)
        {
            _fontMappingsData = JsonUtility.FromJson<Master.Scripts.TextEditorSystem.FontMappingsData>(fontMappingJson.text);
        }
    }

    private string TruncateSnippet(string snippet)
    {
        if (string.IsNullOrEmpty(snippet)) return snippet;
        return snippet.Length <= maxSnippetLength ? snippet : snippet.Substring(0, maxSnippetLength) + "...";
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
        
        // Precompute lowercased values to avoid O(N*M) string allocations in the loop
        var blockValuesLower = allPlayerBlocks.Select(b => b.value.Trim().ToLower()).ToList();

        // 2. Loop through Answer Key
        foreach (var req in currentDocument.answerKey)
        {
            if (string.IsNullOrEmpty(req.targetTextSnippet))
            {
                //Debug.LogWarning("GradingManager: Skipping a requirement because targetTextSnippet is null or empty. Please check the JSON.");
                continue;
            }

            string searchTarget = req.targetTextSnippet.Trim().ToLower();
            
            TextField matchedBlock = null;
            for (int i = 0; i < allPlayerBlocks.Count; i++)
            {
                if (blockValuesLower[i].Contains(searchTarget))
                {
                    matchedBlock = allPlayerBlocks[i];
                    break;
                }
            }

            if (matchedBlock == null)
            {
                string missingSnippet = TruncateSnippet(req.targetTextSnippet);
                report.pennyFeedback.Add(string.Format(pennyScript.missingBlock, missingSnippet));
                report.adviserFeedback.Add(string.Format(adviserScript.missingBlock, missingSnippet));
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

            if (!string.IsNullOrEmpty(req.requiredFontFace)) 
            { anyCheckRan = true; blockPerfect &= CheckFont(matchedBlock, req, report); }

            if (req.requiredListType.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckList(matchedBlock, req, report); }

            if (req.requireSpaceBefore.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckSpaceBefore(matchedBlock, req, report); }

            if (req.requireSpaceAfter.HasValue) 
            { anyCheckRan = true; blockPerfect &= CheckSpaceAfter(matchedBlock, req, report); }

            // 4. Positive Reinforcement
            if (anyCheckRan && blockPerfect)
            {
                string praisedSnippet = TruncateSnippet(req.targetTextSnippet);
                int r = Random.Range(0, 3);
                if (r == 0) aPraises.Add(string.Format(adviserScript.praise1, praisedSnippet));
                else if (r == 1) aPraises.Add(string.Format(adviserScript.praise2, praisedSnippet));
                else aPraises.Add(string.Format(adviserScript.praise3, praisedSnippet));
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
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        
        if (Mathf.RoundToInt(style.fontSize) == req.requiredSize.Value) 
        { report.score++; return true; }
        
        report.pennyFeedback.Add(string.Format(pennyScript.sizeWrong, displaySnippet, req.requiredSize.Value));
        report.adviserFeedback.Add(string.Format(adviserScript.sizeWrong, displaySnippet, req.requiredSize.Value));
        return false;
    }

    private bool CheckBold(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        bool isBold = style.unityFontStyleAndWeight == FontStyle.Bold || style.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        if (isBold == req.requireBold.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireBold.Value ? string.Format(pennyScript.boldMissing, displaySnippet) : string.Format(pennyScript.boldExtra, displaySnippet));
        report.adviserFeedback.Add(req.requireBold.Value ? string.Format(adviserScript.boldMissing, displaySnippet) : string.Format(adviserScript.boldExtra, displaySnippet));
        return false;
    }

    private bool CheckItalic(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        bool isItalic = style.unityFontStyleAndWeight == FontStyle.Italic || style.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        if (isItalic == req.requireItalic.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireItalic.Value ? string.Format(pennyScript.italicMissing, displaySnippet) : string.Format(pennyScript.italicExtra, displaySnippet));
        report.adviserFeedback.Add(req.requireItalic.Value ? string.Format(adviserScript.italicMissing, displaySnippet) : string.Format(adviserScript.italicExtra, displaySnippet));
        return false;
    }

    private bool CheckAlignment(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;

        if (style.unityTextAlign == req.requiredAlignment.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(string.Format(pennyScript.alignmentWrong, displaySnippet));
        report.adviserFeedback.Add(string.Format(adviserScript.alignmentWrong, displaySnippet));
        return false;
    }

    private bool CheckStyle(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);

        if (block.ClassListContains(req.requiredStyle)) 
        { report.score++; return true; }

        report.pennyFeedback.Add(string.Format(pennyScript.styleWrong, req.requiredStyle, displaySnippet));
        report.adviserFeedback.Add(string.Format(adviserScript.styleWrong, req.requiredStyle, displaySnippet));
        return false;
    }

    private bool CheckFont(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        var style = block.Q(className: "unity-text-field__input").resolvedStyle;
        
        string actualFontName = "";
        if (style.unityFontDefinition.font != null)
        {
            actualFontName = style.unityFontDefinition.font.name;
        }
        else if (style.unityFont != null)
        {
            actualFontName = style.unityFont.name;
        }

        string expectedFontName = req.requiredFontFace;
        if (_fontMappingsData != null && _fontMappingsData.mappings != null)
        {
            foreach (var mapping in _fontMappingsData.mappings)
            {
                if (mapping.displayName == req.requiredFontFace)
                {
                    expectedFontName = mapping.regular;
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(actualFontName) && (actualFontName == expectedFontName || actualFontName.Contains(req.requiredFontFace))) 
        { 
            report.score++; 
            return true; 
        }

        report.pennyFeedback.Add(string.Format(pennyScript.fontWrong, displaySnippet, req.requiredFontFace));
        report.adviserFeedback.Add(string.Format(adviserScript.fontWrong, displaySnippet, req.requiredFontFace));
        return false;
    }

    private bool CheckList(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        int currentListType = 0;

        if (block.style.paddingLeft.value.value > 0) // It has an indent
        {
            if (block.value.StartsWith("• ")) currentListType = 1;
            else if (System.Text.RegularExpressions.Regex.IsMatch(block.value, @"^\d+\.\s")) currentListType = 2;
        }

        if (currentListType == req.requiredListType.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(string.Format(pennyScript.listWrong, displaySnippet));
        report.adviserFeedback.Add(string.Format(adviserScript.listWrong, displaySnippet));
        return false;
    }

    private bool CheckSpaceBefore(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        bool hasSpace = block.style.marginTop.value.value > 0.1f;

        if (hasSpace == req.requireSpaceBefore.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireSpaceBefore.Value ? string.Format(pennyScript.spaceBeforeMissing, displaySnippet) : string.Format(pennyScript.spaceBeforeExtra, displaySnippet));
        report.adviserFeedback.Add(req.requireSpaceBefore.Value ? string.Format(adviserScript.spaceBeforeMissing, displaySnippet) : string.Format(adviserScript.spaceBeforeExtra, displaySnippet));
        return false;
    }

    private bool CheckSpaceAfter(TextField block, Requirement req, GradeReport report)
    {
        report.maxScore++;
        string displaySnippet = TruncateSnippet(req.targetTextSnippet);
        bool hasSpace = block.style.marginBottom.value.value > 0.1f;

        if (hasSpace == req.requireSpaceAfter.Value) 
        { report.score++; return true; }

        report.pennyFeedback.Add(req.requireSpaceAfter.Value ? string.Format(pennyScript.spaceAfterMissing, displaySnippet) : string.Format(pennyScript.spaceAfterExtra, displaySnippet));
        report.adviserFeedback.Add(req.requireSpaceAfter.Value ? string.Format(adviserScript.spaceAfterMissing, displaySnippet) : string.Format(adviserScript.spaceAfterExtra, displaySnippet));
        return false;
    }
}