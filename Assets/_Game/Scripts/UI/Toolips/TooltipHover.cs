using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OceanGame
{
    public class TooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action OnHoverEnter;  // caller populates this

        public void OnPointerEnter(PointerEventData _)
        {
            OnHoverEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData _)
        {
            Tooltip.HideUI();
        }
    }
}