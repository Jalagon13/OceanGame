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

            if (world.ForegroundLayer.HasTileAt(mouseTilePos.x, mouseTilePos.y))
            {
                var tileBroken = world.ForegroundLayer.GetItemSO(mouseTilePos.x, mouseTilePos.y);
                world.ForegroundLayer.SetTile(mouseTilePos.x, mouseTilePos.y, TileLayer.AIR_ID, true);
                GameManager.Instance.SpawnItem(tileBroken, 1, mouseTilePos + new Vector2(0.5f, 0.5f));
            }

        }
    }
}