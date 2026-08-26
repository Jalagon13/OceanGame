using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OceanGame
{
    public class InventoryDisplayUI : MonoBehaviour
    {
        [SerializeField] private InventorySlotUI _inventorySlotPrefab;
        [SerializeField] private Transform _hotbarSlotsHolder;
        [SerializeField] private Transform _inventorySlotsHolder;
        [SerializeField] private Transform _inventoryUI;
        [SerializeField] private Image _hotbarSelectionImage;
        
        [Header("Crafting Menu")]
        [SerializeField] private CraftingSlotUI _craftingSlotPrefab;
        [SerializeField] private Transform _externalCraftMenuUI;
        [SerializeField] private Transform _externalCraftingSlotsHolder;
        
        [Header("Default Crafting Menu")]
        [SerializeField] private Transform _defaultCraftSlotsHolder;
        [SerializeField] private List<RecipeSO> _defaultCraftingRecipes;
        
        private Vector2 _interactedCtPos;
    
        private IEnumerator Start()
        {
            InventoryInputManager.Instance.OnInventoryOpenChanged += ToggleInventoryUI;
            InventoryInputManager.Instance.OnActiveHotbarIndexChanged += UpdateHotbarUI;
            InventoryInputManager.Instance.OnCraftTableInteract += ShowCraftingMenu;

            CloseInventoryUI();
            InitializeSlots();
            
            yield return null;

            UpdateHotbarUI(InventoryInputManager.Instance.ActiveHotbarIndex);
        }

        private void OnDestroy()
        {
            InventoryInputManager.Instance.OnInventoryOpenChanged -= ToggleInventoryUI;
            InventoryInputManager.Instance.OnActiveHotbarIndexChanged -= UpdateHotbarUI;
            InventoryInputManager.Instance.OnCraftTableInteract -= ShowCraftingMenu;
        }
        
        private void Update() 
        {
            if(_externalCraftMenuUI.gameObject.activeInHierarchy)
            {
                float distance = Vector2.Distance(Player.Instance.transform.position, _interactedCtPos);
                
                if(distance > Player.Instance.InteractRange)
                {
                    CloseExternalCraftingMenuUI();
                }
            }
        }

        private void ShowCraftingMenu(List<RecipeSO> recipes, int x, int y)
        {
            _interactedCtPos = new Vector2(x + 0.5f, y + 0.5f);
        
            // Destroy any children left
            for (int i = _externalCraftingSlotsHolder.childCount - 1; i >= 0; i--)
            {
                Destroy(_externalCraftingSlotsHolder.GetChild(i).gameObject);
            }

            // Populate craftMenu
            foreach (var recipe in recipes)
            {
                var cSlot = Instantiate(_craftingSlotPrefab, _externalCraftingSlotsHolder);
                cSlot.Initialize(recipe);
            }

            ShowExternalCraftingMenuUI();
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
                var cSlot = Instantiate(_craftingSlotPrefab, _defaultCraftSlotsHolder);
                cSlot.Initialize(recipe);
            }
            
            
        }

        private void UpdateHotbarUI(int activeHotbarIndex)
        {
            Transform child = _hotbarSlotsHolder.GetChild(activeHotbarIndex);
            _hotbarSelectionImage.transform.position = child.transform.position;
        }

        private void ToggleInventoryUI(bool isInventoryOpen)
        {
            if(isInventoryOpen)
            {
                ShowInventoryUI();
            }
            else
            {
                CloseExternalCraftingMenuUI();
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
        
        private void ShowExternalCraftingMenuUI()
        {
            _externalCraftMenuUI.gameObject.SetActive(true);
        }
        
        private void CloseExternalCraftingMenuUI()
        {
            _externalCraftMenuUI.gameObject.SetActive(false);
        }
    }
}
