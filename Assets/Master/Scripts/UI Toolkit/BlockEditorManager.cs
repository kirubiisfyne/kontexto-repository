using System.Linq;
using Master.Scripts; // Needed for the .ToList() on the buttons
using UnityEngine;
using UnityEngine.UIElements;

public class BlockEditorManager : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _documentPage;
    private TextField _activeBlock;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        _documentPage = root.Q<VisualElement>("Document");
        _documentPage.Clear();

        SetupRibbon(root);
        SetupHeader(root);

        // --- NEW: Handle clicking on the empty white page to keep focus ---
        _documentPage.RegisterCallback<MouseDownEvent>(evt => 
        {
            // Only trigger if they clicked the blank page itself, not a text block
            if (evt.target == _documentPage)
            {
                evt.PreventDefault(); // Stop Unity from clearing focus

                if (_activeBlock != null)
                {
                    RestoreFocusAndCursor();
                }
                else if (_documentPage.childCount > 0)
                {
                    // If no active block, focus the very last block on the page
                    var lastBlock = _documentPage.ElementAt(_documentPage.childCount - 1) as TextField;
                    lastBlock?.Focus();
                }
            }
        });

        // Spawn the first empty block
        CreateBlock(0, "");
    }

    private void SetupHeader(VisualElement root)
    {
        root.Q<Button>("Exit").clicked += () => GateManager.Instance.StartWarp(GateManager.Instance.lastSceneString);
    }

    private void SetupRibbon(VisualElement root)
    {
        // --- ALIGNMENT ---
        root.Q<Button>("Left").clicked += () => ApplyAlignment(TextAnchor.UpperLeft);
        root.Q<Button>("Center").clicked += () => ApplyAlignment(TextAnchor.UpperCenter);
        root.Q<Button>("Right").clicked += () => ApplyAlignment(TextAnchor.UpperRight);
        
        // --- STYLES ---
        root.Q<Button>("Bold").clicked += ToggleBold;
        root.Q<Button>("Italic").clicked += ToggleItalic;
        
        // --- NEW: Stop buttons from stealing the internal UI Toolkit focus ---
        var allButtons = root.Query<Button>().ToList();
        foreach (var btn in allButtons)
        {
            btn.focusable = false;
        }

        // --- SIZE DROPDOWN ---
        var sizeDropdown = root.Q<DropdownField>("Size");
        if (sizeDropdown != null)
        {
            sizeDropdown.RegisterValueChangedCallback(evt => 
            {
                if (_activeBlock != null && int.TryParse(evt.newValue, out int newSize))
                {
                    _activeBlock.style.fontSize = newSize;
                    RestoreFocusAndCursor(); // Updated to preserve caret
                }
            });
        }
    }
    
    private void UpdateRibbonState()
    {
        if (_activeBlock == null) return;
        
        var input = GetInnerInput(_activeBlock);
        if (input == null) return;

        // resolvedStyle gets the final computed styles (CSS + C# overrides)
        var currentStyle = input.resolvedStyle;

        // 1. UPDATE SIZE DROPDOWN
        var sizeDropdown = _doc.rootVisualElement.Q<DropdownField>("Size");
        if (sizeDropdown != null)
        {
            int currentSize = Mathf.RoundToInt(currentStyle.fontSize);
            // SetValueWithoutNotify prevents the dropdown from firing its "OnValueChanged" event
            sizeDropdown.SetValueWithoutNotify(currentSize.ToString());
        }

        // 2. UPDATE BOLD / ITALIC BUTTON VISUALS
        bool isBold = currentStyle.unityFontStyleAndWeight == FontStyle.Bold || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;
        bool isItalic = currentStyle.unityFontStyleAndWeight == FontStyle.Italic || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

        SetButtonActiveState("Bold", isBold);
        SetButtonActiveState("Italic", isItalic);

        // 3. UPDATE ALIGNMENT BUTTON VISUALS
        TextAnchor align = currentStyle.unityTextAlign;
        SetButtonActiveState("Left", align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft);
        SetButtonActiveState("Center", align == TextAnchor.UpperCenter || align == TextAnchor.MiddleCenter || align == TextAnchor.LowerCenter);
        SetButtonActiveState("Right", align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight);
    }

    private void SetButtonActiveState(string buttonName, bool isActive)
    {
        var btn = _doc.rootVisualElement.Q<Button>(buttonName);
        if (btn == null) return;

        // Toggles the USS class on or off based on the isActive boolean
        btn.EnableInClassList("ribbon-btn-active", isActive);
    }

    private TextField CreateBlock(int index, string initialText)
    {
        TextField newBlock = new TextField();
        newBlock.multiline = true;
        newBlock.value = initialText;
        newBlock.AddToClassList("editor-block");

        // Track which block is currently active
        newBlock.RegisterCallback<FocusInEvent>(evt => 
        {
            _activeBlock = newBlock;
            // Wait a frame for resolvedStyle to be accurate if it was just spawned
            newBlock.schedule.Execute(UpdateRibbonState);
        });

        // Intercept Enter and Backspace keys before the TextField processes them natively
        newBlock.RegisterCallback<KeyDownEvent, TextField>(OnKeyDown, newBlock, TrickleDown.TrickleDown);

        _documentPage.Insert(index, newBlock);

        // UI Toolkit requires a frame delay before focusing newly created elements
        newBlock.schedule.Execute(() =>
        {
            newBlock.Focus();
            newBlock.SelectRange(0, 0); 
        });

        return newBlock;
    }

    private void OnKeyDown(KeyDownEvent evt, TextField currentBlock)
    {
        // 1. SPLIT BLOCK (Enter)
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            evt.StopPropagation(); 
            evt.PreventDefault();  

            int currentIndex = _documentPage.IndexOf(currentBlock);
            int cursorPos = Mathf.Max(0, currentBlock.cursorIndex);

            string currentText = currentBlock.value;
            currentBlock.value = currentText.Substring(0, cursorPos);
            
            CreateBlock(currentIndex + 1, currentText.Substring(cursorPos));
        }
        // 2. MERGE BLOCK (Backspace at the start of a line)
        else if (evt.keyCode == KeyCode.Backspace && currentBlock.cursorIndex == 0)
        {
            int currentIndex = _documentPage.IndexOf(currentBlock);
            
            // Only merge if there is a block above us
            if (currentIndex > 0)
            {
                evt.StopPropagation();
                evt.PreventDefault();

                var prevBlock = _documentPage.ElementAt(currentIndex - 1) as TextField;
                
                // Save the length so we know where to put the cursor
                int prevTextLength = prevBlock.value.Length;
                
                // Append current block's text to the previous block
                prevBlock.value += currentBlock.value;
                
                // Delete current block
                _documentPage.Remove(currentBlock);
                
                // Focus previous block and put cursor exactly where they merged
                prevBlock.schedule.Execute(() =>
                {
                    prevBlock.Focus();
                    prevBlock.SelectRange(prevTextLength, prevTextLength);
                });
            }
        }
    }

    // --- HELPER METHODS ---

    private void CopyBlockStyles(TextField source, TextField destination)
    {
        var sourceInput = GetInnerInput(source);
        var destInput = GetInnerInput(destination);

        if (sourceInput != null && destInput != null)
        {
            // Read the exact styles currently rendered on the source block
            var sourceStyle = sourceInput.resolvedStyle;

            // Apply them to the new destination block
            destInput.style.unityTextAlign = sourceStyle.unityTextAlign;
            destInput.style.unityFontStyleAndWeight = sourceStyle.unityFontStyleAndWeight;
            
            // In our dropdown logic, we applied size to the outer block, so we copy it there
            destination.style.fontSize = source.resolvedStyle.fontSize;
        }
    }
     
    private VisualElement GetInnerInput(TextField block)
    {
        // Retrieves the hidden input element inside the TextField container
        return block?.Q(className: "unity-text-field__input");
    }
    
    // --- NEW: Restore Cursor Position Helper ---
    private void RestoreFocusAndCursor()
    {
        if (_activeBlock == null) return;

        // Save exactly where the blinking cursor '|' and highlighted text are
        int cursorLoc = _activeBlock.cursorIndex;
        int selectLoc = _activeBlock.selectIndex;

        // Wait one frame for the UI to update the style, then force the cursor back
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
            RestoreFocusAndCursor(); // Updated to preserve caret
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
            RestoreFocusAndCursor(); // Updated to preserve caret
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
            RestoreFocusAndCursor(); // Updated to preserve caret
        }
    }
}