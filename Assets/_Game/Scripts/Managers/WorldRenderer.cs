using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldRenderer : MonoBehaviour
    {
        public static WorldRenderer Instance { get; private set; }
        
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Tilemap _waterTilemap;
        [SerializeField] private TileBase _waterTile;

        private void Awake()
        {
            Instance = this;
        }
        
        private void Start() 
        {
            PlayerCamera.Instance.OnVisibleTileBoundsChanged += OnTileBoundsChanged;
        }
        
        private void OnDestroy() 
        {
            PlayerCamera.Instance.OnVisibleTileBoundsChanged -= OnTileBoundsChanged;
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
                            _waterTilemap.SetTile(tilePos, null);
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
                    var fgTd = world.ForegroundLayer.GetTileData(x, y);
                    if (fgTd.HasTile)
                    {
                        var fgTileAsset = registry.GetTileSOFromTileId(fgTd.TileId);
                        if (fgTileAsset != null)
                        {
                            world.ForegroundLayer.Tilemap.SetTile(tilePos, fgTileAsset);
                        }
                    }
                    else if(fgTd.IsAir) // If we are setting it to air
                    {
                        world.ForegroundLayer.Tilemap.SetTile(tilePos, null);
                    }

                    // Process Background Layer
                    var bgTd = world.BackgroundLayer.GetTileData(x, y);
                    if (bgTd.HasTile)
                    {
                        var bgTileAsset = registry.GetTileSOFromTileId(bgTd.TileId);
                        if (bgTileAsset != null)
                        {
                            world.BackgroundLayer.Tilemap.SetTile(tilePos, bgTileAsset);
                        }
                    }
                    else if(bgTd.IsAir) // If we are setting it to air
                    {
                        world.BackgroundLayer.Tilemap.SetTile(tilePos, null);
                    }
                    
                    // Process Sea Layer
                    if(y <= WorldManager.Instance.SeaLevel)
                    {
                        if (/* fgId <= TileLayer.AIR_ID &&  */!world.ForegroundLayer.GetTileData(x, y).IsOutOfBounds)
                        {
                            _waterTilemap.SetTile(tilePos, _waterTile);
                        }
                    }
                }
            }
        }
    }
}
