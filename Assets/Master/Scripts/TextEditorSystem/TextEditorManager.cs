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

    private Button _boldBtn;
    private Button _italicBtn;
    private Button _leftBtn;
    private Button _centerBtn;
    private Button _rightBtn;
    private DropdownField _sizeDropdown;
    private FormatDataLoader _taskController;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        _documentPage = root.Q<VisualElement>("Document");
        _documentPage.Clear();

        CacheUIReferences(root);
        SetupRibbon();
        SetupHeader();

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
        _boldBtn = root.Q<Button>("Bold");
        _italicBtn = root.Q<Button>("Italic");
        _leftBtn = root.Q<Button>("Left");
        _centerBtn = root.Q<Button>("Center");
        _rightBtn = root.Q<Button>("Right");
        _sizeDropdown = root.Q<DropdownField>("Size");

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

    private void SetupHeader()
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
        if (_leftBtn != null) _leftBtn.clicked += () => ApplyAlignment(TextAnchor.UpperLeft);
        if (_centerBtn != null) _centerBtn.clicked += () => ApplyAlignment(TextAnchor.UpperCenter);
        if (_rightBtn != null) _rightBtn.clicked += () => ApplyAlignment(TextAnchor.UpperRight);
        
        if (_boldBtn != null) _boldBtn.clicked += ToggleBold;
        if (_italicBtn != null) _italicBtn.clicked += ToggleItalic;

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

        SetButtonActiveState(_boldBtn, isBold);
        SetButtonActiveState(_italicBtn, isItalic);

        TextAnchor align = currentStyle.unityTextAlign;
        SetButtonActiveState(_leftBtn, align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft);
        SetButtonActiveState(_centerBtn, align == TextAnchor.UpperCenter || align == TextAnchor.MiddleCenter || align == TextAnchor.LowerCenter);
        SetButtonActiveState(_rightBtn, align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight);
    }

    private void SetButtonActiveState(Button btn, bool isActive)
    {
        if (btn == null) return;
        btn.EnableInClassList("ribbon-btn-active", isActive);
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
            currentBlock.value = currentText.Substring(0, cursorPos);
            
            TextField newBlock = CreateBlock(currentIndex + 1, currentText.Substring(cursorPos));
            
            CopyBlockStyles(currentBlock, newBlock);
        }
        else if (evt.keyCode == KeyCode.Backspace && currentBlock.cursorIndex == 0)
        {
            int currentIndex = _documentPage.IndexOf(currentBlock);
            
            if (currentIndex > 0)
            {
                evt.StopPropagation();
                evt.PreventDefault();

                var prevBlock = _documentPage.ElementAt(currentIndex - 1) as TextField;
                int prevTextLength = prevBlock.value.Length;
                prevBlock.value += currentBlock.value;
                _documentPage.Remove(currentBlock);
                
                prevBlock.schedule.Execute(() =>
                {
                    prevBlock.Focus();
                    prevBlock.SelectRange(prevTextLength, prevTextLength);
                });
            }
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
            
            // Apply font size directly to the outer TextField style to match our dropdown logic
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

    private void ToggleBold()
    {
        var input = GetInnerInput(_activeBlock);
        if (input != null)
        {
            var current = input.style.unityFontStyleAndWeight.value;
            bool isBold = current == FontStyle.Bold || current == FontStyle.BoldAndItalic;

            if (isBold)
                input.style.unityFontStyleAndWeight = (current == FontStyle.BoldAndItalic) ? FontStyle.Italic : FontStyle.Normal;
            else
                input.style.unityFontStyleAndWeight = (current == FontStyle.Italic) ? FontStyle.BoldAndItalic : FontStyle.Bold;
            
            UpdateRibbonState();
            RestoreFocusAndCursor();
        }
    }

    private void ToggleItalic()
    {
        var input = GetInnerInput(_activeBlock);
        if (input != null)
        {
            var current = input.style.unityFontStyleAndWeight.value;
            bool isItalic = current == FontStyle.Italic || current == FontStyle.BoldAndItalic;

            if (isItalic)
                input.style.unityFontStyleAndWeight = (current == FontStyle.BoldAndItalic) ? FontStyle.Bold : FontStyle.Normal;
            else
                input.style.unityFontStyleAndWeight = (current == FontStyle.Bold) ? FontStyle.BoldAndItalic : FontStyle.Italic;
            
            UpdateRibbonState();
            RestoreFocusAndCursor();
        }
    }
}