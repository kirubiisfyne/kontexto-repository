using System.Collections.Generic;
using System.Linq;
using Master.Scripts.GradingSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Master.Scripts.TextEditorSystem
{
    public class TextEditorBlockManager
    {
        private readonly VisualElement _documentPage;
        private readonly System.Action _updateRibbonState;
        private readonly System.Action _clearSelection;
        private readonly System.Func<IEnumerable<TextField>> _getSelectedBlocks;
        private readonly TextEditorFormatting _formatting; // Needed for Enter/Backspace logic

        private TextField _activeBlock;

        public TextField ActiveBlock 
        { 
            get => _activeBlock; 
            set => _activeBlock = value; 
        }

        public TextEditorBlockManager(
            VisualElement documentPage, 
            System.Action updateRibbonState,
            System.Action clearSelection,
            System.Func<IEnumerable<TextField>> getSelectedBlocks,
            TextEditorFormatting formatting)
        {
            _documentPage = documentPage;
            _updateRibbonState = updateRibbonState;
            _clearSelection = clearSelection;
            _getSelectedBlocks = getSelectedBlocks;
            _formatting = formatting;
        }

        public TextField CreateBlock(int index, string initialText, bool shouldFocus = true)
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
                _clearSelection?.Invoke();
                newBlock.schedule.Execute(_updateRibbonState);
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

        public void ClearBlocks()
        {
            if (_documentPage == null) return;
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
                _updateRibbonState?.Invoke();
            }
        }

        public void OnKeyDown(KeyDownEvent evt, TextField currentBlock)
        {
            var selectedBlocks = _getSelectedBlocks().ToList();

            if (selectedBlocks.Count > 0 && (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete))
            {
                evt.StopPropagation();
                evt.PreventDefault();

                var blocksToDelete = selectedBlocks.OrderBy(b => _documentPage.IndexOf(b)).ToList();
                int firstIndex = _documentPage.IndexOf(blocksToDelete[0]);

                foreach (var block in blocksToDelete)
                {
                    _documentPage.Remove(block);
                }

                _clearSelection?.Invoke();

                if (_documentPage.childCount == 0)
                {
                    _activeBlock = CreateBlock(0, "");
                }
                else
                {
                    int focusIndex = Mathf.Clamp(firstIndex, 0, _documentPage.childCount - 1);
                    _activeBlock = _documentPage[focusIndex] as TextField;
                    
                    if (_activeBlock != null)
                    {
                        _activeBlock.Focus();
                        _activeBlock.SelectRange(0, 0);
                    }
                }

                _updateRibbonState?.Invoke();
                return;
            }

            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.StopPropagation(); 
                evt.PreventDefault();  

                int currentIndex = _documentPage.IndexOf(currentBlock);
                int cursorPos = Mathf.Max(0, currentBlock.cursorIndex);
                string currentText = currentBlock.value;
                
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

                currentBlock.value = currentText.Substring(0, cursorPos);
                
                string newContent = currentText.Substring(cursorPos);
                if (!string.IsNullOrEmpty(nextPrefix)) newContent = newContent.TrimStart();

                TextField newBlock = CreateBlock(currentIndex + 1, nextPrefix + newContent);
                CopyBlockStyles(currentBlock, newBlock);
                
                if (nextPrefix != "") 
                {
                    newBlock.style.paddingLeft = 20;
                    if (nextPrefix.Contains("."))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(nextPrefix, @"^(\d+)\.");
                        int nextNum = int.Parse(match.Groups[1].Value) + 1;
                        _formatting.ReindexListFrom(currentIndex + 2, nextNum);
                    }
                }

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
                        prevBlock.value += _formatting.GetCleanText(currentBlock.value);

                        var match = System.Text.RegularExpressions.Regex.Match(prevBlock.value, @"^\s*(\d+)\.\s*");
                        int nextNum = match.Success ? int.Parse(match.Groups[1].Value) + 1 : 1;
                        
                        _documentPage.Remove(currentBlock);

                        if (match.Success)
                        {
                            _formatting.ReindexListFrom(currentIndex, nextNum);
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

        public void CopyBlockStyles(TextField source, TextField destination)
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
    }
}
