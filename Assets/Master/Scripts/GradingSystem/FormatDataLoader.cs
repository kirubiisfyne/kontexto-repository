using Master.Scripts;
using Master.Scripts.GradingSystem;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class FormatDataLoader : MonoBehaviour
{
    // A simple static reference so you can pass data to it from your Main Menu scene if needed
    public static TextAsset PendingLevelJSON; 

    [Header("Dependencies")]
    public TextEditorManager editorManager;

    public GradingManager gradingManager;

    [Header("UI Components")] 
    public GameObject emailPannel;
    public TMPro.TextMeshProUGUI emailText;
    [Space(10)]
    public GameObject feedbackPanel;
    public GameObject feedbackPrefab;
    public VerticalLayoutGroup feedbackContainer;
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI documentTitleText;
    
    [Header("Testing / Default Level")]
    [Tooltip("If you play the UI scene directly, it will load this JSON.")]
    public TextAsset fallbackLevelJSON; 

    private TextAsset currentLevelJson;
    private DocumentData convertedDocumentData;

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentLevel == null)
        {
            Debug.LogWarning("No pending level JSON found. Falling back to default.");
            currentLevelJson = fallbackLevelJSON;
        }
        else
        {
            currentLevelJson = GameManager.Instance.GetNextDocumentData(GameManager.Instance.currentLevel);
        }

        LoadLevelData();
        HandleInstructionEmail(true);
    }

    private void LoadLevelData()
    {
        // 1. Figure out which JSON to load (from another scene, or the fallback)
        TextAsset jsonToLoad = PendingLevelJSON != null ? currentLevelJson : fallbackLevelJSON;

        if (jsonToLoad != null)
        {
            // 2. Parse it
            convertedDocumentData = JsonConvert.DeserializeObject<DocumentData>(jsonToLoad.text);
            Debug.Log("Task Controller: Level Data Loaded.");

            // 3. Send it to the UI
            editorManager.LoadLevel(convertedDocumentData);
        }
        else
        {
            Debug.LogError("No JSON file assigned to the Document Task Controller!");
        }
    }

    // This gets called by your TextEditorManager when the user clicks Print
    public void EvaluatePrintJob(VisualElement documentPage)
    {
        Debug.Log("Task Controller: Sending document to grader...");

        GradeReport result = gradingManager.GradeDocument(documentPage, convertedDocumentData);

        if (result.passedPerfectly)
        {
            Debug.Log($"Score: {result.score}/{result.maxScore} - PERFECT PRINT!");
            // TODO: Play success sound, show success UI, grant XP, etc.
            
            HandleFeedbackUI(true, result);
        }
        else
        {
            Debug.Log($"Score: {result.score}/{result.maxScore} - NEEDS REVISION.");
            if (result.adviserFeedback != null && result.adviserFeedback.Count > 0)
            {
                Debug.Log("Adviser Feedback: " + result.adviserFeedback[0]);
                // TODO: Display result.adviserFeedback[0] in your Adviser dialogue box
            }
        }
    }

#region UI Callbacks
    public void HandleFeedbackUI(bool isActive, GradeReport report)
    {
        foreach (string feedback in report.adviserFeedback)
        {
            GameObject feedbackField = Instantiate(feedbackPrefab, feedbackContainer.transform, true);
            feedbackField.GetComponent<TMPro.TextMeshProUGUI>().text = feedback;
        }
        
        scoreText.text = $"{report.score}/{report.maxScore}";
        documentTitleText.text = convertedDocumentData.startingTextBlocks[0];
        feedbackPanel.SetActive(isActive);
    }

    public void HandleInstructionEmail(bool isActive)
    {
        emailText.text = convertedDocumentData.instructionString;
        emailPannel.SetActive(isActive);
    }
#endregion
}