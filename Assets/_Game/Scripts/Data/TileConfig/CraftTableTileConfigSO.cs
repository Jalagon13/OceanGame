using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New CraftTableTileConfigSO", menuName = "OceanGame/TileConfig/CraftTableTileConfigSO")]
    public class CraftTableTileConfigSO : TileConfigSO, IInteractable
    {
        [field: Header("Craft Table Tile Settings")]
        [field: SerializeField] public List<RecipeSO> Recipes { get; private set; }

        public void OnInteract(int x, int y)
        {
            Debug.Log($"Interacting with Crafting Table");
            InventoryInputManager.Instance.OnInteractWithCraftingTable(Recipes, x, y);
            
            
        }
    }
}
