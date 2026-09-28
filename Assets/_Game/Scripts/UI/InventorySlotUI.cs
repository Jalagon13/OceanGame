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

        private InventorySlot _slot;
        private Action _onSlotModified;
        private bool _isPlayerSlot;

        private void OnDestroy()
        {
            if (_isPlayerSlot && InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnPlayerInventoryChanged -= RefreshUI;
            }
        }

        // Called for Player Hotbar / Main Inventory
        public void Initialize(int index)
        {
            _slot = InventoryManager.Instance.PlayerInventory[index];
            _isPlayerSlot = true;
            _onSlotModified = null;

            InventoryManager.Instance.OnPlayerInventoryChanged += RefreshUI;
            RefreshUI();
        }

        // Called for Chests or external containers
        public void Initialize(InventorySlot slot, Action onSlotModified)
        {
            _slot = slot;
            _isPlayerSlot = false;
            _onSlotModified = onSlotModified;

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (_slot == null || _slot.IsEmpty)
            {
                _itemIcon.enabled = false;
                _stackText.text = string.Empty;
            }
            else
            {
                _itemIcon.enabled = true;
                _itemIcon.sprite = GameDataRegistry.Instance.GetItemSOFromItemId(_slot.ItemId).DisplayIcon;
                _stackText.text = _slot.CurrentAmount > 1 ? _slot.CurrentAmount.ToString() : string.Empty;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Player.Instance.Character.Health.CurrentLifeState.Value == LifeState.Dead || _slot == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                InventoryCursorManager.Instance.HandleSlotLeftClick(_slot, _onSlotModified);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                InventoryCursorManager.Instance.HandleSlotRightClick(_slot, _onSlotModified);
            }
        }
    }
}