using UnityEngine;

namespace OceanGame
{
    public class InventoryDisplayUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI _inventorySlotPrefab;
        [SerializeField] private Transform _hotbarSlotsHolder;
        [SerializeField] private Transform _inventorySlotsHolder;
    
        private void Start()
        {
            if(_inventorySlotPrefab == null) return;
        
            var inventorySize = InventoryManager.Instance.InventorySize;
            var hotbarSize = InventoryManager.Instance.HotbarSize;
            
            for (int i = 0; i < inventorySize; i++)
            {
                if(i < hotbarSize)
                {
                    // Initialize HotbarSlot
                    var slot = Instantiate(_inventorySlotPrefab, _hotbarSlotsHolder);
                    slot.Initialize(i);
                }
                else
                {
                    // Initialize Inventory Slot
                    var slot = Instantiate(_inventorySlotPrefab, _inventorySlotsHolder);
                    slot.Initialize(i);
                }
            }
        }
        
    }
}
