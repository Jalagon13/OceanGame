using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OceanGame
{
    public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _itemIcon;
        [SerializeField] private TextMeshProUGUI _stackText;

        private int _slotIndex;

        private void OnDestroy()
        {
            InventoryManager.Instance.OnPlayerInventoryChanged -= RefreshUI;
        }
    
        public void Initialize(int index)
        {
            _slotIndex = index;
        
            InventoryManager.Instance.OnPlayerInventoryChanged += RefreshUI;
            
            RefreshUI();
        }

        private void RefreshUI()
        {
            var slot = InventoryManager.Instance.PlayerInventory[_slotIndex];
            
            if(slot.IsEmpty)
            {
                _itemIcon.enabled = false;
                _stackText.text = string.Empty;
            }
            else
            {
                _itemIcon.enabled = true;
                _itemIcon.sprite = GameDataRegistry.Instance.GetItemSOFromItemId(slot.ItemId).DisplayIcon;
                _stackText.text = slot.CurrentAmount > 1 ? slot.CurrentAmount.ToString() : string.Empty;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if(eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryCursorManager.Instance.HandleSlotLeftClick(_slotIndex);
            }
            else if(eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryCursorManager.Instance.HandleSlotRightClick(_slotIndex);
            }
        }
    }
}