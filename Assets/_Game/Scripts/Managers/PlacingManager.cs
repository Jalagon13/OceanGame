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

        private void Start()
        {
            if (Player.Instance == null) return;

            Player.Instance.PlayerReady += OnPlayerReady;

            if (Player.Instance.Character != null)
                OnPlayerReady(Player.Instance.Character);
        }

        private void OnDestroy()
        {
            if (Player.Instance == null) return;

            if(Player.Instance.Character != null)
                Player.Instance.Character.Health.CurrentLifeState.OnValueChanged -= OnLifeStateChanged;
                
            Player.Instance.PlayerReady -= OnPlayerReady;
        }

        private void OnPlayerReady(ServerCharacter player)
        {
            player.Health.CurrentLifeState.OnValueChanged += OnLifeStateChanged;
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if (newValue == LifeState.Dead)
            {
                StopPlacing();
            }
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

            float distanceToPlayer = Vector2.Distance(Player.Instance.Character.transform.position, mouseWorldPos);
            if (distanceToPlayer > Player.Instance.InteractRange) return false;

            var world = WorldManager.Instance;
            var activeLayer = world.ActiveLayer;
            var targetGrid = world.ActiveGrid;
            var tileConfig = tileItem.PlaceTileDataSO;

            // Only 1x1 non-multitile tiles can be placed on the background layer
            if (activeLayer == WorldManager.LayerType.Background && tileConfig.IsMultiTile) return false;

            // Check if the targeted layer already has a tile at this cell
            var targetTileData = targetGrid.GetTileData(mouseTilePos.x, mouseTilePos.y);
            if(targetTileData.HasTile) return false;

            Vector2Int placementSize = tileConfig.Size;

            if (HasEntityOverlappingTileArea(mouseTilePos, placementSize))
            {
                return false;
            }

            bool isBackgroundPlacement = activeLayer == WorldManager.LayerType.Background;
            var supportGrid = isBackgroundPlacement ? world.BgGrid : world.FgGrid;

            // Just the complicated rules for when you can place a tile as it is now
            bool hasSolidNeighbor =
                IsSolidTile(supportGrid, mouseTilePos.x, mouseTilePos.y + 1) ||
                IsSolidTile(supportGrid, mouseTilePos.x, mouseTilePos.y - 1) ||
                IsSolidTile(supportGrid, mouseTilePos.x - 1, mouseTilePos.y) ||
                IsSolidTile(supportGrid, mouseTilePos.x + 1, mouseTilePos.y);

            bool hasBackgroundNeighbor =
                world.BgGrid.GetTileData(mouseTilePos.x, mouseTilePos.y + 1).HasTile ||
                world.BgGrid.GetTileData(mouseTilePos.x, mouseTilePos.y - 1).HasTile ||
                world.BgGrid.GetTileData(mouseTilePos.x - 1, mouseTilePos.y).HasTile ||
                world.BgGrid.GetTileData(mouseTilePos.x + 1, mouseTilePos.y).HasTile;

            bool hasForegroundNeighbor =
                world.FgGrid.GetTileData(mouseTilePos.x, mouseTilePos.y + 1).HasTile ||
                world.FgGrid.GetTileData(mouseTilePos.x, mouseTilePos.y - 1).HasTile ||
                world.FgGrid.GetTileData(mouseTilePos.x - 1, mouseTilePos.y).HasTile ||
                world.FgGrid.GetTileData(mouseTilePos.x + 1, mouseTilePos.y).HasTile;

            bool canPlaceBesideForeground = isBackgroundPlacement && !hasBackgroundNeighbor && hasForegroundNeighbor;
            bool isBehindForegroundTile = world.FgGrid.GetTileData(mouseTilePos.x, mouseTilePos.y).HasTile;
            bool isInFrontOfBackgroundTile = !isBackgroundPlacement && world.BgGrid.GetTileData(mouseTilePos.x, mouseTilePos.y).HasTile;
            bool canPlaceBesideBackground = !isBackgroundPlacement && hasBackgroundNeighbor;
            bool hasPlacementSupport = hasSolidNeighbor || (isBackgroundPlacement && isBehindForegroundTile) || canPlaceBesideForeground || isInFrontOfBackgroundTile || canPlaceBesideBackground;

            // If no support, do not do that
            if (!hasPlacementSupport) return false;

            // If everything is good then we can proceed to place the tile
            var tileToPlaceTd = new TileData(tileConfig.GetId());

            if (activeLayer == WorldManager.LayerType.Foreground && tileConfig.IsMultiTile)
            {
                world.FgGrid.PlaceMultiTileData(mouseTilePos.x, mouseTilePos.y, tileToPlaceTd, refreshCurrentBounds: true, manualyPlaced: true);
            }
            else
            {
                targetGrid.SetTileData(mouseTilePos.x, mouseTilePos.y, tileToPlaceTd, refreshCurrentBounds: true, manualyPlaced: true);
            }

            InventoryInputManager.Instance.GetActiveInvSlot().RemoveFromCurrentAmount(1);
            InventoryManager.Instance.RefreshInventory();

            float interval = _placementsPerSecond > 0f ? 1f / _placementsPerSecond : 0.25f;
            _nextPlaceTime = Time.time + interval;
            
            return true;
        }

        private static bool HasEntityOverlappingTileArea(Vector2Int tilePosition, Vector2Int tileSize)
        {
            var entityManager = EntityManager.Instance;

            // Don't allow placement when entity occupancy can't be checked.
            if (entityManager == null) return true;

            Vector2 tileAreaCenter = new(tilePosition.x + tileSize.x * 0.5f, tilePosition.y + tileSize.y * 0.5f);
            Vector2 tileAreaSize = new(tileSize.x, tileSize.y);
            var entities = entityManager.Entities;

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                if (entity == null) continue;

                if (GridPhysics.IsOverlapping(tileAreaCenter, tileAreaSize, entity.transform.position, entity.ColliderSize))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSolidTile(TileGrid grid, int x, int y)
        {
            var tile = grid.GetTileData(x, y);
            return tile.HasTile && tile.IsSolid;
        }
    }
}
