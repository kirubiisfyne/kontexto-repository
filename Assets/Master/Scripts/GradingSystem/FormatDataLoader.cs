using Master.Scripts;
using Master.Scripts.GradingSystem;
using Master.Scripts.TextEditorSystem;
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

    [Header("Penny Assistant")]
    [FormerlySerializedAs("clippyAssistant")]
    public PennyAssistantController pennyAssistant;

    [Header("Warping")]
    [Tooltip("The name of the Campus scene to warp back to when passed")]
    public string campusSceneName = "Campus";


    [Header("UI Components")] 
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
    private VisualElement _emailRoot;
    private Label _emailTextBox;
    
    private Label _senderLabel;
    private Label _subjectLabel;


    private void Start()
    {
        // Initialize UI Toolkit Elements for Email Panel
        var root = editorManager.GetComponent<UIDocument>().rootVisualElement;
        _emailRoot = root.Q<VisualElement>("EmailRoot");
        _emailTextBox = root.Q<Label>("EmailTextBox");
        
        _senderLabel = root.Q<Label>("SenderLabel");
        _subjectLabel = root.Q<Label>("SubjectLabel");


        // Hook up the close button
        var closeButton = _emailRoot?.Q<UnityEngine.UIElements.Button>("Exit");
        if (closeButton != null)
        {
            closeButton.clicked += () => HandleInstructionEmail(false);
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("No GameManager instance found. Falling back to default.");
            currentLevelJson = fallbackLevelJSON;
        }
        else
        {
            currentLevelJson = GameManager.Instance.currentLevelData != null ? GameManager.Instance.currentLevelData.documentData : null;
        }

        LoadLevelData();
        HandleInstructionEmail(true);
    }

    private void LoadLevelData()
    {
        // 1. Figure out which JSON to load (from another scene, or the fallback)
        TextAsset jsonToLoad = PendingLevelJSON != null ? PendingLevelJSON : (currentLevelJson != null ? currentLevelJson : fallbackLevelJSON);

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

        if (result.passedLevel)
        {
            // Signal success for the Computer in the Campus scene
            if (GameManager.Instance != null) 
            {
                GameManager.Instance.pendingDocumentSuccess = true;
            }
            
            // Store dynamic lines for the Campus scene to pick up
            if (GameManager.Instance != null)
            {
                GameManager.Instance.pendingAdviserFeedback = new System.Collections.Generic.List<string>(result.adviserFeedback);
            }
            
            // Show Penny and wait for dismissal to warp back
            if (pennyAssistant != null) 
            {
                pennyAssistant.onFeedbackDismissed = () => { StartCoroutine(WarpBackRoutine()); };
                string passMsg = (result.pennyFeedback != null && result.pennyFeedback.Count > 0) ? result.pennyFeedback[0] : "You passed!";
                pennyAssistant.ShowFeedback(passMsg, true);
            }
            else
            {
                StartCoroutine(WarpBackRoutine());
            }
        }
        else
        {
            Debug.Log($"Score: {result.score}/{result.maxScore} - FAILED. NEEDS REVISION.");
            if (result.pennyFeedback != null && result.pennyFeedback.Count > 0)
            {
                Debug.Log("Penny Feedback: " + result.pennyFeedback[0]);
                if (pennyAssistant != null) pennyAssistant.ShowFeedback(result.pennyFeedback[0], false);
            }
        }
    }

    private System.Collections.IEnumerator WarpBackRoutine()
    {
        if (Master.Scripts.TransitionManager.Instance != null)
        {
            yield return Master.Scripts.TransitionManager.Instance.PlayTransitionAndWait("transition");
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(campusSceneName);
    }

#region UI Callbacks
    public void HandleFeedbackUI(bool isActive, GradeReport report)
    {
        foreach (string feedback in report.pennyFeedback)
        {
            GameObject feedbackField = Instantiate(feedbackPrefab, feedbackContainer.transform, true);
            feedbackField.GetComponent<TMPro.TextMeshProUGUI>().text = feedback;
        }
        
        scoreText.text = $"{report.score}/{report.maxScore}";
        documentTitleText.text = convertedDocumentData.startingTextBlocks[0].text;
        feedbackPanel.SetActive(isActive);
    }

    public void HandleInstructionEmail(bool isActive)
    {
        if (_emailRoot != null && _emailTextBox != null)
        {
            _emailTextBox.text = convertedDocumentData.instructionString;
            
            if (_senderLabel != null)
                _senderLabel.text = string.IsNullOrEmpty(convertedDocumentData.sender) ? "Unknown" : convertedDocumentData.sender;
            if (_subjectLabel != null)
                _subjectLabel.text = string.IsNullOrEmpty(convertedDocumentData.subject) ? "Unknown" : convertedDocumentData.subject;
            
            _emailRoot.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }


#endregion
}