using System.Collections.Generic;
using System.Linq;
using Master.Scripts;
using Master.Scripts.GradingSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Master.Scripts.TextEditorSystem
{
    public class TextEditorManager : MonoBehaviour
    {
        private UIDocument _doc;
        private VisualElement _documentPage;
        private FormatDataLoader _taskController;

        private TextEditorSelectionController _selectionController;
        private TextEditorRibbonController _ribbonController;
        private TextEditorFormatting _formatting;
        private TextEditorBlockManager _blockManager;

        private void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;

            _documentPage = root.Q<VisualElement>("Document");
            var marqueeBox = root.Q<VisualElement>("MarqueeBox");
            
            // 1. Initialize Controllers
            _selectionController = new TextEditorSelectionController(_documentPage, marqueeBox);
            _ribbonController = new TextEditorRibbonController(root);
            
            _formatting = new TextEditorFormatting(
                _documentPage, 
                GetAffectedBlocks, 
                UpdateRibbonState, 
                RestoreFocusAndCursor,
                (source, index, text, focus) => _blockManager.CreateBlock(index, text, focus),
                (source, dest) => _blockManager.CopyBlockStyles(source, dest)
            );

            _blockManager = new TextEditorBlockManager(
                _documentPage, 
                UpdateRibbonState, 
                ClearSelection, 
                () => _selectionController.SelectedBlocks,
                _formatting
            );

            // 2. Setup UI & Events
            _blockManager.ClearBlocks();
            SetupRibbon();
            SetupTabs();
            SetupClipboard();

            _documentPage.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _documentPage.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            _documentPage.RegisterCallback<PointerUpEvent>(OnPointerUp);

            _blockManager.CreateBlock(0, "");
        }

        private void Start()
        {
            _taskController = FindObjectOfType<FormatDataLoader>();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _selectionController.HandlePointerDown(evt, (active) => {
                active?.Blur();
                _blockManager.ActiveBlock = null;
                UpdateRibbonState();
            }, _blockManager.ActiveBlock);
        }

        private void OnPointerMove(PointerMoveEvent evt) => _selectionController.HandlePointerMove(evt);

        private void OnPointerUp(PointerUpEvent evt)
        {
            _selectionController.HandlePointerUp(evt, () => {
                _blockManager.ActiveBlock?.Blur();
                _blockManager.ActiveBlock = null;
            });
        }

        public void LoadLevel(DocumentData currentDocument) => _blockManager.LoadLevel(currentDocument);
private void SetupRibbon()
{
    _ribbonController.SetupRibbon(
        _formatting.ToggleBullet,
        _formatting.ToggleNumber,
        _formatting.ApplyAlignment,
        _formatting.ApplySpacing,
        _formatting.ToggleBold,
        _formatting.ToggleItalic,
        (newSize) => {
            foreach (var block in GetAffectedBlocks())
            {
                block.style.fontSize = newSize;
            }
            // Removed UpdateRibbonState to prevent engine-lag overwrite
            RestoreFocusAndCursor();
        },
        _formatting.ApplyStyle
    );
}

        private void SetupClipboard()
        {
            _ribbonController.SetupClipboard(
                _formatting.PerformCopy,
                () => _formatting.PerformCut(block => _blockManager.ActiveBlock = block, ClearSelection),
                () => _formatting.PerformPaste(_blockManager.ActiveBlock, block => _blockManager.ActiveBlock = block)
            );
        }

        private void SetupTabs()
        {
            var root = _doc.rootVisualElement;
            root.Q<Button>("Exit").clicked += () => SceneGateManager.Instance.StartWarp(SceneGateManager.Instance.lastSceneString);
            
            Button printButton = root.Q<Button>("Print"); 
            if (printButton != null)
            {
                printButton.clicked += () => _taskController?.EvaluatePrintJob(_documentPage);
            }
        }

        private void UpdateRibbonState()
        {
            var targetBlock = GetAffectedBlocks().FirstOrDefault();
            _ribbonController.UpdateRibbonState(targetBlock);
        }

        private void RestoreFocusAndCursor()
        {
            var active = _blockManager.ActiveBlock;
            if (active == null) return;

            int cursorLoc = active.cursorIndex;
            int selectLoc = active.selectIndex;

            active.schedule.Execute(() =>
            {
                active.Focus();
                active.SelectRange(cursorLoc, selectLoc);
            });
        }

        private void ClearSelection() => _selectionController.ClearSelection();

        private IEnumerable<TextField> GetAffectedBlocks()
        {
            if (_selectionController.SelectedBlocks.Count > 0)
                return _selectionController.SelectedBlocks.OrderBy(block => _documentPage.IndexOf(block));
            
            return _blockManager.ActiveBlock != null ? new[] { _blockManager.ActiveBlock } : Enumerable.Empty<TextField>();
        }
    }
}
