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
    private TextField _lastHighlightedBlock;

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
    private Button _cutBtn;
    private Button _copyBtn;
    private Button _pasteBtn;
    private FormatDataLoader _taskController;

    private System.Collections.Generic.List<TextField> _selectedBlocks = new System.Collections.Generic.List<TextField>();
    private Vector2 _startMousePos;
    private bool _isSelecting;
    private TextField _initialDragBlock;
    private VisualElement _marqueeBox;
    private const float DragThreshold = 8f;

    private void OnEnable()
    {
        _doc = GetComponent<UIDocument>();
        var root = _doc.rootVisualElement;

        _documentPage = root.Q<VisualElement>("Document");
        ClearBlocks();

        CacheUIReferences(root);
        SetupRibbon();
        SetupTabs();
        SetupClipboard();

        // Removed TrickleDown to let TextFields handle focus naturally first
        _documentPage.RegisterCallback<PointerDownEvent>(OnPointerDown);
        _documentPage.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        _documentPage.RegisterCallback<PointerUpEvent>(OnPointerUp);

        CreateBlock(0, "");
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0) return;

        _startMousePos = _documentPage.WorldToLocal(evt.position);
        _isSelecting = false;

        // Identify the block we started on
        _initialDragBlock = null;
        VisualElement target = evt.target as VisualElement;
        while (target != null && target != _documentPage)
        {
            if (target is TextField tf)
            {
                _initialDragBlock = tf;
                break;
            }
            target = target.parent;
        }

        // If clicking background, unfocus everything
        if (evt.target == _documentPage)
        {
            _activeBlock?.Blur();
            _activeBlock = null;
            ClearSelection();
            UpdateRibbonState();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if ((evt.pressedButtons & 1) == 0) return;

        Vector2 currentLocalPos = _documentPage.WorldToLocal(evt.position);
        float dist = Vector2.Distance(_startMousePos, currentLocalPos);
        float deltaY = Mathf.Abs(currentLocalPos.y - _startMousePos.y);

        if (!_isSelecting && dist > DragThreshold)
        {
            bool triggerMarquee = false;

            if (_initialDragBlock == null)
            {
                // Dragging from background always starts marquee
                triggerMarquee = true;
            }
            else if (deltaY > 20f) // Threshold to distinguish text select from block select
            {
                // Dragging vertically starting from a block starts marquee
                triggerMarquee = true;
                _initialDragBlock.Blur();
            }

            if (triggerMarquee)
            {
                if (_marqueeBox == null) return;
                _isSelecting = true;
                _marqueeBox.style.display = DisplayStyle.Flex;
                _documentPage.CapturePointer(evt.pointerId);
            }
        }

        if (_isSelecting)
        {
            Rect selectionRect = UpdateMarqueeBox(currentLocalPos);
            CalculateBlockIntersections(selectionRect, currentLocalPos);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.button != 0) return;

        if (_isSelecting)
        {
            _marqueeBox.style.display = DisplayStyle.None;
            _documentPage.ReleasePointer(evt.pointerId);
            _isSelecting = false;

            // If we have a selection, we typically want to clear the active single-block focus 
            // so the user can see the group selection clearly.
            if (_selectedBlocks.Count > 0)
            {
                _activeBlock?.Blur();
                _activeBlock = null;
            }
        }
    }

    private Rect UpdateMarqueeBox(Vector2 currentMousePos)
    {
        float x = Mathf.Min(_startMousePos.x, currentMousePos.x);
        float y = Mathf.Min(_startMousePos.y, currentMousePos.y);
        float width = Mathf.Abs(currentMousePos.x - _startMousePos.x);
        float height = Mathf.Abs(currentMousePos.y - _startMousePos.y);

        _marqueeBox.style.left = x;
        _marqueeBox.style.top = y;
        _marqueeBox.style.width = width;
        _marqueeBox.style.height = height;

        return new Rect(x, y, width, height);
    }

    private void CalculateBlockIntersections(Rect marqueeRect, Vector2 mousePos)
    {
        TextField closestBlock = null;
        float minDistance = float.MaxValue;

        foreach (var child in _documentPage.Children())
        {
            if (child is TextField block && block != _marqueeBox)
            {
                bool isOverlapping = marqueeRect.Overlaps(block.layout);

                if (isOverlapping)
                {
                    if (!_selectedBlocks.Contains(block))
                    {
                        _selectedBlocks.Add(block);
                        block.AddToClassList("editor-block--selected");
                    }

                    // Track the block closest to the mouse cursor (more robust for fast drags/margins)
                    float centerY = block.layout.yMin + (block.layout.height / 2f);
                    float dist = Mathf.Abs(mousePos.y - centerY);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestBlock = block;
                    }
                }
                else
                {
                    if (_selectedBlocks.Contains(block))
                    {
                        _selectedBlocks.Remove(block);
                        block.RemoveFromClassList("editor-block--selected");
                    }
                }
            }
        }

        // Update the focus target to the one the user is currently "pointing" at
        if (closestBlock != null)
        {
            _lastHighlightedBlock = closestBlock;
        }
        else if (_selectedBlocks.Count == 0)
        {
            _lastHighlightedBlock = null;
        }
    }

    private void ClearSelection()
    {
        foreach (var block in _selectedBlocks)
        {
            block.RemoveFromClassList("editor-block--selected");
        }
        _selectedBlocks.Clear();
    }

    private System.Collections.Generic.IEnumerable<TextField> GetAffectedBlocks()
    {
        if (_selectedBlocks != null && _selectedBlocks.Count > 0)
        {
            return _selectedBlocks.OrderBy(block => _documentPage.IndexOf(block));
        }
        return _activeBlock != null ? new[] { _activeBlock } : System.Linq.Enumerable.Empty<TextField>();
    }

    private void Start()
    {
        _taskController = FindObjectOfType<FormatDataLoader>();
    }

    private void CacheUIReferences(VisualElement root)
    {
        _marqueeBox = root.Q<VisualElement>("MarqueeBox");
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
        _cutBtn = root.Q<Button>("Cut");
        _copyBtn = root.Q<Button>("Copy");
        _pasteBtn = root.Q<Button>("Paste");
        _spaceBeforeToggle = root.Q<Toggle>("SpaceBefore");
        _spaceAfterToggle = root.Q<Toggle>("SpaceAfter");
        var ribbonButtons = root.Query<Button>().ToList();
        foreach (var btn in ribbonButtons)
        {
            btn.focusable = false;
        }
    }

    private void ClearBlocks()
    {
        if (_documentPage == null) return;
        
        // Only remove TextFields, keeping the MarqueeBox and other UI structure intact
        var blocks = _documentPage.Query<TextField>().ToList();
        foreach (var block in blocks)
        {
            block.RemoveFromHierarchy();
        }
    }

    public void LoadLevel(DocumentData currentDocument)
    {
        ClearBlocks();
        _activeBlock = null;

        if (currentDocument.startingTextBlocks != null && currentDocument.startingTextBlocks.Count > 0)
        {
            for (int i = 0; i < currentDocument.startingTextBlocks.Count; i++)
            {
                // Don't focus during bulk creation to avoid "Focus Bombing"
                CreateBlock(i, currentDocument.startingTextBlocks[i], false);
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
                if (int.TryParse(evt.newValue, out int newSize))
                {
                    foreach (var block in GetAffectedBlocks())
                    {
                        block.style.fontSize = newSize;
                    }
                    UpdateRibbonState();
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
        // Use the first selected block or the active block to drive the ribbon UI
        var targetBlock = GetAffectedBlocks().FirstOrDefault();
        if (targetBlock == null) return;
        
        var input = GetInnerInput(targetBlock);
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

        // List Sync
        string val = targetBlock.value ?? "";
        bool isBullet = val.StartsWith("• ");
        bool isNumbered = System.Text.RegularExpressions.Regex.IsMatch(val, @"^\s*\d+\.\s*");
        SetButtonActiveState(_bulletBtn, isBullet);
        SetButtonActiveState(_numberBtn, isNumbered);

        if (_styleDropdown != null)
        {
            if (targetBlock.ClassListContains("format-title")) _styleDropdown.SetValueWithoutNotify("Title");
            else if (targetBlock.ClassListContains("format-subtitle")) _styleDropdown.SetValueWithoutNotify("Subtitle");
            else if (targetBlock.ClassListContains("format-h1")) _styleDropdown.SetValueWithoutNotify("Heading 1");
            else if (targetBlock.ClassListContains("format-h2")) _styleDropdown.SetValueWithoutNotify("Heading 2");
            else if (targetBlock.ClassListContains("format-h3")) _styleDropdown.SetValueWithoutNotify("Heading 3");
            else if (targetBlock.ClassListContains("format-h4")) _styleDropdown.SetValueWithoutNotify("Heading 4");
            else if (targetBlock.ClassListContains("format-h5")) _styleDropdown.SetValueWithoutNotify("Heading 5");
            else _styleDropdown.SetValueWithoutNotify("Normal Text");
        }

        // Spacing Sync
        bool hasSpaceBefore = targetBlock.style.marginTop == 12;
        bool hasSpaceAfter = targetBlock.style.marginBottom == 12;

        if (_spaceBeforeToggle != null)
        {
            _spaceBeforeToggle.SetValueWithoutNotify(hasSpaceBefore);
            _spaceBeforeToggle.EnableInClassList("space-active", hasSpaceBefore);
        }

        if (_spaceAfterToggle != null)
        {
            _spaceAfterToggle.SetValueWithoutNotify(hasSpaceAfter);
            _spaceAfterToggle.EnableInClassList("space-active", hasSpaceAfter);
        }
    }

    // Changed from Toggle to VisualElement so Buttons and Toggles can both use this
    private void SetButtonActiveState(VisualElement btn, bool isActive)
    {
        if (btn == null) return;
        btn.EnableInClassList("editor-button--active", isActive);
    }

    private TextField CreateBlock(int index, string initialText, bool shouldFocus = true)
    {
        TextField newBlock = new TextField();
        newBlock.multiline = true;
        newBlock.value = initialText;
        newBlock.selectAllOnFocus = false;
        newBlock.selectAllOnMouseUp = false;
        newBlock.AddToClassList("editor-block");

        newBlock.RegisterCallback<FocusInEvent>(evt =>
        {
            _activeBlock = newBlock;
            ClearSelection();
            newBlock.schedule.Execute(UpdateRibbonState);
        });
        newBlock.RegisterCallback<KeyDownEvent, TextField>(OnKeyDown, newBlock, TrickleDown.TrickleDown);

        _documentPage.Insert(index, newBlock);

        if (shouldFocus)
        {
            newBlock.schedule.Execute(() =>
            {
                newBlock.Focus();
                newBlock.SelectRange(0, 0); 
            });
        }

        return newBlock;
    }

    private void OnKeyDown(KeyDownEvent evt, TextField currentBlock)
    {
        // --- NEW MULTI-BLOCK DELETION ---
        if (_selectedBlocks.Count > 0 && (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete))
        {
            evt.StopPropagation();
            evt.PreventDefault();

            var blocksToDelete = _selectedBlocks.OrderBy(b => _documentPage.IndexOf(b)).ToList();
            int firstIndex = _documentPage.IndexOf(blocksToDelete[0]);

            foreach (var block in blocksToDelete)
            {
                _documentPage.Remove(block);
            }

            ClearSelection();

            if (_documentPage.childCount == 0)
            {
                _activeBlock = CreateBlock(0, "");
            }
            else
            {
                // Focus the block that shifted into the first deleted index, or the last remaining block
                int focusIndex = Mathf.Clamp(firstIndex, 0, _documentPage.childCount - 1);
                _activeBlock = _documentPage[focusIndex] as TextField;
                
                if (_activeBlock != null)
                {
                    _activeBlock.Focus();
                    // Place cursor at the start of the block that took the selection's place
                    _activeBlock.SelectRange(0, 0);
                }
            }

            UpdateRibbonState();
            return;
        }

        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            evt.StopPropagation(); 
            evt.PreventDefault();  

            int currentIndex = _documentPage.IndexOf(currentBlock);
            int cursorPos = Mathf.Max(0, currentBlock.cursorIndex);
            string currentText = currentBlock.value;
            
            // --- NEW LIST LOGIC ---
            string nextPrefix = "";
            if (System.Text.RegularExpressions.Regex.IsMatch(currentText, @"^\s*•\s*")) 
            {
                nextPrefix = "• ";
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(currentText, @"^\s*\d+\.\s*")) 
            {
                var match = System.Text.RegularExpressions.Regex.Match(currentText, @"^\s*(\d+)\.\s*");
                int nextNum = int.Parse(match.Groups[1].Value) + 1;
                nextPrefix = $"{nextNum}. ";
            }
            // ----------------------

            currentBlock.value = currentText.Substring(0, cursorPos);
            
            // Content for new block, cleaned of the split-point artifact or inherited formatting
            string newContent = currentText.Substring(cursorPos);
            if (!string.IsNullOrEmpty(nextPrefix)) newContent = newContent.TrimStart();

            // Inject the prefix into the new block
            TextField newBlock = CreateBlock(currentIndex + 1, nextPrefix + newContent);
            CopyBlockStyles(currentBlock, newBlock);
            
            // Keep the indent if it's a list
            if (nextPrefix != "") 
            {
                newBlock.style.paddingLeft = 20;
                // If it was a numbered list, we might need to re-index things after the new block
                if (nextPrefix.Contains("."))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(nextPrefix, @"^(\d+)\.");
                    int nextNum = int.Parse(match.Groups[1].Value) + 1;
                    ReindexListFrom(currentIndex + 2, nextNum);
                }
            }

            // Remove the newline character that Unity's internal text field logic might append
            currentBlock.schedule.Execute(() => {
                currentBlock.value = currentBlock.value.TrimEnd('\n', '\r');
            });
        }
        else if (evt.keyCode == KeyCode.Backspace)
        {
            int cursorPos = Mathf.Max(0, currentBlock.cursorIndex);
            int selectPos = Mathf.Max(0, currentBlock.selectIndex);

            if (cursorPos == 0 && selectPos == 0 && _documentPage.IndexOf(currentBlock) > 0)
            {
                evt.StopPropagation();
                evt.PreventDefault();

                int currentIndex = _documentPage.IndexOf(currentBlock);
                TextField prevBlock = _documentPage[currentIndex - 1] as TextField;

                if (prevBlock != null)
                {
                    int prevLength = prevBlock.text.Length;

                    // Clean the current block's value using the robust helper before merging
                    prevBlock.value += GetCleanText(currentBlock.value);

                    // Check if we need to re-index after removing this block
                    var match = System.Text.RegularExpressions.Regex.Match(prevBlock.value, @"^\s*(\d+)\.\s*");

                    int nextNum = match.Success ? int.Parse(match.Groups[1].Value) + 1 : 1;
                    
                    _documentPage.Remove(currentBlock);

                    if (match.Success)
                    {
                        ReindexListFrom(currentIndex, nextNum);
                    }

                    prevBlock.schedule.Execute(() =>
                    {
                        prevBlock.Focus();
                        prevBlock.SelectRange(prevLength, prevLength);
                    });
                }
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
        foreach (var block in GetAffectedBlocks())
        {
            var input = GetInnerInput(block);
            if (input != null)
            {
                input.style.unityTextAlign = align;
            }
        }
        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ToggleBold(bool applyBold)
    {
        foreach (var block in GetAffectedBlocks())
        {
            var input = GetInnerInput(block);
            if (input != null)
            {
                var current = input.style.unityFontStyleAndWeight.value;
                bool isItalic = current == FontStyle.Italic || current == FontStyle.BoldAndItalic;

                if (applyBold)
                    input.style.unityFontStyleAndWeight = isItalic ? FontStyle.BoldAndItalic : FontStyle.Bold;
                else
                    input.style.unityFontStyleAndWeight = isItalic ? FontStyle.Italic : FontStyle.Normal;
            }
        }
        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ToggleItalic(bool applyItalic)
    {
        foreach (var block in GetAffectedBlocks())
        {
            var input = GetInnerInput(block);
            if (input != null)
            {
                var current = input.style.unityFontStyleAndWeight.value;
                bool isBold = current == FontStyle.Bold || current == FontStyle.BoldAndItalic;

                if (applyItalic)
                    input.style.unityFontStyleAndWeight = isBold ? FontStyle.BoldAndItalic : FontStyle.Italic;
                else
                    input.style.unityFontStyleAndWeight = isBold ? FontStyle.Bold : FontStyle.Normal;
            }
        }
        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ToggleBullet()
    {
        var blocks = GetAffectedBlocks().ToList();
        if (!blocks.Any()) return;

        bool isAlreadyBullet = blocks[0].value.StartsWith("• ");
        ApplyListFormat(!isAlreadyBullet, false);
    }

    private void ToggleNumber()
    {
        var blocks = GetAffectedBlocks().ToList();
        if (!blocks.Any()) return;

        bool isAlreadyNumber = System.Text.RegularExpressions.Regex.IsMatch(blocks[0].value, @"^\s*\d+\.\s*");
        ApplyListFormat(!isAlreadyNumber, true);
    }

    private string GetCleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Remove ALL leading whitespace, then any list prefix, then any whitespace that was after the prefix
        string cleaned = text.TrimStart();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(•\s*|\d+\.\s*)", "");
        return cleaned.TrimStart();
    }

    private void ApplyListFormat(bool apply, bool isNumbered)
    {
        var blocks = GetAffectedBlocks().ToList();
        if (blocks.Count == 0) return;

        int firstIndex = _documentPage.IndexOf(blocks[0]);
        int runningNumber = 1;

        if (firstIndex > 0)
        {
            TextField prevBlock = _documentPage[firstIndex - 1] as TextField;
            if (prevBlock != null)
            {
                // Match prefix even if it has leading spaces
                var match = System.Text.RegularExpressions.Regex.Match(prevBlock.value, @"^\s*(\d+)\.\s*");
                if (match.Success) runningNumber = int.Parse(match.Groups[1].Value) + 1;
            }
        }

        foreach (var block in blocks)
        {
            string cleanText = GetCleanText(block.value);

            if (apply)
            {
                block.style.paddingLeft = 20;
                block.value = isNumbered ? $"{runningNumber}. {cleanText}" : $"• {cleanText}";
                if (isNumbered) runningNumber++;
            }
            else
            {
                block.style.paddingLeft = 0;
                block.value = cleanText;
            }
        }
        
        int lastIndex = _documentPage.IndexOf(blocks.Last());
        ReindexListFrom(lastIndex + 1, runningNumber);

        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ReindexListFrom(int startIndex, int nextNumber)
    {
        for (int i = startIndex; i < _documentPage.childCount; i++)
        {
            if (_documentPage[i] is TextField block)
            {
                var match = System.Text.RegularExpressions.Regex.Match(block.value, @"^\s*(\d+)\.\s*");
                if (match.Success)
                {
                    string cleanText = GetCleanText(block.value);
                    block.value = $"{nextNumber}. {cleanText}";
                    nextNumber++;
                }
                else break;
            }
        }
    }

    private void ApplyStyle(string styleName)
    {
        string[] allStyles = { "format-normal", "format-title", "format-subtitle", 
                               "format-h1", "format-h2", "format-h3", "format-h4", "format-h5" };

        foreach (var block in GetAffectedBlocks())
        {
            foreach (var style in allStyles) 
                block.RemoveFromClassList(style);

            switch (styleName)
            {
                case "Title": block.AddToClassList("format-title"); break;
                case "Subtitle": block.AddToClassList("format-subtitle"); break;
                case "Heading 1": block.AddToClassList("format-h1"); break;
                case "Heading 2": block.AddToClassList("format-h2"); break;
                case "Heading 3": block.AddToClassList("format-h3"); break;
                case "Heading 4": block.AddToClassList("format-h4"); break;
                case "Heading 5": block.AddToClassList("format-h5"); break;
                default: block.AddToClassList("format-normal"); break;
            }
        }

        UpdateRibbonState();
        RestoreFocusAndCursor();
    }

    private void ApplySpacing(bool addSpace, bool isTop)
    {
        float amount = addSpace ? 12 : 0;

        foreach (var block in GetAffectedBlocks())
        {
            if (isTop) block.style.marginTop = amount;
            else block.style.marginBottom = amount;
        }

        RestoreFocusAndCursor();
    }

    private void SetupClipboard()
    {
        if (_copyBtn != null) _copyBtn.clicked += PerformCopy;
        if (_cutBtn != null) _cutBtn.clicked += PerformCut;
        if (_pasteBtn != null) _pasteBtn.clicked += PerformPaste;
    }

    private void PerformCopy()
    {
        var blocks = GetAffectedBlocks().ToList();
        if (blocks.Count == 0) return;

        string combinedText = string.Join("\n", blocks.Select(b => b.value));
        GUIUtility.systemCopyBuffer = combinedText;
    }

    private void PerformCut()
    {
        var blocks = GetAffectedBlocks().ToList();
        if (blocks.Count == 0) return;

        PerformCopy();

        int firstIndex = _documentPage.IndexOf(blocks[0]);
        
        // If multiple blocks are selected, remove all but the first one and clear it
        if (blocks.Count > 1)
        {
            for (int i = 1; i < blocks.Count; i++)
            {
                _documentPage.Remove(blocks[i]);
            }
        }
        
        blocks[0].value = "";
        blocks[0].Focus();
        _activeBlock = blocks[0];
        ClearSelection();
        UpdateRibbonState();
    }

    private void PerformPaste()
    {
        string clipboardText = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clipboardText) || _activeBlock == null) return;

        string[] lines = clipboardText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        if (lines.Length == 0) return;

        int currentIndex = _documentPage.IndexOf(_activeBlock);
        int cursorLoc = _activeBlock.cursorIndex;
        string currentVal = _activeBlock.value ?? "";

        string prefix = currentVal.Substring(0, Mathf.Clamp(cursorLoc, 0, currentVal.Length));
        string suffix = currentVal.Substring(Mathf.Clamp(cursorLoc, 0, currentVal.Length));

        // Line 1: Merges into the active block
        _activeBlock.value = prefix + lines[0];
        
        TextField lastTarget = _activeBlock;

        // Subsequent lines: Create new blocks
        if (lines.Length > 1)
        {
            for (int i = 1; i < lines.Length; i++)
            {
                // Pass false to CreateBlock to avoid multiple focus requests during multi-line paste
                lastTarget = CreateBlock(currentIndex + i, lines[i], false);
                CopyBlockStyles(_activeBlock, lastTarget);
            }
        }

        // Append the original suffix to the very last block affected by the paste
        lastTarget.value += suffix;
        
        // Focus the last block at the end of the pasted content
        lastTarget.Focus();
        int finalCursorPos = lastTarget.value.Length - suffix.Length;
        lastTarget.SelectRange(finalCursorPos, finalCursorPos);
        _activeBlock = lastTarget;

        UpdateRibbonState();
    }
}