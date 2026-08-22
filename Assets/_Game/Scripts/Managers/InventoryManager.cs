using System;
using UnityEngine;

namespace OceanGame
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Setup")]
        [SerializeField] private int _inventorySize = 36; 
        [SerializeField] private int _maxStackSize = 999;

        private InventorySlot[] _slots;

        private void Awake() 
        {
            Instance = this;

            // Initialize Inventory Slots
            _slots = new InventorySlot[_inventorySize];
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = new InventorySlot();
            }
        }

        public bool AddItem(int itemId, int amount)
        {
            // Search for existing matching stacks
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].ItemId == itemId)
                {
                    // Check if there is room remaining in this stack
                    int roomLeft = _maxStackSize - _slots[i].StackSize;
                    if(roomLeft > 0)
                    {
                        int amountToAdd = Mathf.Min(amount, roomLeft);
                        _slots[i].AddToStack(amountToAdd);
                        amount -= amountToAdd;

                        if (amount <= 0) return true;
                    }
                }
            }

            // Next find a fresh empty slot for leftover amounts
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    int amountToAdd = Mathf.Min(amount, _maxStackSize);
                    _slots[i].AssignItem(itemId, amountToAdd);
                    amount -= amountToAdd;

                    if (amount <= 0) return true;
                }
            }

            // If amount is still greater than 0, inventory is completely full
            Debug.LogWarning($"No more space in inventory. Need to develop what happens when inventory is full");
            return amount <= 0;
        }

        public bool RemoveItem(int itemId, int amount)
        {
            // Verify the player actually has enough total items to remove
            if (GetTotalItemCount(itemId) < amount) return false;

            // Remove items starting from the back of the inventory
            for (int i = _slots.Length -1; i >= 0; i--)
            {
                if (!_slots[i].IsEmpty && _slots[i].ItemId == itemId)
                {
                    if(_slots[i].StackSize >= amount)
                    {
                        _slots[i].RemoveFromStack(amount);
                        return true;
                    }
                    else
                    {
                        amount -= _slots[i].StackSize;
                        _slots[i].Clear();
                    }
                }
            }
            
            return true;
        }

        public int GetTotalItemCount(int itemId)
        {
            int total = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].ItemId == itemId)
                {
                    total += _slots[i].StackSize;
                }
            }
            return total;
        }
    }
    
    [Serializable]
    public class InventorySlot
    {
        public static int EMPTY_SLOT_ID { get; } = -1;

        public int ItemId { get; private set; } = EMPTY_SLOT_ID;
        public int StackSize { get; private set; } = 0;

        public bool IsEmpty => ItemId == -1 || StackSize <= 0;

        public InventorySlot()
        {
            Clear();
        }

        public void AssignItem(int itemId, int amount)
        {
            ItemId = itemId;
            StackSize = amount;
        }

        public void AddToStack(int amount)
        {
            StackSize += amount;
        }

        public void RemoveFromStack(int amount)
        {
            StackSize -= amount;
            if (StackSize <= 0)
            {
                Clear();
            }
        }

        public void Clear()
        {
            ItemId = EMPTY_SLOT_ID;
            StackSize = 0;
        }
    }
}
