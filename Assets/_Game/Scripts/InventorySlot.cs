using System;
using UnityEngine;

namespace OceanGame 
{
    [Serializable]
    public class InventorySlot
    {
        public ushort ItemId { get; private set; } 
        public int CurrentAmount { get; private set; } = 0;

        public bool IsEmpty => CurrentAmount <= 0;

        public InventorySlot(ItemSO itemSO, int amount)
        {
            Clear();

            AssignItem(GameDataRegistry.Instance.GetItemIdFromItemSO(itemSO), amount);
        }

        public InventorySlot(ushort id, int amount)
        {
            Clear();
            AssignItem(id, amount);
        }

        public InventorySlot()
        {
            Clear();
        }
        
        public ItemSO GetItemSO()
        {
            return GameDataRegistry.Instance.GetItemSOFromItemId(ItemId);
        }

        public void AssignItem(ushort itemId, int amount)
        {
            ItemId = itemId;
            CurrentAmount = amount;
        }

        public void AddToCurrentAmount(int amount)
        {
            CurrentAmount += amount;
        }

        public void RemoveFromCurrentAmount(int amount)
        {
            CurrentAmount -= amount;
            if (CurrentAmount <= 0)
            {
                Clear();
            }
        }

        public void Clear()
        {
            ItemId = 0;
            CurrentAmount = 0;
        }
        
        public InventorySlot Clone()
        {
            return IsEmpty ? new InventorySlot() : new InventorySlot(ItemId, CurrentAmount);
        }
    }
}