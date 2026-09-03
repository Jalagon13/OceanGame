using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class CrafterIB : InteractBehavior
    {
        [field: Header("Craft Table Tile Settings")]
        [field: SerializeField] public List<RecipeSO> Recipes { get; private set; }

        public override void Interact(int posX, int posY)
        {
            Debug.Log($"Interacting with Crafting Table");
            InventoryInputManager.Instance.OnInteractWithCraftingTable(Recipes, posX, posY);
        }
    }
}