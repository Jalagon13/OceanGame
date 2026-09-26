using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class TileGrid
    {
        public event Action<Vector2Int> OnTileDestroyed;
        public event Action<Vector2Int> OnTilePlaced;
    
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;

        public Dictionary<Vector2Int, int> DamagedTiles { get; private set; } = new(); // Stores accumulated damage only for tiles that have taken damage
        public Tilemap Tilemap { get; }

        public TileGrid(int width, int height, Tilemap tilemap)
        {
            _width = width;
            _height = height;
            Tilemap = tilemap;

            _tiles = new TileData[width * height];
        }
        
        public TileData GetTileData(int x, int y)
        {
            if(!IsInBounds(x, y)) return TileData.OutOfBounds;
        
            return _tiles[y * _width + x];
        }

        public void DamageTile(int x, int y, int damageAmount, bool refreshCurrentBounds = true)
        {
            if (!IsInBounds(x, y)) return;

            TileData targetTile = GetTileData(x, y);
            if (targetTile.IsAir) return;

            TileConfigSO tc = targetTile.TileConfig;
            if (tc == null || tc.Indestructible || tc.MaxHP <= 0) return;

            // For rigid multi-tiles that share HP across the whole structure,
            // route damage to the root cell. (Trees won't use IsMultiTile so they route damage to (x, y)!)
            int damageX = tc.IsMultiTile ? x - targetTile.OffsetX : x;
            int damageY = tc.IsMultiTile ? y - targetTile.OffsetY : y;
            Vector2Int damagePos = new(damageX, damageY);

            DamagedTiles.TryGetValue(damagePos, out int currentDamage);
            currentDamage += damageAmount;

            if (currentDamage >= tc.MaxHP)
            {
                // Clear damage record for this tile/root
                DamagedTiles.Remove(damagePos);

                // Destroy the tile via DestroyTile (which delegates to TileConfigSO)
                DestroyTile(x, y, refreshCurrentBounds);
            }
            else
            {
                DamagedTiles[damagePos] = currentDamage;

                if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
                {
                    PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
                }
            }
        }

        public void DestroyTile(int x, int y, bool refreshCurrentBounds = false)
        {
            if (!IsInBounds(x, y)) return;

            TileData targetTile = _tiles[y * _width + x];
            if (targetTile.IsAir) return;

            TileConfigSO tc = targetTile.TileConfig;

            if (tc != null)
            {
                // Let the TileConfig handle clearing the cells and dropping items!
                tc.OnTileDestroyed(this, x, y, targetTile, refreshCurrentBounds);
            }
            else
            {
                // Fallback for tiles without config
                _tiles[y * _width + x] = TileData.Air;
                ClearDamage(x, y);
            }

            // Grid-level event notification (for audio, chunk updates, etc.)
            OnTileDestroyed?.Invoke(new Vector2Int(x, y));

            if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }

        public void SetTileData(int x, int y, TileData newTileData, bool refreshCurrentBounds = false, bool manualyPlaced = false)
        {
            if (!IsInBounds(x, y)) return;

            _tiles[y * _width + x] = newTileData;
            
            if(manualyPlaced)
            {
                OnTilePlaced?.Invoke(new(x, y));
            }

            if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }

        public void SetTileDataDirect(int x, int y, TileData newTileData)
        {
            if (!IsInBounds(x, y)) return;
            _tiles[y * _width + x] = newTileData;
        }

        public void ChangeMultiTileData(int x, int y, TileData newTileData, bool refreshCurrentBounds = false)
        {
            if (!IsInBounds(x, y)) return;

            TileConfigSO newTc = newTileData.TileConfig;

            if (!newTc.IsMultiTile) return;

            Vector2Int size = newTc.Size;
            
            int rootX = x - newTileData.OffsetX;
            int rootY = y - newTileData.OffsetY;

            // Update the State on ALL cells of the multi-tile
            for (int ox = 0; ox < size.x; ox++)
            {
                for (int oy = 0; oy < size.y; oy++)
                {
                    int targetX = rootX + ox;
                    int targetY = rootY + oy;

                    if (IsInBounds(targetX, targetY))
                    {
                        newTileData.OffsetX = (byte)ox;
                        newTileData.OffsetY = (byte)oy;
                        _tiles[targetY * _width + targetX] = newTileData;
                    }
                }
            }

            if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }

        public void PlaceMultiTileData(int x, int y, TileData newTileData, bool refreshCurrentBounds = false, bool manualyPlaced = false)
        {
            if (!IsInBounds(x, y)) return;
            
            TileConfigSO newTc = newTileData.TileConfig;
            
            if(!newTc.IsMultiTile) return;
            if(!CanMultiTileFit(x, y, newTc)) return;

            Vector2Int size = newTc.Size;

            // Update the State on ALL cells of the multi-tile
            for (int ox = 0; ox < size.x; ox++)
            {
                for (int oy = 0; oy < size.y; oy++)
                {
                    int targetX = x + ox;
                    int targetY = y + oy;

                    if (IsInBounds(targetX, targetY))
                    {
                        newTileData.OffsetX = (byte)ox;
                        newTileData.OffsetY = (byte)oy;
                        _tiles[targetY * _width + targetX] = newTileData;
                    }
                }
            }
            
            if(manualyPlaced)
            {
                OnTilePlaced?.Invoke(new(x, y));
            }

            if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }

        private bool CanMultiTileFit(int x, int y, TileConfigSO tc)
        {
            var size = tc.Size;
            
            for (int ox = 0; ox < size.x; ox++)
            {
                for (int oy = 0; oy < size.y; oy++)
                {
                    int offSetX = x + ox;
                    int offSetY = y + oy;
                    
                    if(!IsInBounds(offSetX, offSetY) || _tiles[offSetY * _width + offSetX].HasTile)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        public bool IsInBounds(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return false;
            }

            return true;
        }

        public void ClearDamage(int x, int y)
        {
            DamagedTiles.Remove(new Vector2Int(x, y));
        }
    }

    public struct TileData
    {
        public ushort TileId; // 2 bytes (0 -> 65,535 tile types)
        public byte OffsetX; // 1 byte  (multi-tile width up to 0 -> 255)
        public byte OffsetY; // 1 byte  (multi-tile height up to 0 -> 255)
        public byte State; // 1 byte  (0 -> 255 state variants)
        public byte LightLevel; // 1 byte  (0 -> 255 light emission/block light)
        public bool IsSolid; // 1 byte

        public const ushort AIR_ID = 0;
        public const ushort OUT_OF_BOUNDS_ID = ushort.MaxValue;
        
        public static readonly TileData OutOfBounds = new TileData { TileId = OUT_OF_BOUNDS_ID };
        public static readonly TileData Air = new TileData { TileId = AIR_ID };
        
        public bool IsAir => TileId == AIR_ID;
        public bool IsOutOfBounds => TileId == OUT_OF_BOUNDS_ID;
        public bool HasTile => TileId > AIR_ID && TileId < OUT_OF_BOUNDS_ID;
        public bool IsMultiTileRoot => TileConfig.IsMultiTile && OffsetX == 0 && OffsetY == 0;
        public TileConfigSO TileConfig => GameDataRegistry.Instance.GetTileConfigSOFromTileId(TileId);

        public TileData(ushort tileId, byte offsetX = 0, byte offsetY = 0, byte state = 0)
        {
            this = default; // Need to do this bc i cant access this keyword for initializing some of the stuff below
        
            TileId = tileId;
            OffsetX = offsetX;
            OffsetY = offsetY;
            State = state;
            
            // Config variables automatically set if tileconfig exists
            if(TileConfig != null)
            {
                LightLevel = (byte)(TileConfig.LightLevel * 100); // LightLevel here should be a byte between 0 and 100
                IsSolid = TileConfig.IsSolid;
            }
            else
            {
                Debug.Log($"U fucked up");
            }
        }
    }
}