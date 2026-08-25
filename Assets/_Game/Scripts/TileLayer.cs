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

        public void SetTile(int x, int y, TileData tileData, bool refreshCurrentBounds = false)
        {
            if (!IsInBounds(x, y)) return;

            _tiles[y * _width + x] = tileData;

            if (refreshCurrentBounds)
            {
                // If its a change on screen, refresh the bounds to refresh the rendered tiles
                if (PlayerCamera.Instance.PositionExistsInBounds(x, y))
                {
                    PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
                }
            }
        }

        public TileItemSO GetItemSO(int x, int y)
        {
            return GameDataRegistry.Instance.GetTileSOFromTileId(_tiles[y * _width + x].TileId).TileItemSO;
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
        public ushort TileId;     // 2 bytes (0 -> 65,535 tile types)
        public byte OffsetX;      // 1 byte  (multi-tile width up to 0 -> 255)
        public byte OffsetY;      // 1 byte  (multi-tile height up to 0 -> 255)
        public byte State;        // 1 byte  (0 -> 255 state variants)
        public byte LightLevel;   // 1 byte  (0 -> 255 light emission/block light)
        public ushort Flags;      // 2 bytes (extra flags like flipped, active, etc.) MIGHT NOT NEED

        public const ushort AIR_ID = 0;
        public const ushort OUT_OF_BOUNDS_ID = ushort.MaxValue;
        
        public static readonly TileData OutOfBounds = new TileData { TileId = OUT_OF_BOUNDS_ID };
        public static readonly TileData Air = new TileData { TileId = AIR_ID };
        
        public bool IsAir => TileId == AIR_ID;
        public bool IsOutOfBounds => TileId == OUT_OF_BOUNDS_ID;
        public bool HasTile => TileId > AIR_ID && TileId < OUT_OF_BOUNDS_ID;

        public TileData(ushort tileId, byte offsetX = 0, byte offsetY = 0, byte state = 0)
        {
            TileId = tileId;
            OffsetX = offsetX;
            OffsetY = offsetY;
            State = state;
            LightLevel = 0;
            Flags = 0;
        }
    }
}