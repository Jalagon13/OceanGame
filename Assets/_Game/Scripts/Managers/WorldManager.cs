using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }
        
        [SerializeField] private int _tempX = 45;
        [SerializeField] private int _tempY = 45;
        [SerializeField] private TileSO _grassTile;
        [SerializeField] private TileSO _dirtBgTile;

        [field: Header("World Settings")]
        [field: SerializeField] public int WorldWidth { get; private set; } = 100;
        [field: SerializeField] public int WorldHeight { get; private set; } = 100;
        [field: SerializeField] public int SeaLevel { get; private set; } = 40;

        [Header("World References")]
        [SerializeField] private Tilemap _foregroundTilemap;
        [SerializeField] private Tilemap _backgroundTilemap;

        public TileLayer ForegroundLayer { get; private set; }
        public TileLayer BackgroundLayer { get; private set; }
        public static Vector2Int MouseWorldTilePosition { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        public bool MouseOverUI { get; private set; }

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
                    if(y > _tempY /* && y < _tempY + 4 */) continue;
                    if(x > _tempX) continue;
                
                    ForegroundLayer.SetTile(x, y, _grassTile.GetId());
                    BackgroundLayer.SetTile(x, y, _dirtBgTile.GetId()); 
                }
            }

            PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
        }

        private void Update()
        {
            MouseOverUI = EventSystem.current.IsPointerOverGameObject();

            MouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseWorldTilePosition = new(Mathf.FloorToInt(MouseWorldPosition.x), Mathf.FloorToInt(MouseWorldPosition.y));
        }
    }
    
    
}

