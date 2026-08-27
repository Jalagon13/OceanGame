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
            var playerInRange = Vector2.Distance(Player.Instance.transform.position, WorldManager.MouseWorldPosition) < Player.Instance.InteractRange;

            if (!playerInRange) return;

            if (world.FgLayer.GetTileData(mouseTilePos.x, mouseTilePos.y).HasTile)
            {
                world.DamageTile(mouseTilePos, 10, WorldManager.LayerType.Foreground, true);
            }

        }
    }
}