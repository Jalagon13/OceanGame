using System;
using UnityEngine;

namespace OceanGame
{
    public class InventoryCursorManager : MonoBehaviour
    {
        public static InventoryCursorManager Instance { get; private set; }
        
        public event Action OnCursorSlotChanged;

        public InventorySlot CursorSlot { get; private set; } = new();

        private void Awake()
        {
            Instance = this;
        }

        public void HandleSlotLeftClick(int clickedIndex)
        {
            if(CursorSlot.IsEmpty && InventoryManager.Instance.PlayerInventory[clickedIndex].IsEmpty) return;
            
            // At least one of the 2 are not empty
            
            if(CursorSlot.IsEmpty) // Cursor slot is empty and clicked slot has an item
            {
                // Clicked Slot is not empty
                CursorSlot = InventoryManager.Instance.PlayerInventory[clickedIndex].Clone();
                InventoryManager.Instance.PlayerInventory[clickedIndex].Clear();
                
                InventoryManager.Instance.RefreshInventory();
                OnCursorSlotChanged?.Invoke();
                return;
            }
            
            if(InventoryManager.Instance.PlayerInventory[clickedIndex].IsEmpty) // Cursor slot has an item and clicked slot is empty
            {
                InventoryManager.Instance.PlayerInventory[clickedIndex] = CursorSlot.Clone();
                CursorSlot.Clear();

                InventoryManager.Instance.RefreshInventory();
                OnCursorSlotChanged?.Invoke();
                return;
            }

            // If we get to here, both the clicked slot and cursor slot have some item
            if (CanStacksMerge(InventoryManager.Instance.PlayerInventory[clickedIndex], CursorSlot))
            {
                int movedAmount = MoveAmount(CursorSlot, InventoryManager.Instance.PlayerInventory[clickedIndex], GetMaxStackSize(GameDataRegistry.Instance.GetItemFromId(InventoryManager.Instance.PlayerInventory[clickedIndex].ItemId)));
                if (movedAmount > 0)
                {
                    InventoryManager.Instance.RefreshInventory();
                    OnCursorSlotChanged?.Invoke();
                }

                return;
            }

            // If they both have a stack and both different items, swap them
            InventorySlot swappedItem = InventoryManager.Instance.PlayerInventory[clickedIndex].Clone();
            InventoryManager.Instance.PlayerInventory[clickedIndex] = CursorSlot.Clone();
            CursorSlot = swappedItem;

            InventoryManager.Instance.RefreshInventory();
            OnCursorSlotChanged?.Invoke();
        }

        public void HandleSlotRightClick(int slotIndex)
        {
            
        }

        private int MoveAmount(InventorySlot source, InventorySlot target, int maxTargetAmount, int requestedAmount = int.MaxValue)
        {
            if (source == null || target == null || source.IsEmpty || target.IsEmpty)
            {
                return 0;
            }

            int amountToMove = Mathf.Min(requestedAmount, source.CurrentAmount);
            amountToMove = Mathf.Min(amountToMove, maxTargetAmount - target.CurrentAmount);
            if (amountToMove <= 0)
            {
                return 0;
            }

            target.AddToCurrentAmount(amountToMove);
            source.RemoveFromCurrentAmount(amountToMove);
            return amountToMove;
        }

        private bool CanStacksMerge(InventorySlot target, InventorySlot source)
        {
            return target != null &&
                source != null &&
                !target.IsEmpty &&
                !source.IsEmpty &&
                target.ItemId == source.ItemId &&
                target.CurrentAmount < GetMaxStackSize(GameDataRegistry.Instance.GetItemFromId(target.ItemId));
        }

        private int GetMaxStackSize(ItemSO item)
        {
            if (item == null)
            {
                return 1;
            }

            return item.IsStackable ? InventoryManager.Instance.MaxStackSize : 1;
        }

    }
}
