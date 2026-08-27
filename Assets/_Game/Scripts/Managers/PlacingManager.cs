using UnityEngine;

namespace OceanGame
{
    public class PlacingManager : MonoBehaviour
    {
        public static PlacingManager Instance { get; private set; }

        [SerializeField] private float _placementsPerSecond = 4f;

        private float _nextPlaceTime;
        private bool _isPlacing;

        private void Awake()
        {
            Instance = this;
        }

        public void StartPlacing(TileItemSO tileItem)
        {
            _isPlacing = true;
            TryPerformPlace(tileItem); // Instantly attempt placement on first click
        }

        public void TickPlacing(TileItemSO tileItem)
        {
            if (!_isPlacing) return;

            TryPerformPlace(tileItem);
        }

        public void StopPlacing()
        {
            _isPlacing = false;
        }

        private bool TryPerformPlace(TileItemSO tileItem)
        {
            if (tileItem == null || tileItem.PlaceTileDataSO == null) return false;
            if (Time.time < _nextPlaceTime) return false;
            if (WorldManager.Instance == null || WorldManager.Instance.MouseOverUI) return false;

            Vector2 mouseWorldPos = WorldManager.MouseWorldPosition;
            Vector2Int mouseTilePos = WorldManager.MouseWorldTilePosition;

            float distanceToPlayer = Vector2.Distance(Player.Instance.transform.position, mouseWorldPos);
            if (distanceToPlayer > Player.Instance.InteractRange) return false;

            var world = WorldManager.Instance;
            var targetTileData = world.FgLayer.GetTileData(mouseTilePos.x, mouseTilePos.y);

            if (!targetTileData.HasTile)
            {
                var tileToPlaceTd = new TileData(tileItem.PlaceTileDataSO.GetId(), isSolid: tileItem.PlaceTileDataSO.IsSolid);

                if (tileToPlaceTd.TileConfig != null && tileToPlaceTd.TileConfig.IsMultiTile)
                {
                    world.FgLayer.PlaceMultiTileData(mouseTilePos.x, mouseTilePos.y, tileToPlaceTd, refreshCurrentBounds: true);
                }
                else
                {
                    world.FgLayer.SetTileData(mouseTilePos.x, mouseTilePos.y, tileToPlaceTd, refreshCurrentBounds: true);
                }

                InventoryInputManager.Instance.GetActiveInvSlot().RemoveFromCurrentAmount(1);
                InventoryManager.Instance.RefreshInventory();

                float interval = _placementsPerSecond > 0f ? 1f / _placementsPerSecond : 0.25f;
                _nextPlaceTime = Time.time + interval;
                return true;
            }

            return false;
        }
    }
}
