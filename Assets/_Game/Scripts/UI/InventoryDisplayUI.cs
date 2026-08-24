using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class InventoryDisplayUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI _inventorySlotPrefab;
        [SerializeField] private Transform _hotbarSlotsHolder;
        [SerializeField] private Transform _inventorySlotsHolder;
        [SerializeField] private Transform _inventoryUI;
        
        [Header("Default Crafting Menu")]
        [SerializeField] private CraftingSlotUI _craftingSlotPrefab;
        [SerializeField] private Transform _craftingSlotsHolder;
        [SerializeField] private List<RecipeSO> _defaultCraftingRecipes;
    
        private void Start()
        {
            InventoryInputManager.Instance.OnInventoryOpenChanged += ToggleInventoryUI;

            CloseInventoryUI();
            InitializeSlots();
        }

        private void OnDestroy()
        {
            InventoryInputManager.Instance.OnInventoryOpenChanged -= ToggleInventoryUI;
        }
        
        private void InitializeSlots()
        {
            // Initialize Inventory and Hotbar slots
            var inventorySize = InventoryManager.Instance.InventorySize;
            var hotbarSize = InventoryManager.Instance.HotbarSize;

            for (int i = 0; i < inventorySize; i++)
            {
                if (i < hotbarSize)
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

            // Initalize Default Crafting Menu Crafing Slots
            foreach (var recipe in _defaultCraftingRecipes)
            {
                var cSlot = Instantiate(_craftingSlotPrefab, _craftingSlotsHolder);
                cSlot.Initialize(recipe);
            }
        }

        private void ToggleInventoryUI(bool isInventoryOpen)
        {
            if(isInventoryOpen)
            {
                ShowInventoryUI();
            }
            else
            {
                CloseInventoryUI();
            }
        }

        private void ShowInventoryUI()
        {
            _inventoryUI.gameObject.SetActive(true);
        }

        private void CloseInventoryUI()
        {
            _inventoryUI.gameObject.SetActive(false); ;
        }
    }
}
