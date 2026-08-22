using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OceanGame
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Header("Inventory Setup")]
        [field: SerializeField] public int InventorySize { get; private set; } = 40;
        [field: SerializeField] public int MaxStackSize { get; private set; } = 999;
        [field: SerializeField] public int HotbarSize { get; private set; } = 10;

        public InventorySlot[] Slots { get; private set; }
        public InventorySlot SelectedSlot { get; private set; }
        public int SelectedSlotIndex { get; private set; }
        
        private void Awake() 
        {
            Instance = this;

            // Initialize Inventory Slots
            Slots = new InventorySlot[InventorySize];
            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = new InventorySlot();
            }
        }
        
        private void Start() 
        {
            GameInput.Instance.OnScrollWheel += OnScrollWheel;
            GameInput.Instance.OnSelectSlot += OnSelectSlot;
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnScrollWheel -= OnScrollWheel;
            GameInput.Instance.OnSelectSlot -= OnSelectSlot;
        }

        #region Inventory Functions

        public int AddItem(int itemId, int amount) // Returns remainder 
        {
            // Search for existing matching stacks
            for (int i = 0; i < Slots.Length; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].ItemId == itemId)
                {
                    // Check if there is room remaining in this stack
                    int roomLeft = MaxStackSize - Slots[i].StackSize;
                    if (roomLeft > 0)
                    {
                        int amountToAdd = Mathf.Min(amount, roomLeft);
                        Slots[i].AddToStack(amountToAdd);
                        amount -= amountToAdd;

                        if (amount <= 0) return 0;
                    }
                }
            }

            // Next find a fresh empty slot for leftover amounts
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].IsEmpty)
                {
                    int amountToAdd = Mathf.Min(amount, MaxStackSize);
                    Slots[i].AssignItem(itemId, amountToAdd);
                    amount -= amountToAdd;

                    if (amount <= 0) return 0;
                }
            }

            // If amount is still greater than 0, inventory is completely full
            Debug.LogWarning($"No more space in inventory. Need to develop what happens when inventory is full");
            return amount;
        }

        public bool RemoveItem(int itemId, int amount)
        {
            // Verify the player actually has enough total items to remove
            if (GetTotalItemCount(itemId) < amount) return false;

            // Remove items starting from the back of the inventory
            for (int i = Slots.Length - 1; i >= 0; i--)
            {
                if (!Slots[i].IsEmpty && Slots[i].ItemId == itemId)
                {
                    if (Slots[i].StackSize >= amount)
                    {
                        Slots[i].RemoveFromStack(amount);
                        return true;
                    }
                    else
                    {
                        amount -= Slots[i].StackSize;
                        Slots[i].Clear();
                    }
                }
            }

            return true;
        }

        public int GetTotalItemCount(int itemId)
        {
            int total = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                if (!Slots[i].IsEmpty && Slots[i].ItemId == itemId)
                {
                    total += Slots[i].StackSize;
                }
            }
            return total;
        }

        public bool CanAcceptItem(int itemId, int amount)
        {
            int remainingAmount = amount;

            for (int i = 0; i < Slots.Length; i++)
            {
                // If it's an empty slot, it can hold a full maximum stack size
                if (Slots[i].IsEmpty)
                {
                    remainingAmount -= MaxStackSize;
                }
                // If it matches, it can hold whatever room is left in this specific stack
                else if (Slots[i].ItemId == itemId)
                {
                    int roomLeft = MaxStackSize - Slots[i].StackSize;
                    if (roomLeft > 0)
                    {
                        remainingAmount -= roomLeft;
                    }
                }

                // if remaining amount 0, it can accept it
                if (remainingAmount <= 0)
                {
                    return true;
                }
            }

            // If we finished checking every slot and there's still leftover amount, it cannot fully fit
            return false;
        }

        #endregion

        #region Selection Input

        private void OnScrollWheel(InputAction.CallbackContext context)
        {
            Vector2 scrollDelta = context.ReadValue<Vector2>();
            int itemCount = HotbarSize;
            if (itemCount == 0)
            {
                return;
            }

            int selectedSlotIndex = SelectedSlotIndex;

            if (scrollDelta.y > 0f)
            {
                int upcomingIndex = selectedSlotIndex - 1;
                selectedSlotIndex = upcomingIndex < 0 ? itemCount - 1 : selectedSlotIndex - 1;
                SelectHotbarSlot(selectedSlotIndex);
            }
            else if (scrollDelta.y < 0f)
            {
                int upcomingIndex = selectedSlotIndex + 1;
                selectedSlotIndex = upcomingIndex >= itemCount ? 0 : selectedSlotIndex + 1;
                SelectHotbarSlot(selectedSlotIndex);
            }
        }

        private void OnSelectSlot(InputAction.CallbackContext context)
        {
            var control = context.control;

            if (control is KeyControl key)
            {
                int slotIndex = key.keyCode - Key.Digit1;
                if (slotIndex >= 0 && slotIndex < HotbarSize)
                {
                    SelectHotbarSlot(slotIndex);
                }
            }
        }

        private void SelectHotbarSlot(int hotbarSlotIndex)
        {
            int newIndex = Mathf.Clamp(hotbarSlotIndex, 0, HotbarSize - 1);
            
            if (newIndex == SelectedSlotIndex) // Ignore same calls
            {
                return;
            }

            SelectedSlotIndex = newIndex;
            SelectedSlot = Slots[newIndex];
            Debug.Log($"Selected Slot Index is: {newIndex}");
        }

        #endregion

    }

    [Serializable]
    public class InventorySlot
    {
        public static int EMPTY_SLOT_ID { get; } = -1;

        public int ItemId { get; private set; } = EMPTY_SLOT_ID;
        public int StackSize { get; private set; } = 0;

        public bool IsEmpty => ItemId == -1 || StackSize <= 0;
        
        public InventorySlot(ItemSO itemSO, int amount)
        {
            Clear();

            AssignItem(GameDataRegistry.Instance.GetItemId(itemSO), amount);
        }

        public InventorySlot(int id, int amount)
        {
            Clear();
            AssignItem(id, amount);
        }
        
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
