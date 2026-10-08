using System;
using UnityEngine;

namespace OceanGame 
{
    [Serializable]
    public class InventorySlot
    {
        public ushort ItemId { get; private set; } 
        public int Amount { get; private set; } = 0;

        public bool IsEmpty => Amount <= 0;

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
            Amount = amount;
        }

        public void AddToCurrentAmount(int amount)
        {
            Amount += amount;
        }

        public void RemoveFromCurrentAmount(int amount)
        {
            Amount -= amount;
            if (Amount <= 0)
            {
                Clear();
            }
        }

        public void Clear()
        {
            ItemId = 0;
            Amount = 0;
        }
        
        public InventorySlot Clone()
        {
            return IsEmpty ? new InventorySlot() : new InventorySlot(ItemId, Amount);
        }
    }
}