using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Master.Scripts.TextEditorSystem
{
    public class TextEditorSelectionController
    {
        private readonly VisualElement _documentPage;
        private readonly VisualElement _marqueeBox;
        private readonly List<TextField> _selectedBlocks = new List<TextField>();
        
        private Vector2 _startMousePos;
        private bool _isSelecting;
        private TextField _initialDragBlock;
        private const float DragThreshold = 8f;

        public List<TextField> SelectedBlocks => _selectedBlocks;
        public bool IsSelecting => _isSelecting;

        public TextEditorSelectionController(VisualElement documentPage, VisualElement marqueeBox)
        {
            _documentPage = documentPage;
            _marqueeBox = marqueeBox;
        }

        public void HandlePointerDown(PointerDownEvent evt, System.Action<TextField> onBackgroundClicked, TextField activeBlock)
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

            // If clicking background, notify manager to handle focus/UI sync
            if (evt.target == _documentPage)
            {
                onBackgroundClicked?.Invoke(activeBlock);
                ClearSelection();
            }
        }

        public void HandlePointerMove(PointerMoveEvent evt)
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
                    triggerMarquee = true;
                }
                else if (deltaY > 20f) 
                {
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
                CalculateBlockIntersections(selectionRect);
            }
        }

        public void HandlePointerUp(PointerUpEvent evt, System.Action onSelectionMade)
        {
            if (evt.button != 0) return;

            if (_isSelecting)
            {
                _marqueeBox.style.display = DisplayStyle.None;
                _documentPage.ReleasePointer(evt.pointerId);
                _isSelecting = false;

                if (_selectedBlocks.Count > 0)
                {
                    onSelectionMade?.Invoke();
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

        private void CalculateBlockIntersections(Rect marqueeRect)
        {
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
        }

        public void ClearSelection()
        {
            foreach (var block in _selectedBlocks)
            {
                block.RemoveFromClassList("editor-block--selected");
            }
            _selectedBlocks.Clear();
        }
    }
}
