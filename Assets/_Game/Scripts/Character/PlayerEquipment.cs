using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class PlayerEquipment : MonoBehaviour
    {
        public event Action OnEquipmentChanged;

        public InventorySlot EquippedOxygenTank { get; private set; } = new();
        public OxygenTankItemSO CurrentOxygenTank => EquippedOxygenTank.IsEmpty ? null : GameDataRegistry.Instance.GetItemSOFromItemId(EquippedOxygenTank.ItemId) as OxygenTankItemSO;
        public Buff InAirOxygenBuff { get; private set; }
        public Buff InWaterOxygenBuff { get; private set; }

        public InventorySlot EquippedHelmet { get; private set; } = new();
        public InventorySlot EquippedChestplate { get; private set; } = new();
        public InventorySlot EquippedPants { get; private set; } = new();
        private readonly Dictionary<ArmorType, StatModifier> _appliedDefenseModifiers = new(); // Cache applied defense modifiers so they can be removed cleanly

        public ArmorItemSO GetEquippedArmorSO(ArmorType type)
        {
            var slot = GetArmorSlot(type);
            if (slot == null || slot.IsEmpty) return null;
            return GameDataRegistry.Instance.GetItemSOFromItemId(slot.ItemId) as ArmorItemSO;
        }

        public InventorySlot GetArmorSlot(ArmorType type) => type switch
        {
            ArmorType.Helmet => EquippedHelmet,
            ArmorType.Chestplate => EquippedChestplate,
            ArmorType.Pants => EquippedPants,
            _ => null
        };

        public void EquipArmor(ArmorType type, ushort itemId)
        {
            UnequipArmor(type, refresh: false); // If something was already equipped in this slot, unequip it first

            var slot = GetArmorSlot(type);
            slot.AssignItem(itemId, 1);
            var armorSO = GetEquippedArmorSO(type);
            
            if (armorSO != null && Player.Instance.Character != null)
            {
                // Apply Defense modifier to CharacterStats
                var defMod = new StatModifier(StatModifierType.Flat, armorSO.Defense);
                _appliedDefenseModifiers[type] = defMod;
                Player.Instance.Character.Stats.Defense.AddModifier(defMod);
                
                // Trigger future/unique effects lifecycle
                armorSO.OnEquip(Player.Instance.Character);
            }
            
            CheckSetBonus();
            RefreshEquipment();
        }
        public void UnequipArmor(ArmorType type, bool refresh = true)
        {
            var slot = GetArmorSlot(type);
            if (slot == null || slot.IsEmpty) return;
            var armorSO = GetEquippedArmorSO(type);
            
            if (armorSO != null && Player.Instance.Character != null)
            {
                // Remove defense modifier
                if (_appliedDefenseModifiers.TryGetValue(type, out var defMod))
                {
                    Player.Instance.Character.Stats.Defense.RemoveModifier(defMod);
                    _appliedDefenseModifiers.Remove(type);
                }
                
                // Cleanup future/unique effects
                armorSO.OnUnequip(Player.Instance.Character);
            }
            
            slot.Clear();
            CheckSetBonus();
            
            if (refresh)
            {
                RefreshEquipment();
            }
        }
        
        private void CheckSetBonus()
        {
            // Foundation for Terraria set bonuses:
            // Check if Helm, Chest, and Pants are equipped and belong to the same set.
        }

        public void EquipOxygenTank(ushort itemId)
        {
            EquippedOxygenTank.AssignItem(itemId, 1);

            InAirOxygenBuff = CurrentOxygenTank.CreateInfiniteBuffInstance();
            InWaterOxygenBuff = CurrentOxygenTank.CreateFiniteBuffInstance();

            RefreshEquipment();
        }

        public void UnequipOxygenTank()
        {
            EquippedOxygenTank.Clear();
            
            Player.Instance.Character.Stats.StopBuff(InAirOxygenBuff);
            Player.Instance.Character.Stats.StopBuff(InWaterOxygenBuff);

            InAirOxygenBuff = null;
            InWaterOxygenBuff = null;

            RefreshEquipment();
        }

        public void RefreshEquipment()
        {
            OnEquipmentChanged?.Invoke();
            InventoryCursorManager.Instance.RefreshCursorSlot();
        }
    }
}
