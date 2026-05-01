using System.Linq;
using Master.Scripts;
using Master.Scripts.GradingSystem;
using UnityEngine;
using UnityEngine.UIElements;

public class TextEditorManager : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _documentPage;
    private TextField _activeBlock;

    private Toggle _boldToggle;
    private Toggle _italicToggle;
    private Button _leftBtn;
    private Button _centerBtn;
    private Button _rightBtn;

    private Toggle _spaceAfterToggle;
    private Toggle _spaceBeforeToggle;
    private DropdownField _fontDropdown; // For future font family selection
    private DropdownField _sizeDropdown;
    private DropdownField _styleDropdown;
    private Button _bulletBtn;
    private Button _numberBtn;
    private FormatDataLoader _taskController;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        _documentPage = root.Q<VisualElement>("Document");
        _documentPage.Clear();

        CacheUIReferences(root);
        SetupRibbon();
        SetupTabs();

        _documentPage.RegisterCallback<MouseDownEvent>(evt => 
        {
            if (evt.target == _documentPage)
            {
                evt.PreventDefault();
                if (_activeBlock != null)
                {
                    RestoreFocusAndCursor();
                }
                else if (_documentPage.childCount > 0)
                {
                    var lastBlock = _documentPage.ElementAt(_documentPage.childCount - 1) as TextField;
                    lastBlock?.Focus();
                }
            }
        });

        CreateBlock(0, "");
    }

    private void Start()
    {
        _taskController = FindObjectOfType<FormatDataLoader>();
    }

    private void CacheUIReferences(VisualElement root)
    {
        _boldToggle = root.Q<Toggle>("Bold");
        _italicToggle = root.Q<Toggle>("Italic");
        _leftBtn = root.Q<Button>("Left");
        _centerBtn = root.Q<Button>("Center");
        _rightBtn = root.Q<Button>("Right");
        _fontDropdown = root.Q<DropdownField>("Font");
        _sizeDropdown = root.Q<DropdownField>("Size");
        _styleDropdown = root.Q<DropdownField>("Styles");
        _bulletBtn = root.Q<Button>("Bullet");
        _numberBtn = root.Q<Button>("Numbering");
        _spaceBeforeToggle = root.Q<Toggle>("SpaceBefore");
        _spaceAfterToggle = root.Q<Toggle>("SpaceAfter");
        var ribbonButtons = root.Query<Button>().ToList();
        foreach (var btn in ribbonButtons)
        {
            btn.focusable = false;
        }
    }

    public void LoadLevel(DocumentData currentDocument)
    {
        _documentPage.Clear();
        _activeBlock = null;

        if (currentDocument.startingTextBlocks != null && currentDocument.startingTextBlocks.Count > 0)
        {
            for (int i = 0; i < currentDocument.startingTextBlocks.Count; i++)
            {
                CreateBlock(i, currentDocument.startingTextBlocks[i]);
            }
        }
        else
        {
            CreateBlock(0, ""); 
        }

        if (_documentPage.childCount > 0)
        {
            var firstBlock = _documentPage.ElementAt(0) as TextField;
            firstBlock?.Focus();
            _activeBlock = firstBlock;
            UpdateRibbonState();
        }
    }

    private void SetupTabs()
    {
        var root = _doc.rootVisualElement;
        root.Q<Button>("Exit").clicked += () => SceneGateManager.Instance.StartWarp(SceneGateManager.Instance.lastSceneString);
        
        Button printButton = root.Q<Button>("Print"); 
        if (printButton != null)
        {
            printButton.clicked += () => 
            {
                if (_taskController != null)
                {
                    _taskController.EvaluatePrintJob(_documentPage);
                }
                else
                {
                    Debug.LogError("FormatDataLoader reference is missing!");
                }
            };
        }
    }

    private void SetupRibbon()
    {
        if (_bulletBtn != null) _bulletBtn.clicked += ToggleBullet;
        if (_numberBtn != null) _numberBtn.clicked += ToggleNumber;

        if (_leftBtn != null) _leftBtn.clicked += () => ApplyAlignment(TextAnchor.UpperLeft);
        if (_centerBtn != null) _centerBtn.clicked += () => ApplyAlignment(TextAnchor.UpperCenter);
        if (_rightBtn != null) _rightBtn.clicked += () => ApplyAlignment(TextAnchor.UpperRight);

        _spaceBeforeToggle.RegisterValueChangedCallback(evt => ApplySpacing(evt.newValue, true));
        _spaceAfterToggle.RegisterValueChangedCallback(evt => ApplySpacing(evt.newValue, false));
        
        // Toggles use ValueChanged callbacks in UI Toolkit
        if (_boldToggle != null) 
            _boldToggle.RegisterValueChangedCallback(evt => ToggleBold(evt.newValue));
            
        if (_italicToggle != null) 
            _italicToggle.RegisterValueChangedCallback(evt => ToggleItalic(evt.newValue));

        if (_sizeDropdown != null)
        {
            _sizeDropdown.RegisterValueChangedCallback(evt => 
            {
                if (_activeBlock != null && int.TryParse(evt.newValue, out int newSize))
                {
                    _activeBlock.style.fontSize = newSize;
                    RestoreFocusAndCursor();
                }
            });
        }

        if (_styleDropdown != null)
        {
            _styleDropdown.RegisterValueChangedCallback(evt => ApplyStyle(evt.newValue));
        }
    }
    
    private void UpdateRibbonState()
    {
        if (_activeBlock == null) return;
        
        var input = GetInnerInput(_activeBlock);
        if (input == null) return;

        var currentStyle = input.resolvedStyle;

        if (_sizeDropdown != null)
        {
            int currentSize = Mathf.RoundToInt(currentStyle.fontSize);
            _sizeDropdown.SetValueWithoutNotify(currentSize.ToString());
        }

        bool isBold = currentStyle.unityFontStyleAndWeight == FontStyle.Bold || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;
        bool isItalic = currentStyle.unityFontStyleAndWeight == FontStyle.Italic || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        // Update the actual toggle values without firing their change events
        if (_boldToggle != null) _boldToggle.SetValueWithoutNotify(isBold);
        if (_italicToggle != null) _italicToggle.SetValueWithoutNotify(isItalic);

        SetButtonActiveState(_boldToggle, isBold);
        SetButtonActiveState(_italicToggle, isItalic);

        TextAnchor align = currentStyle.unityTextAlign;
        SetButtonActiveState(_leftBtn, align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft);
        SetButtonActiveState(_centerBtn, align == TextAnchor.UpperCenter || align == TextAnchor.MiddleCenter || align == TextAnchor.LowerCenter);
        SetButtonActiveState(_rightBtn, align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight);

        if (_styleDropdown != null)
        {
            if (_activeBlock.ClassListContains("format-title")) _styleDropdown.SetValueWithoutNotify("Title");
            else if (_activeBlock.ClassListContains("format-subtitle")) _styleDropdown.SetValueWithoutNotify("Subtitle");
            else if (_activeBlock.ClassListContains("format-h1")) _styleDropdown.SetValueWithoutNotify("Heading 1");
            else if (_activeBlock.ClassListContains("format-h2")) _styleDropdown.SetValueWithoutNotify("Heading 2");
            else if (_activeBlock.ClassListContains("format-h3")) _styleDropdown.SetValueWithoutNotify("Heading 3");
            else if (_activeBlock.ClassListContains("format-h4")) _styleDropdown.SetValueWithoutNotify("Heading 4");
            else if (_activeBlock.ClassListContains("format-h5")) _styleDropdown.SetValueWithoutNotify("Heading 5");
            else _styleDropdown.SetValueWithoutNotify("Normal Text");
        }

        if (_activeBlock.style.marginTop == 12) _spaceBeforeToggle.AddToClassList("space-active");
        else _spaceBeforeToggle.RemoveFromClassList("space-active");

        if (_activeBlock.style.marginBottom == 12) _spaceAfterToggle.AddToClassList("space-active");
        else _spaceAfterToggle.RemoveFromClassList("space-active");
    }

    // Changed from Toggle to VisualElement so Buttons and Toggles can both use this
    private void SetButtonActiveState(VisualElement btn, bool isActive)
    {
        if (btn == null) return;
        btn.EnableInClassList("editor-btn--tool--active", isActive);
    }

    private TextField CreateBlock(int index, string initialText)
    {
        TextField newBlock = new TextField();
        newBlock.multiline = true;
        newBlock.value = initialText;
        newBlock.AddToClassList("editor-block");

        newBlock.RegisterCallback<FocusInEvent>(evt => 
        {
            _activeBlock = newBlock;
            newBlock.schedule.Execute(UpdateRibbonState);
        });

        newBlock.RegisterCallback<KeyDownEvent, TextField>(OnKeyDown, newBlock, TrickleDown.TrickleDown);

        _documentPage.Insert(index, newBlock);

        newBlock.schedule.Execute(() =>
        {
            newBlock.Focus();
            newBlock.SelectRange(0, 0); 
        });

        return newBlock;
    }

    private void OnKeyDown(KeyDownEvent evt, TextField currentBlock)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            evt.StopPropagation(); 
            evt.PreventDefault();  

            int currentIndex = _documentPage.IndexOf(currentBlock);
            int cursorPos = Mathf.Max(0, currentBlock.cursorIndex);
            string currentText = currentBlock.value;
            
            // --- NEW LIST LOGIC ---
            string nextPrefix = "";
            if (currentText.StartsWith("• ")) 
            {
                nextPrefix = "• ";
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(currentText, @"^\d+\.\s")) 
            {
                var match = System.Text.RegularExpressions.Regex.Match(currentText, @"^(\d+)\.\s");
                int nextNum = int.Parse(match.Groups[1].Value) + 1;
                nextPrefix = $"{nextNum}. ";
            }
            // ----------------------

            currentBlock.value = currentText.Substring(0, cursorPos);
            
            // Inject the prefix into the new block
            TextField newBlock = CreateBlock(currentIndex + 1, nextPrefix + currentText.Substring(cursorPos));
            CopyBlockStyles(currentBlock, newBlock);
            
            // Keep the indent if it's a list
            if (nextPrefix != "") newBlock.style.paddingLeft = 20;
        }
    }

    private void CopyBlockStyles(TextField source, TextField destination)
    {
        var sourceInput = GetInnerInput(source);
        var destInput = GetInnerInput(destination);

        if (sourceInput != null && destInput != null)
        {
            var sourceStyle = sourceInput.resolvedStyle;

            destInput.style.unityTextAlign = sourceStyle.unityTextAlign;
            destInput.style.unityFontStyleAndWeight = sourceStyle.unityFontStyleAndWeight;
            
            destination.style.fontSize = sourceStyle.fontSize;
        }
    }
     
    private VisualElement GetInnerInput(TextField block)
    {
        return block?.Q(className: "unity-text-field__input");
    }
    
    private void RestoreFocusAndCursor()
    {
        if (_activeBlock == null) return;

        int cursorLoc = _activeBlock.cursorIndex;
        int selectLoc = _activeBlock.selectIndex;

        _activeBlock.schedule.Execute(() =>
        {
            _activeBlock.Focus();
            _activeBlock.SelectRange(cursorLoc, selectLoc);
        });
    }

    private void ApplyAlignment(TextAnchor align)
    {
        var input = GetInnerInput(_activeBlock);
        if (input != null)
        {
            input.style.unityTextAlign = align;
            UpdateRibbonState();
            RestoreFocusAndCursor();
        }
    }

    // Now accepts a bool from the Toggle's ValueChanged event
    private void ToggleBold(bool applyBold)
    {
        var input = GetInnerInput(_activeBlock);
        if (input != null)
        {
            var current = input.style.unityFontStyleAndWeight.value;
            bool isItalic = current == FontStyle.Italic || current == FontStyle.BoldAndItalic;

            if (applyBold)
                input.style.unityFontStyleAndWeight = isItalic ? FontStyle.BoldAndItalic : FontStyle.Bold;
            else
                input.style.unityFontStyleAndWeight = isItalic ? FontStyle.Italic : FontStyle.Normal;
            
            UpdateRibbonState();
            RestoreFocusAndCursor();
        }
    }

    // Now accepts a bool from the Toggle's ValueChanged event
    private void ToggleItalic(bool applyItalic)
    {
        var input = GetInnerInput(_activeBlock);
        if (input != null)
        {
            var current = input.style.unityFontStyleAndWeight.value;
            bool isBold = current == FontStyle.Bold || current == FontStyle.BoldAndItalic;

            if (applyItalic)
                input.style.unityFontStyleAndWeight = isBold ? FontStyle.BoldAndItalic : FontStyle.Italic;
            else
                input.style.unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal;
            
            UpdateRibbonState();
            RestoreFocusAndCursor();
        }
    }

    private void ToggleBullet()
    {
        if (_activeBlock == null) return;
        bool isAlreadyBullet = _activeBlock.value.StartsWith("• ");
        ApplyListFormat(!isAlreadyBullet, false);
    }

    private void ToggleNumber()
    {
        if (_activeBlock == null) return;
        bool isAlreadyNumber = System.Text.RegularExpressions.Regex.IsMatch(_activeBlock.value, @"^\d+\.\s");
        ApplyListFormat(!isAlreadyNumber, true);
    }

    private void ApplyListFormat(bool apply, bool isNumbered)
    {
        if (_activeBlock == null) return;

        if (apply)
        {
            _activeBlock.style.paddingLeft = 20; // Add indent

            string cleanText = System.Text.RegularExpressions.Regex.Replace(_activeBlock.value, @"^(• |\d+\.\s)", "");
            
            string prefix = isNumbered ? "1. " : "• ";
            _activeBlock.value = prefix + cleanText;
            
            // If it's not already formatted, inject the prefix
            if (!_activeBlock.value.StartsWith("•") && !System.Text.RegularExpressions.Regex.IsMatch(_activeBlock.value, @"^\d+\.\s"))
            {
                _activeBlock.value = prefix + _activeBlock.value;
            }
        }
        else
        {
            // Turn it off: Remove indent and strip the prefix
            _activeBlock.style.paddingLeft = 0; 
            _activeBlock.value = System.Text.RegularExpressions.Regex.Replace(_activeBlock.value, @"^(• |\d+\.\s)", "");
        }
        
        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ApplyStyle(string styleName)
    {
        if (_activeBlock == null) return;

        // 1. Scrub ALL possible style classes first
        string[] allStyles = { "format-normal", "format-title", "format-subtitle", 
                               "format-h1", "format-h2", "format-h3", "format-h4", "format-h5" };
        
        foreach (var style in allStyles) 
            _activeBlock.RemoveFromClassList(style);

        // 2. Apply the specific class based on selection
        switch (styleName)
        {
            case "Title": _activeBlock.AddToClassList("format-title"); break;
            case "Subtitle": _activeBlock.AddToClassList("format-subtitle"); break;
            case "Heading 1": _activeBlock.AddToClassList("format-h1"); break;
            case "Heading 2": _activeBlock.AddToClassList("format-h2"); break;
            case "Heading 3": _activeBlock.AddToClassList("format-h3"); break;
            case "Heading 4": _activeBlock.AddToClassList("format-h4"); break;
            case "Heading 5": _activeBlock.AddToClassList("format-h5"); break;
            default: _activeBlock.AddToClassList("format-normal"); break;
        }

        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ApplySpacing(bool addSpace, bool isTop)
    {
        if (_activeBlock == null) return;

        float amount = addSpace ? 12 : 0;

        if (isTop) _activeBlock.style.marginTop = amount;
        else _activeBlock.style.marginBottom = amount;

        RestoreFocusAndCursor();
    }
}