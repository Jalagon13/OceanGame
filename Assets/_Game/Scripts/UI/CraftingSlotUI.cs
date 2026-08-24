using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OceanGame
{
    public class CraftingSlotUI : MonoBehaviour
    {
        [SerializeField] private Button _craftButton;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private Image _greyOverlay;
        [SerializeField] private TextMeshProUGUI _stackText;
        
        private RecipeSO _recipe;

        private void OnDestroy()
        {
            InventoryManager.Instance.OnPlayerInventoryChanged -= RefreshUI;
        }

        public void Initialize(RecipeSO recipe)
        {
            _recipe = recipe;

            InventoryManager.Instance.OnPlayerInventoryChanged += RefreshUI;

            _craftButton.onClick.AddListener(OnCraftButtonClicked);

            _itemIcon.sprite = recipe.OutputItem.DisplayIcon;
            _stackText.text = recipe.OutputAmonut > 1 ? recipe.OutputAmonut.ToString() : string.Empty;

            RefreshUI();
        }

        private void OnCraftButtonClicked()
        {
            var inventory = InventoryManager.Instance;
        
            if(inventory.CanCraftRecipe(_recipe))
            {
                if(!inventory.IsInventoryFull() && inventory.CanAcceptItem(_recipe.OutputItem.GetId(), _recipe.OutputAmonut)) // NTFS: Later on I need to make it so canacceptitem is true even if i do not have the space but if i craft the item and i consume the ingredients THEN I will have space
                {
                    var outputId = GameDataRegistry.Instance.GetItemIdFromItemSO(_recipe.OutputItem);

                    foreach (var ingredient in _recipe.Recipe)
                    {
                        inventory.RemoveItem(ingredient.Item.GetId(), ingredient.Amount);
                    }

                    inventory.AddItem(outputId, _recipe.OutputAmonut);
                }
            
                
            }
        }

        private void RefreshUI()
        {
            var inventory = InventoryManager.Instance;

            if (inventory.CanCraftRecipe(_recipe))
            {
                _greyOverlay.enabled = false;
            }
            else
            {
                _greyOverlay.enabled = true;
            }
        }
    }
}