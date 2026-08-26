using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class TileLayer
    {
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;

        public Tilemap Tilemap { get; }

        public TileLayer(int width, int height, Tilemap tilemap)
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

        public void DestroyTile(int x, int y, bool refreshCurrentBounds = false)
        {
            if (!IsInBounds(x, y)) return;

            TileData targetTile = _tiles[y * _width + x];
            if (targetTile.IsAir) return; // Nothing to destroy if it's air

            TileConfigSO tc = targetTile.TileConfig;

            if (tc != null && tc.IsMultiTile)
            {
                // Calculate Root position by subtracting local offsets
                int rootX = x - targetTile.OffsetX;
                int rootY = y - targetTile.OffsetY;

                Vector2Int size = tc.Size;

                // Loop through all cells belonging to this multi-tile structure and clear them
                for (int ox = 0; ox < size.x; ox++)
                {
                    for (int oy = 0; oy < size.y; oy++)
                    {
                        int tileX = rootX + ox;
                        int tileY = rootY + oy;

                        if (IsInBounds(tileX, tileY))
                        {
                            _tiles[tileY * _width + tileX] = TileData.Air;
                        }
                    }
                }
            }
            else
            {
                // Single 1x1 tile destruction
                _tiles[y * _width + x] = TileData.Air;
            }

            if (refreshCurrentBounds)
            {
                // If it's a change on screen, refresh the bounds to refresh rendered tiles
                if (PlayerCamera.Instance.PositionExistsInBounds(x, y))
                {
                    PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
                }
            }
        }
        
        public void SetTileData(int x, int y, TileData newTileData, bool refreshCurrentBounds = false)
        {
            if (!IsInBounds(x, y)) return;

            _tiles[y * _width + x] = newTileData;

            if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
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

        public void PlaceMultiTileData(int x, int y, TileData newTileData, bool refreshCurrentBounds = false)
        {
            Debug.Log($"1");
            if (!IsInBounds(x, y)) return;
            Debug.Log($"2");
            
            TileConfigSO newTc = newTileData.TileConfig;
            
            if(!newTc.IsMultiTile) return;
            Debug.Log($"3");
            
            if(!CanMultiTileFit(x, y, newTc)) return;
            Debug.Log($"4");

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

        public TileData(ushort tileId, byte offsetX = 0, byte offsetY = 0, byte state = 0, bool isSolid = true)
        {
            TileId = tileId;
            OffsetX = offsetX;
            OffsetY = offsetY;
            State = state;
            LightLevel = 0;
            IsSolid = isSolid;
        }
    }
}