using System;
using UnityEngine;

namespace OceanGame
{
    public class InventoryCursorManager : MonoBehaviour
    {
        public static InventoryCursorManager Instance { get; private set; }
        
        public event Action OnCursorSlotChanged;
        
        [SerializeField] private float _throwItemForce = 15;

        public InventorySlot CursorSlot { get; private set; } = new();

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start() 
        {
            GameInput.Instance.OnSecondaryActionPressed += TryToThrowCursorSlotItem;
        }
        
        private void OnDestroy()
        {
            GameInput.Instance.OnSecondaryActionPressed -= TryToThrowCursorSlotItem;
        }

        public void RefreshCursorSlot()
        {
            OnCursorSlotChanged?.Invoke();
        }

        public void AssignCursorSlot(int itemId, int amount)
        {
            CursorSlot.AssignItem(itemId, amount);
            InventoryManager.Instance.RefreshInventory();
        }

        private void TryToThrowCursorSlotItem()
        {
            if(CursorSlot.IsEmpty || WorldManager.Instance.MouseOverUI) return;
            
            Vector2 aimDirection = WorldManager.MouseWorldPosition - (Vector2)Player.Instance.transform.position;
            aimDirection.Normalize();
            aimDirection *= _throwItemForce;
            
            GameManager.Instance.SpawnItem(GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId), CursorSlot.CurrentAmount, Player.Instance.transform.position, aimDirection);
            CursorSlot.Clear();

            InventoryManager.Instance.RefreshInventory();
        }

        public void HandleSlotLeftClick(int clickedIndex)
        {
            InventorySlot[] inventory = InventoryManager.Instance.PlayerInventory;

            if (CursorSlot.IsEmpty && inventory[clickedIndex].IsEmpty) return; // Garentees there is at least 1 non empty slot

            if (CursorSlot.IsEmpty) // Cursor is empty and clicked slot has an item
            {
                CursorSlot = inventory[clickedIndex].Clone();
                inventory[clickedIndex].Clear();
            }
            else if (inventory[clickedIndex].IsEmpty) // Clicked slot is empty and cursor has an item
            {
                inventory[clickedIndex] = CursorSlot.Clone();
                CursorSlot.Clear();
            }
            else if (CanStacksMerge(inventory[clickedIndex], CursorSlot)) // Checks if they are both the same item
            {
                int maxStack = GetMaxStackSize(GameDataRegistry.Instance.GetItemSOFromItemId(inventory[clickedIndex].ItemId));
                MoveAmount(CursorSlot, inventory[clickedIndex], maxStack);
            }
            else 
            {
                // Swap items safely using the cached array
                InventorySlot swappedItem = inventory[clickedIndex].Clone();
                inventory[clickedIndex] = CursorSlot.Clone();
                CursorSlot = swappedItem;
            }

            InventoryManager.Instance.RefreshInventory();
        }


        public void HandleSlotRightClick(int clickedIndex)
        {
            InventorySlot[] inventory = InventoryManager.Instance.PlayerInventory;

            if (CursorSlot.IsEmpty && inventory[clickedIndex].IsEmpty) return; // Garentees there is at least 1 non empty slot

            if (CursorSlot.IsEmpty) // Cursor is empty and clicked slot has an item
            {
                // split the stack in half
                int cursorAmount = Mathf.CeilToInt(inventory[clickedIndex].CurrentAmount * 0.5f);
                CursorSlot.AssignItem(inventory[clickedIndex].ItemId, cursorAmount);
                inventory[clickedIndex].RemoveFromCurrentAmount(cursorAmount);
            }
            else if (inventory[clickedIndex].IsEmpty) // Clicked slot is empty and cursor has an item
            {
                inventory[clickedIndex].AssignItem(CursorSlot.ItemId, 1);
                CursorSlot.RemoveFromCurrentAmount(1);
            }
            else if (CanStacksMerge(inventory[clickedIndex], CursorSlot)) // Checks if they are both the same item
            {
                inventory[clickedIndex].AddToCurrentAmount(1);
                CursorSlot.RemoveFromCurrentAmount(1);
            }

            InventoryManager.Instance.RefreshInventory();
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
                target.CurrentAmount < GetMaxStackSize(GameDataRegistry.Instance.GetItemSOFromItemId(target.ItemId));
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
