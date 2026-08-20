using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        [Header("World Settings")]
        [SerializeField] private int _worldWidth = 100;
        [SerializeField] private int _worldHeight = 100;

        [Header("World References")]
        [SerializeField] private TilemapRenderer _foregroundTilemap;
        [SerializeField] private TilemapRenderer _backgroundTilemap;

        public TileLayer ForegroundLayer { get; private set; }
        public TileLayer BackgroundLayer { get; private set; }

        private void Awake()
        {
            Instance = this;
            
            ForegroundLayer = new TileLayer(_worldWidth, _worldHeight, _foregroundTilemap);
            BackgroundLayer = new TileLayer(_worldWidth, _worldHeight, _backgroundTilemap);
        }

        private void Start() 
        {
            
        }
    }
    
    public class TileLayer
    {
        private readonly int[] _tiles;
        private readonly int _width;
        private readonly int _height;
        
        public TilemapRenderer Tilemap { get; }
        
        public TileLayer(int width, int height, TilemapRenderer tilemap)
        {
            _width = width;
            _height = height;
            Tilemap = tilemap;
            
            _tiles = new int[width * height];
        }
        
        public int this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= _width || y < 0 || y >= _height) 
                {
                    Debug.LogError($"Attempted to access tile at ({x}, {y}) which is out of bounds.");
                    return -1;
                }
        
                return _tiles[y * _width + x];
            }
            set
            {
                if (x < 0 || x >= _width || y < 0 || y >= _height)
                {
                    Debug.LogError($"Attempted to set tile at ({x}, {y}) which is out of bounds.");
                    return;
                }
        
                _tiles[y * _width + x] = value;
            }
        }

    }
}

