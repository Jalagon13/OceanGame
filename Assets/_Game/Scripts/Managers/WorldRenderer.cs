using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldRenderer : MonoBehaviour
    {
        public static WorldRenderer Instance { get; private set; }
        public event Action<RectInt> OnVisibleTileBoundsChanged;

        [SerializeField] private Camera _mainCamera;
        [SerializeField] private int _padding = 4;

        public RectInt CurrentVisibleTileBounds { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void LateUpdate()
        {
            int padding = _padding;
            Vector2 bottomLeft = _mainCamera.ViewportToWorldPoint(new Vector2(0, 0));
            Vector2 topRight = _mainCamera.ViewportToWorldPoint(new Vector2(1, 1));

            int minX = Mathf.FloorToInt(bottomLeft.x) - padding;
            int minY = Mathf.FloorToInt(bottomLeft.y) - padding;
            int maxX = Mathf.CeilToInt(topRight.x) + padding;
            int maxY = Mathf.CeilToInt(topRight.y) + padding;

            RectInt visibleBounds = new(minX, minY, Mathf.Max(0, maxX - minX), Mathf.Max(0, maxY - minY));

            if (visibleBounds == CurrentVisibleTileBounds)
            {
                return;
            }

            RectInt previousBounds = CurrentVisibleTileBounds;

            CurrentVisibleTileBounds = visibleBounds;
            OnVisibleTileBoundsChanged?.Invoke(visibleBounds);

            OnTileBoundsChanged(previousBounds, visibleBounds);
        }

        private void OnTileBoundsChanged(RectInt oldBounds, RectInt newBounds)
        {
            var world = WorldManager.Instance;
            var registry = GameDataRegistry.Instance;

            if (world == null || registry == null) return;

            // Find out if there are any old tiles left form the last visible bounds, if so clear the rendering of them
            if (oldBounds.width > 0 && oldBounds.height > 0)
            {
                for (int x = oldBounds.xMin; x < oldBounds.xMax; x++)
                {
                    for (int y = oldBounds.yMin; y < oldBounds.yMax; y++)
                    {
                        Vector2Int oldPos2D = new Vector2Int(x, y);

                        // If this old tile position is NO LONGER inside the new view box, wipe it
                        if (!newBounds.Contains(oldPos2D))
                        {
                            Vector3Int tilePos = new Vector3Int(x, y, 0);
                            world.ForegroundLayer.Tilemap.SetTile(tilePos, null);
                            world.BackgroundLayer.Tilemap.SetTile(tilePos, null);
                        }
                    }
                }
            }

            // Next draw the new tiles
            for (int x = newBounds.xMin; x < newBounds.xMax; x++)
            {
                for (int y = newBounds.yMin; y < newBounds.yMax; y++)
                {
                    Vector3Int tilePos = new(x, y, 0);

                    // Process Foreground Layer
                    int fgId = world.ForegroundLayer[x, y];
                    if (fgId > -1)
                    {
                        var fgTileAsset = registry.GetTileFromId(fgId);
                        if (fgTileAsset != null)
                        {
                            world.ForegroundLayer.Tilemap.SetTile(tilePos, fgTileAsset);
                        }
                    }

                    // Process Background Layer
                    int bgId = world.BackgroundLayer[x, y];
                    if (bgId > -1)
                    {
                        var bgTileAsset = registry.GetTileFromId(bgId);
                        if (bgTileAsset != null)
                        {
                            world.BackgroundLayer.Tilemap.SetTile(tilePos, bgTileAsset);
                        }
                    }
                }
            }
        }
    }
}
