using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileitemSO", menuName = "OceanGame/Item/TileItemSO")]
    public class TileItemSO : ItemSO
    {
        [field: Header("Tile Item Settings")]
        [field: SerializeField] public TileDataSO PlaceTileDataSO { get; private set; }

        public override void OnPrimaryActionPressed()
        {
            var world = WorldManager.Instance;
            var mouseTilePos = WorldManager.MouseWorldTilePosition;
            var playerInRange = Vector2.Distance(Player.Instance.transform.position, WorldManager.MouseWorldPosition) < Player.Instance.PlayerInteractRange;
            
            if(!playerInRange) return;

            if (!world.FgLayer.GetTileData(mouseTilePos.x, mouseTilePos.y).HasTile)
            {
                var tileToPlace = new TileData(PlaceTileDataSO.GetId());
                world.FgLayer.SetTile(mouseTilePos.x, mouseTilePos.y, tileToPlace, true);
                InventoryInputManager.Instance.GetActiveInvSlot().RemoveFromCurrentAmount(1);
                InventoryManager.Instance.RefreshInventory();
            }

        }
        
    }
}