using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileitemSO", menuName = "OceanGame/Item/TileItemSO")]
    public class TileItemSO : ItemSO
    {
        [field: Header("Tile Settings")]
        [field: SerializeField] public TileSO Tile { get; private set; }
        [field: SerializeField] public ItemSO Drop { get; private set; }

        public override void OnPrimaryActionPressed()
        {
            var world = WorldManager.Instance;
            var mouseTilePos = WorldManager.MouseWorldTilePosition;
            var playerInRange = Vector2.Distance(Player.Instance.transform.position, WorldManager.MouseWorldPosition) < Player.Instance.PlayerInteractRange;
            
            if(!playerInRange) return;

            if (!world.ForegroundLayer.GetTileData(mouseTilePos.x, mouseTilePos.y).HasTile)
            {
                world.ForegroundLayer.SetTile(mouseTilePos.x, mouseTilePos.y, new(Tile.GetId()), true);
                InventoryInputManager.Instance.GetActiveInvSlot().RemoveFromCurrentAmount(1);
                InventoryManager.Instance.RefreshInventory();
            }

        }
    }
}