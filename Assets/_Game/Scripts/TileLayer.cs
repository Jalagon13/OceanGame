using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class TileLayer
    {
        public static int OUT_OF_BOUNDS_ID { get; } = -2;
        public static int AIR_ID { get; } = -1;

        private readonly int[] _tiles;
        private readonly int _width;
        private readonly int _height;

        public Tilemap Tilemap { get; }

        public TileLayer(int width, int height, Tilemap tilemap)
        {
            _width = width;
            _height = height;
            Tilemap = tilemap;

            _tiles = new int[width * height];

            // Overwrite the defaults so the world starts as empty Air (-1)
            for (int i = 0; i < _tiles.Length; i++)
            {
                _tiles[i] = -1;
            }
        }

        public int this[int x, int y]
        {
            get
            {
                if (!IsInBounds(x, y))
                {
                    return OUT_OF_BOUNDS_ID; // Return a special value indicating out-of-bounds access
                }

                return _tiles[y * _width + x];
            }
        }

        public TileItemSO GetItemSO(int x, int y)
        {
            return GameDataRegistry.Instance.GetTileSOFromTileId(_tiles[y * _width + x]).TileItemSO;
        }

        public void SetTile(int x, int y, int tileId, bool refreshCurrentBounds = false)
        {
            if (!IsInBounds(x, y)) return;

            _tiles[y * _width + x] = tileId;

            if (refreshCurrentBounds)
            {
                // If its a change on screen, refresh the bounds to refresh the rendered tiles
                if (PlayerCamera.Instance.PositionExistsInBounds(x, y))
                {
                    PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
                }
            }
        }

        public bool HasTileAt(int x, int y)
        {
            if (!IsInBounds(x, y)) return false;

            return _tiles[y * _width + x] > AIR_ID;
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
}