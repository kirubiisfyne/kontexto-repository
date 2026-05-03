using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Master.Scripts.TextEditorSystem
{
    public class TextEditorRibbonController
    {
        private readonly VisualElement _root;
        
        private Toggle _boldToggle;
        private Toggle _italicToggle;
        private Button _leftBtn;
        private Button _centerBtn;
        private Button _rightBtn;
        private Toggle _spaceAfterToggle;
        private Toggle _spaceBeforeToggle;
        private DropdownField _sizeDropdown;
        private DropdownField _styleDropdown;
        private Button _bulletBtn;
        private Button _numberBtn;
        private Button _cutBtn;
        private Button _copyBtn;
        private Button _pasteBtn;

        public TextEditorRibbonController(VisualElement root)
        {
            _root = root;
            CacheUIReferences();
        }

        private void CacheUIReferences()
        {
            _boldToggle = _root.Q<Toggle>("Bold");
            _italicToggle = _root.Q<Toggle>("Italic");
            _leftBtn = _root.Q<Button>("Left");
            _centerBtn = _root.Q<Button>("Center");
            _rightBtn = _root.Q<Button>("Right");
            _sizeDropdown = _root.Q<DropdownField>("Size");
            _styleDropdown = _root.Q<DropdownField>("Styles");
            _bulletBtn = _root.Q<Button>("Bullet");
            _numberBtn = _root.Q<Button>("Numbering");
            _cutBtn = _root.Q<Button>("Cut");
            _copyBtn = _root.Q<Button>("Copy");
            _pasteBtn = _root.Q<Button>("Paste");
            _spaceBeforeToggle = _root.Q<Toggle>("SpaceBefore");
            _spaceAfterToggle = _root.Q<Toggle>("SpaceAfter");

            var ribbonButtons = _root.Query<Button>().ToList();
            foreach (var btn in ribbonButtons)
            {
                btn.focusable = false;
            }
        }

        public void SetupRibbon(
            System.Action toggleBullet, 
            System.Action toggleNumber, 
            System.Action<TextAnchor> applyAlignment,
            System.Action<bool, bool> applySpacing,
            System.Action<bool> toggleBold,
            System.Action<bool> toggleItalic,
            System.Action<int> applySize,
            System.Action<string> applyStyle)
        {
            if (_bulletBtn != null) _bulletBtn.clicked += toggleBullet;
            if (_numberBtn != null) _numberBtn.clicked += toggleNumber;

            if (_leftBtn != null) _leftBtn.clicked += () => applyAlignment(TextAnchor.UpperLeft);
            if (_centerBtn != null) _centerBtn.clicked += () => applyAlignment(TextAnchor.UpperCenter);
            if (_rightBtn != null) _rightBtn.clicked += () => applyAlignment(TextAnchor.UpperRight);

            _spaceBeforeToggle.RegisterValueChangedCallback(evt => applySpacing(evt.newValue, true));
            _spaceAfterToggle.RegisterValueChangedCallback(evt => applySpacing(evt.newValue, false));
            
            if (_boldToggle != null) 
                _boldToggle.RegisterValueChangedCallback(evt => toggleBold(evt.newValue));
                
            if (_italicToggle != null) 
                _italicToggle.RegisterValueChangedCallback(evt => toggleItalic(evt.newValue));

            if (_sizeDropdown != null)
            {
                _sizeDropdown.RegisterValueChangedCallback(evt => 
                {
                    if (int.TryParse(evt.newValue, out int newSize))
                    {
                        applySize(newSize);
                    }
                });
            }

            if (_styleDropdown != null)
            {
                _styleDropdown.RegisterValueChangedCallback(evt => applyStyle(evt.newValue));
            }
        }

        public void SetupClipboard(System.Action copy, System.Action cut, System.Action paste)
        {
            if (_copyBtn != null) _copyBtn.clicked += copy;
            if (_cutBtn != null) _cutBtn.clicked += cut;
            if (_pasteBtn != null) _pasteBtn.clicked += paste;
        }

        public void UpdateRibbonState(TextField targetBlock)
        {
            if (targetBlock == null) return;
            
            var input = targetBlock.Q(className: "unity-text-field__input");
            if (input == null) return;

            var currentStyle = input.resolvedStyle;

            if (_sizeDropdown != null)
            {
                int currentSize = Mathf.RoundToInt(currentStyle.fontSize);
                _sizeDropdown.SetValueWithoutNotify(currentSize.ToString());
            }

            bool isBold = currentStyle.unityFontStyleAndWeight == FontStyle.Bold || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;
            bool isItalic = currentStyle.unityFontStyleAndWeight == FontStyle.Italic || currentStyle.unityFontStyleAndWeight == FontStyle.BoldAndItalic;

            if (_boldToggle != null) _boldToggle.SetValueWithoutNotify(isBold);
            if (_italicToggle != null) _italicToggle.SetValueWithoutNotify(isItalic);

            SetButtonActiveState(_boldToggle, isBold);
            SetButtonActiveState(_italicToggle, isItalic);

            TextAnchor align = currentStyle.unityTextAlign;
            SetButtonActiveState(_leftBtn, align == TextAnchor.UpperLeft || align == TextAnchor.MiddleLeft || align == TextAnchor.LowerLeft);
            SetButtonActiveState(_centerBtn, align == TextAnchor.UpperCenter || align == TextAnchor.MiddleCenter || align == TextAnchor.LowerCenter);
            SetButtonActiveState(_rightBtn, align == TextAnchor.UpperRight || align == TextAnchor.MiddleRight || align == TextAnchor.LowerRight);

            string val = targetBlock.value ?? "";
            SetButtonActiveState(_bulletBtn, val.StartsWith("• "));
            SetButtonActiveState(_numberBtn, System.Text.RegularExpressions.Regex.IsMatch(val, @"^\s*\d+\.\s*"));

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

            bool hasSpaceBefore = targetBlock.style.marginTop == 12;
            bool hasSpaceAfter = targetBlock.style.marginBottom == 12;
            if (_spaceBeforeToggle != null) _spaceBeforeToggle.SetValueWithoutNotify(hasSpaceBefore);
            if (_spaceAfterToggle != null) _spaceAfterToggle.SetValueWithoutNotify(hasSpaceAfter);
        }

        private void SetButtonActiveState(VisualElement btn, bool isActive)
        {
            if (btn == null) return;
            btn.EnableInClassList("editor-button--active", isActive);
        }
    }
}
