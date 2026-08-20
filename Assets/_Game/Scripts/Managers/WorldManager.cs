using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        [field: Header("World Settings")]
        [field: SerializeField] public int WorldWidth { get; } = 100;
        [field: SerializeField] public int WorldHeight { get; } = 100;

        [Header("World References")]
        [SerializeField] private TileBase _grassTile;
        [SerializeField] private Tilemap _foregroundTilemap;
        [SerializeField] private Tilemap _backgroundTilemap;

        public TileLayer ForegroundLayer { get; private set; }
        public TileLayer BackgroundLayer { get; private set; }

        private void Awake()
        {
            Instance = this;
            
            ForegroundLayer = new TileLayer(WorldWidth, WorldHeight, _foregroundTilemap);
            BackgroundLayer = new TileLayer(WorldWidth, WorldHeight, _backgroundTilemap);
        }

        private void Start() 
        {
            for (int x = 0; x < WorldWidth; x++)
            {
                for (int y = 0; y < WorldHeight; y++)
                {
                    ForegroundLayer[x, y] = GameDataRegistry.Instance.GetTileId(_grassTile);
                    BackgroundLayer[x, y] = GameDataRegistry.Instance.GetTileId(_grassTile);
                }
            }
        }
    }
    
    public class TileLayer
    {
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
                if (x < 0 || x >= _width || y < 0 || y >= _height) 
                {
                    // Debug.LogWarning($"{Tilemap.name}: Attempted to access tile at ({x}, {y}) which is out of bounds.");
                    return -2; // Return a special value indicating out-of-bounds access
                }
        
                return _tiles[y * _width + x];
            }
            set
            {
                if (x < 0 || x >= _width || y < 0 || y >= _height)
                {
                    Debug.LogError($"{Tilemap.name}: Attempted to set tile at ({x}, {y}) which is out of bounds.");
                    return;
                }
        
                _tiles[y * _width + x] = value;
            }
        }

        // public bool IsInBounds(int x, int y)
        // {
        //     if (x < 0 || x >= _width || y < 0 || y >= _height)
        //     {
        //         return false;
        //     }

        //     // Checks if the tile gives back a real block/air code, or the out-of-bounds error code
        //     return _tiles[y * _width + x] >= -1;
        // }
    }
}

