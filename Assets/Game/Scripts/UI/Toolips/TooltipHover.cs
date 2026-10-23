using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SketchLand
{
    public class TooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnHoverEnter;  // caller populates this

        private bool _isPointerOver;

        private void Start()
        {
            if (InventoryCursorManager.Instance != null)
            {
                InventoryCursorManager.Instance.OnCursorSlotChanged += RefreshTooltip;
            }
        }

        private void OnDestroy()
        {
            if (InventoryCursorManager.Instance != null)
            {
                InventoryCursorManager.Instance.OnCursorSlotChanged -= RefreshTooltip;
            }
        }

        public void OnPointerEnter(PointerEventData _)
        {
            _isPointerOver = true;
            RefreshTooltip();
        }

        public void OnPointerExit(PointerEventData _)
        {
            _isPointerOver = false;
            Tooltip.HideUI();
        }

        public void RefreshTooltip()
        {
            if (!_isPointerOver) return;

            if (InventoryCursorManager.Instance != null && !InventoryCursorManager.Instance.CursorSlot.IsEmpty)
            {
                Tooltip.HideUI();
                return;
            }

            OnHoverEnter?.Invoke();
        }
    }
}