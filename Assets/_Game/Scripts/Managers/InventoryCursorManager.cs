using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        public void AssignCursorSlot(ushort itemId, int amount)
        {
            CursorSlot.AssignItem(itemId, amount);
            InventoryManager.Instance.RefreshInventory();
        }

        private void TryToThrowCursorSlotItem(InputAction.CallbackContext context)
        {
            if(CursorSlot.IsEmpty || WorldManager.Instance.MouseOverUI || context.phase != InputActionPhase.Started) return;
            
            Vector2 aimDirection = WorldManager.MouseWorldPosition - (Vector2)Player.Instance.Character.transform.position;
            aimDirection.Normalize();
            aimDirection *= _throwItemForce;
            
            GameManager.Instance.SpawnItem(GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId), CursorSlot.CurrentAmount, Player.Instance.transform.position, aimDirection);
            CursorSlot.Clear();

            InventoryManager.Instance.RefreshInventory();
        }

        // Generic slot handlers that work for Player, Chest, or any container
        public void HandleSlotLeftClick(InventorySlot slot, Action onSlotModified = null)
        {
            if (slot == null) return;
            if (CursorSlot.IsEmpty && slot.IsEmpty) return;
            if (CursorSlot.IsEmpty)
            {
                CursorSlot = slot.Clone();
                slot.Clear();
            }
            else if (slot.IsEmpty)
            {
                slot.AssignItem(CursorSlot.ItemId, CursorSlot.CurrentAmount);
                CursorSlot.Clear();
            }
            else if (CanStacksMerge(slot, CursorSlot))
            {
                int maxStack = GetMaxStackSize(GameDataRegistry.Instance.GetItemSOFromItemId(slot.ItemId));
                MoveAmount(CursorSlot, slot, maxStack);
            }
            else
            {
                // Swap slot contents safely
                ushort tempId = slot.ItemId;
                int tempAmount = slot.CurrentAmount;
                slot.AssignItem(CursorSlot.ItemId, CursorSlot.CurrentAmount);
                CursorSlot.AssignItem(tempId, tempAmount);
            }
            onSlotModified?.Invoke();
            InventoryManager.Instance.RefreshInventory();
        }
        public void HandleSlotRightClick(InventorySlot slot, Action onSlotModified = null)
        {
            if (slot == null) return;
            if (CursorSlot.IsEmpty && slot.IsEmpty) return;
            if (CursorSlot.IsEmpty)
            {
                int cursorAmount = Mathf.CeilToInt(slot.CurrentAmount * 0.5f);
                CursorSlot.AssignItem(slot.ItemId, cursorAmount);
                slot.RemoveFromCurrentAmount(cursorAmount);
            }
            else if (slot.IsEmpty)
            {
                slot.AssignItem(CursorSlot.ItemId, 1);
                CursorSlot.RemoveFromCurrentAmount(1);
            }
            else if (CanStacksMerge(slot, CursorSlot))
            {
                slot.AddToCurrentAmount(1);
                CursorSlot.RemoveFromCurrentAmount(1);
            }
            onSlotModified?.Invoke();
            InventoryManager.Instance.RefreshInventory();
        }

        public void HandleOxygenTankSlotLeftClick()
        {
            if (Player.Instance == null || Player.Instance.Equipment == null) return;

            InventorySlot equipSlot = Player.Instance.Equipment.EquippedOxygenTank;
            
            if (equipSlot == null) return;
            if (CursorSlot.IsEmpty && equipSlot.IsEmpty) return;

            if (CursorSlot.IsEmpty)
            {
                ushort unequippedItemId = equipSlot.ItemId;
                CursorSlot.AssignItem(unequippedItemId, 1);
                Player.Instance.Equipment.UnequipOxygenTank();
            }
            else if (equipSlot.IsEmpty)
            {
                ItemSO itemSO = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                
                if (itemSO is not OxygenTankItemSO) return;
                
                ushort itemIdToEquip = CursorSlot.ItemId;
                CursorSlot.RemoveFromCurrentAmount(1);
                Player.Instance.Equipment.EquipOxygenTank(itemIdToEquip);
            }
            else
            {
                ItemSO cursorItemSO = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                
                if (cursorItemSO is not OxygenTankItemSO) return;
                
                if (CursorSlot.CurrentAmount == 1)
                {
                    ushort oldEquippedItemId = equipSlot.ItemId;
                    ushort newEquippedItemId = CursorSlot.ItemId;

                    CursorSlot.AssignItem(oldEquippedItemId, 1);
                    Player.Instance.Equipment.EquipOxygenTank(newEquippedItemId);
                }
            }
        }

        public void HandleOxygenTankSlotRightClick()
        {
            if (Player.Instance == null || Player.Instance.Equipment == null) return;

            InventorySlot equipSlot = Player.Instance.Equipment.EquippedOxygenTank;
            
            if (equipSlot == null) return;
            if (CursorSlot.IsEmpty && equipSlot.IsEmpty) return;

            if (CursorSlot.IsEmpty)
            {
                ushort unequippedItemId = equipSlot.ItemId;
                CursorSlot.AssignItem(unequippedItemId, 1);
                Player.Instance.Equipment.UnequipOxygenTank();
            }
            else if (equipSlot.IsEmpty)
            {
                ItemSO itemSO = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                if (itemSO is not OxygenTankItemSO) return;

                ushort itemIdToEquip = CursorSlot.ItemId;
                CursorSlot.RemoveFromCurrentAmount(1);
                Player.Instance.Equipment.EquipOxygenTank(itemIdToEquip);
            }
            else
            {
                ItemSO cursorItemSO = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                if (cursorItemSO is not OxygenTankItemSO) return;

                if (CursorSlot.CurrentAmount == 1)
                {
                    ushort oldEquippedItemId = equipSlot.ItemId;
                    ushort newEquippedItemId = CursorSlot.ItemId;

                    CursorSlot.AssignItem(oldEquippedItemId, 1);
                    Player.Instance.Equipment.EquipOxygenTank(newEquippedItemId);
                }
            }
        }

        public void HandleArmorSlotLeftClick(ArmorType armorType)
        {
            if (Player.Instance == null || Player.Instance.Equipment == null) return;
            var equipSlot = Player.Instance.Equipment.GetArmorSlot(armorType);
            if (equipSlot == null) return;
            if (CursorSlot.IsEmpty && equipSlot.IsEmpty) return;
            if (CursorSlot.IsEmpty)
            {
                // Unequip armor to cursor
                ushort unequippedItemId = equipSlot.ItemId;
                CursorSlot.AssignItem(unequippedItemId, 1);
                Player.Instance.Equipment.UnequipArmor(armorType);
            }
            else if (equipSlot.IsEmpty)
            {
                // Equip from cursor if type matches
                ItemSO cursorItem = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                if (cursorItem is not ArmorItemSO armorSO || armorSO.ArmorPieceType != armorType) return;
                ushort itemIdToEquip = CursorSlot.ItemId;
                CursorSlot.RemoveFromCurrentAmount(1);
                Player.Instance.Equipment.EquipArmor(armorType, itemIdToEquip);
            }
            else
            {
                // Swap currently equipped armor with cursor item
                ItemSO cursorItem = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                if (cursorItem is not ArmorItemSO armorSO || armorSO.ArmorPieceType != armorType) return;
                if (CursorSlot.CurrentAmount == 1)
                {
                    ushort oldEquippedId = equipSlot.ItemId;
                    ushort newEquippedId = CursorSlot.ItemId;
                    CursorSlot.AssignItem(oldEquippedId, 1);
                    Player.Instance.Equipment.EquipArmor(armorType, newEquippedId);
                }
            }
        }

        public void HandleArmorSlotRightClick(ArmorType armorType)
        {
            // Right click in Terraria or slot can quickly unequip to cursor or inventory
            HandleArmorSlotLeftClick(armorType);
        }

        public void HandleAccessorySlotLeftClick(int slotIndex)
        {
            if (Player.Instance == null || Player.Instance.Equipment == null) return;
            
            var equipSlot = Player.Instance.Equipment.GetAccessorySlot(slotIndex);
            if (equipSlot == null) return;
            
            if (CursorSlot.IsEmpty && equipSlot.IsEmpty) return;
            
            if (CursorSlot.IsEmpty)
            {
                // Unequip accessory to cursor
                ushort unequippedItemId = equipSlot.ItemId;
                CursorSlot.AssignItem(unequippedItemId, 1);
                Player.Instance.Equipment.UnequipAccessory(slotIndex);
            }
            else if (equipSlot.IsEmpty)
            {
                // Validate cursor item is an AccessoryItemSO
                ItemSO cursorItem = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                if (cursorItem is not AccessoryItemSO) return;
                
                // Terraria Rule: Prevent duplicate accessories
                if (Player.Instance.Equipment.IsAccessoryEquipped(CursorSlot.ItemId, ignoreIndex: slotIndex))
                {
                    // Debug.LogWarning("Cannot equip duplicate accessories!");
                    return;
                }
                
                ushort itemIdToEquip = CursorSlot.ItemId;
                CursorSlot.RemoveFromCurrentAmount(1);
                
                Player.Instance.Equipment.EquipAccessory(slotIndex, itemIdToEquip);
            }
            else
            {
                // Swap currently equipped accessory with cursor item
                ItemSO cursorItem = GameDataRegistry.Instance.GetItemSOFromItemId(CursorSlot.ItemId);
                if (cursorItem is not AccessoryItemSO) return;
                
                if (Player.Instance.Equipment.IsAccessoryEquipped(CursorSlot.ItemId, ignoreIndex: slotIndex))
                {
                    // Debug.LogWarning("Cannot equip duplicate accessories!");
                    return;
                }
                
                if (CursorSlot.CurrentAmount == 1)
                {
                    ushort oldEquippedId = equipSlot.ItemId;
                    ushort newEquippedId = CursorSlot.ItemId;
                    
                    CursorSlot.AssignItem(oldEquippedId, 1);
                    Player.Instance.Equipment.EquipAccessory(slotIndex, newEquippedId);
                }
            }
        }
        
        public void HandleAccessorySlotRightClick(int slotIndex)
        {
            HandleAccessorySlotLeftClick(slotIndex);
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
