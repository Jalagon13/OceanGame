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

        [Header("Decal Overlay References")]
        [SerializeField] private Tilemap _damageOverlayTilemap;
        [SerializeField] private TileBase[] _crackTiles; // Array of crack stage tiles (e.g. 3 stages: 25%, 50%, 75%)

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

            if (world == null || !world.IsWorldReady || registry == null) return;

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
                            world.FgGrid.Tilemap.SetTile(tilePos, null);
                            world.BgGrid.Tilemap.SetTile(tilePos, null);
                            _waterTilemap.SetTile(tilePos, null);
                            _damageOverlayTilemap.SetTile(tilePos, null);
                        }
                    }
                }
            }

            // Next draw the new tiles
            for (int x = newBounds.xMin; x < newBounds.xMax; x++)
            {
                for (int y = newBounds.yMin; y < newBounds.yMax; y++)
                {
                    Vector2Int tilePos = new(x, y);

                    // Process Foreground Layer
                    var fgTd = world.FgGrid.GetTileData(x, y);
                    var bgTd = world.BgGrid.GetTileData(x, y);
                    Vector3Int tilePos3D = (Vector3Int)tilePos;

                    if (fgTd.HasTile)
                    {
                        var fgtc = fgTd.TileConfig;
                        
                        if (fgtc != null)
                        {
                            if (fgtc.IsMultiTile)
                            {
                                // Render the sprite ONLY on the root cell.
                                // Non-root cells set 'null' to ensure old tiles underneath are cleared.
                                if (fgTd.IsMultiTileRoot)
                                {
                                    // Before it draws the multi tile, it needs to fetch the interpretation of the tile depending on the state
                                    byte state = fgTd.State;
                                    TileBase interpretedTile = fgtc.GetStateInterpretedTileForRendering(state);

                                    world.FgGrid.Tilemap.SetTile(tilePos3D, interpretedTile);
                                }
                                else
                                {
                                    world.FgGrid.Tilemap.SetTile(tilePos3D, null);
                                }
                            }
                            else
                            {
                                // Standard 1x1 tile
                                // Before it draws the tile, it needs to fetch the interpretation of the tile depending on the state
                                byte state = fgTd.State;
                                TileBase interpretedTile = fgtc.GetStateInterpretedTileForRendering(state);

                                world.FgGrid.Tilemap.SetTile(tilePos3D, interpretedTile);
                            }
                        }
                    }
                    else if(fgTd.IsAir) // If we are setting it to air
                    {
                        world.FgGrid.Tilemap.SetTile(tilePos3D, null);
                    }

                    // Process cracked tile decals
                    // Check if the foreground has damage first; if empty or undamaged, check background damage
                    int currentDamage = 0;
                    int maxHp = 0;

                    if (world.FgGrid.DamagedTiles.TryGetValue(tilePos, out int fgDmg) && fgTd.HasTile)
                    {
                        currentDamage = fgDmg;
                        maxHp = fgTd.TileConfig.MaxHP;
                    }
                    else if (world.BgGrid.DamagedTiles.TryGetValue(tilePos, out int bgDmg) && bgTd.HasTile)
                    {
                        currentDamage = bgDmg;
                        maxHp = bgTd.TileConfig.MaxHP;
                    }

                    if (currentDamage > 0 && maxHp > 0 && _crackTiles != null && _crackTiles.Length > 0)
                    {
                        float damageRatio = Mathf.Clamp01((float)currentDamage / maxHp);
                        int stageIndex = Mathf.Clamp(Mathf.FloorToInt(damageRatio * _crackTiles.Length), 0, _crackTiles.Length - 1);
                        _damageOverlayTilemap.SetTile(tilePos3D, _crackTiles[stageIndex]);
                    }
                    else
                    {
                        _damageOverlayTilemap.SetTile(tilePos3D, null);
                    }

                    // Process Background Layer
                    if (bgTd.HasTile)
                    {
                        var bgtc = bgTd.TileConfig;
                        
                        if (bgtc != null)
                        {
                            // Use state interpretation so variations/connections match foreground logic
                            TileBase interpretedTile = bgtc.GetStateInterpretedTileForRendering(bgTd.State);
                            world.BgGrid.Tilemap.SetTile(tilePos3D, interpretedTile);
                        }
                    }
                    else if (bgTd.IsAir)
                    {
                        world.BgGrid.Tilemap.SetTile(tilePos3D, null);
                    }
                    
                    // Process Sea Layer
                    FluidType fluid = world.FluidGrid.GetFluidType(x, y);
                    switch (fluid)
                    {
                        case FluidType.Water:
                            _waterTilemap.SetTile(tilePos3D, _waterTile);
                            break;
                        case FluidType.Air:
                            _waterTilemap.SetTile(tilePos3D, null);
                            break;
                        default:
                            _waterTilemap.SetTile(tilePos3D, null);
                            break;
                    }
                }
            }
        }
    }
}
