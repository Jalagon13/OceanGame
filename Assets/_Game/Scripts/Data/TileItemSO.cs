using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileitemSO", menuName = "OceanGame/Item/TileItemSO")]
    public class TileItemSO : ItemSO
    {
        [field: Header("Tile Item Settings")]
        [field: SerializeField] public TileConfigSO PlaceTileDataSO { get; private set; }

        public override void OnPrimaryActionPressed()
        {
            var world = WorldManager.Instance;
            var pos = WorldManager.MouseWorldTilePosition;
            var playerInRange = Vector2.Distance(Player.Instance.transform.position, WorldManager.MouseWorldPosition) < Player.Instance.PlayerInteractRange;
            
            if(!playerInRange) return;

            if (!world.FgLayer.GetTileData(pos.x, pos.y).HasTile)
            {
                var tileToPlaceTd = new TileData(PlaceTileDataSO.GetId(), isSolid: PlaceTileDataSO.IsSolid);
                
                if(tileToPlaceTd.TileConfig.IsMultiTile)
                {
                    world.FgLayer.PlaceMultiTileData(pos.x, pos.y, tileToPlaceTd, true);    
                }
                else
                {
                    world.FgLayer.SetTileData(pos.x, pos.y, tileToPlaceTd, true);
                }
                
                InventoryInputManager.Instance.GetActiveInvSlot().RemoveFromCurrentAmount(1);
                InventoryManager.Instance.RefreshInventory();
            }

        }
        
    }
}