using System;
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

        public void EquipOxygenTank(ushort itemId)
        {
            Debug.Log($"Oxygen Tank Equipped");
            EquippedOxygenTank.AssignItem(itemId, 1);

            InAirOxygenBuff = CurrentOxygenTank.CreateInfiniteBuffInstance();
            InWaterOxygenBuff = CurrentOxygenTank.CreateFiniteBuffInstance();

            RefreshEquipment();
        }

        public void UnequipOxygenTank()
        {
            Debug.Log($"Oxygen Tank UnEquipped");
            EquippedOxygenTank.Clear();

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
