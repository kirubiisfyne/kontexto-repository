using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Master.Scripts.TextEditorSystem
{
    public class TextEditorFormatting
    {
        private readonly VisualElement _documentPage;
        private readonly System.Func<IEnumerable<TextField>> _getAffectedBlocks;
        private readonly System.Action _updateRibbonState;
        private readonly System.Action _restoreFocus;
        private readonly System.Func<TextField, int, string, bool, TextField> _createBlock;
        private readonly System.Action<TextField, TextField> _copyBlockStyles;

        public TextEditorFormatting(
            VisualElement documentPage, 
            System.Func<IEnumerable<TextField>> getAffectedBlocks,
            System.Action updateRibbonState,
            System.Action restoreFocus,
            System.Func<TextField, int, string, bool, TextField> createBlock,
            System.Action<TextField, TextField> copyBlockStyles)
        {
            _documentPage = documentPage;
            _getAffectedBlocks = getAffectedBlocks;
            _updateRibbonState = updateRibbonState;
            _restoreFocus = restoreFocus;
            _createBlock = createBlock;
            _copyBlockStyles = copyBlockStyles;
        }

        public void ApplyAlignment(TextAnchor align)
        {
            foreach (var block in _getAffectedBlocks())
            {
                var input = GetInnerInput(block);
                if (input != null)
                {
                    input.style.unityTextAlign = align;
                }
            }
            _updateRibbonState?.Invoke();
            _restoreFocus?.Invoke();
        }

        public void ToggleBold(bool applyBold)
        {
            foreach (var block in _getAffectedBlocks())
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
            _updateRibbonState?.Invoke();
            _restoreFocus?.Invoke();
        }

        public void ToggleItalic(bool applyItalic)
        {
            foreach (var block in _getAffectedBlocks())
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
            _updateRibbonState?.Invoke();
            _restoreFocus?.Invoke();
        }

        public void ToggleBullet()
        {
            var blocks = _getAffectedBlocks().ToList();
            if (!blocks.Any()) return;

            bool isAlreadyBullet = blocks[0].value.StartsWith("• ");
            ApplyListFormat(!isAlreadyBullet, false);
        }

        public void ToggleNumber()
        {
            var blocks = _getAffectedBlocks().ToList();
            if (!blocks.Any()) return;

            bool isAlreadyNumber = System.Text.RegularExpressions.Regex.IsMatch(blocks[0].value, @"^\s*\d+\.\s*");
            ApplyListFormat(!isAlreadyNumber, true);
        }

        public void ApplyListFormat(bool apply, bool isNumbered)
        {
            var blocks = _getAffectedBlocks().ToList();
            if (blocks.Count == 0) return;

            int firstIndex = _documentPage.IndexOf(blocks[0]);
            int runningNumber = 1;

            if (firstIndex > 0)
            {
                TextField prevBlock = _documentPage[firstIndex - 1] as TextField;
                if (prevBlock != null)
                {
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

            _updateRibbonState?.Invoke();
            _restoreFocus?.Invoke();
        }

        public void ReindexListFrom(int startIndex, int nextNumber)
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

        public void ApplyStyle(string styleName)
        {
            string[] allStyles = { "format-normal", "format-title", "format-subtitle", 
                                   "format-h1", "format-h2", "format-h3", "format-h4", "format-h5" };

            foreach (var block in _getAffectedBlocks())
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

            _updateRibbonState?.Invoke();
            _restoreFocus?.Invoke();
        }

        public void ApplySpacing(bool addSpace, bool isTop)
        {
            float amount = addSpace ? 12 : 0;

            foreach (var block in _getAffectedBlocks())
            {
                if (isTop) block.style.marginTop = amount;
                else block.style.marginBottom = amount;
            }

            _restoreFocus?.Invoke();
        }

        public string GetCleanText(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string cleaned = text.TrimStart();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(•\s*|\d+\.\s*)", "");
            return cleaned.TrimStart();
        }

        private VisualElement GetInnerInput(TextField block)
        {
            return block?.Q(className: "unity-text-field__input");
        }

        public void PerformCopy()
        {
            var blocks = _getAffectedBlocks().ToList();
            if (blocks.Count == 0) return;

            string combinedText = string.Join("\n", blocks.Select(b => b.value));
            GUIUtility.systemCopyBuffer = combinedText;
        }

        public void PerformCut(System.Action<TextField> setActiveBlock, System.Action clearSelection)
        {
            var blocks = _getAffectedBlocks().ToList();
            if (blocks.Count == 0) return;

            PerformCopy();

            if (blocks.Count > 1)
            {
                for (int i = 1; i < blocks.Count; i++)
                {
                    _documentPage.Remove(blocks[i]);
                }
            }
            
            blocks[0].value = "";
            blocks[0].Focus();
            setActiveBlock?.Invoke(blocks[0]);
            clearSelection?.Invoke();
            _updateRibbonState?.Invoke();
        }

        public void PerformPaste(TextField activeBlock, System.Action<TextField> setActiveBlock)
        {
            string clipboardText = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrEmpty(clipboardText) || activeBlock == null) return;

            string[] lines = clipboardText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            if (lines.Length == 0) return;

            int currentIndex = _documentPage.IndexOf(activeBlock);
            int cursorLoc = activeBlock.cursorIndex;
            string currentVal = activeBlock.value ?? "";

            string prefix = currentVal.Substring(0, Mathf.Clamp(cursorLoc, 0, currentVal.Length));
            string suffix = currentVal.Substring(Mathf.Clamp(cursorLoc, 0, currentVal.Length));

            activeBlock.value = prefix + lines[0];
            
            TextField lastTarget = activeBlock;

            if (lines.Length > 1)
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    lastTarget = _createBlock(activeBlock, currentIndex + i, lines[i], false);
                    _copyBlockStyles(activeBlock, lastTarget);
                }
            }

            lastTarget.value += suffix;
            
            lastTarget.Focus();
            int finalCursorPos = lastTarget.value.Length - suffix.Length;
            lastTarget.SelectRange(finalCursorPos, finalCursorPos);
            setActiveBlock?.Invoke(lastTarget);

            _updateRibbonState?.Invoke();
        }
    }
}
