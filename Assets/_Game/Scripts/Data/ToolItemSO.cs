using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New ToolItemSO", menuName = "OceanGame/Item/ToolItemSO")]
    public class ToolItemSO : ItemSO
    {
        // [field: Header("Tool Settings")]
        // [field: SerializeField] public TileSO Tile { get; private set; }

        public override void OnPrimaryActionPressed()
        {
            var world = WorldManager.Instance;
            var mouseTilePos = WorldManager.MouseWorldTilePosition;
            var playerInRange = Vector2.Distance(Player.Instance.transform.position, WorldManager.MouseWorldPosition) < Player.Instance.PlayerInteractRange;

            if (!playerInRange) return;

            if (world.FgLayer.GetTileData(mouseTilePos.x, mouseTilePos.y).HasTile)
            {
                var droppedItem = world.FgLayer.GetTileData(mouseTilePos.x, mouseTilePos.y).TileConfig.DroppedItem;
                world.FgLayer.DestroyTile(mouseTilePos.x, mouseTilePos.y, true);
                
                if(droppedItem != null)
                {
                    GameManager.Instance.SpawnItem(droppedItem, 1, mouseTilePos + new Vector2(0.5f, 0.5f));
                }
            }

        }
    }
}