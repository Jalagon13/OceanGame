using System;
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
        [SerializeField] private TileConfigSO _grassTc;
        [SerializeField] private TileConfigSO _dirtTc;

        [field: Header("World Settings")]
        [field: SerializeField] public int WorldWidth { get; private set; } = 100;
        [field: SerializeField] public int WorldHeight { get; private set; } = 100;
        [field: SerializeField] public int SeaLevel { get; private set; } = 40;

        [Header("World References")]
        [SerializeField] private Tilemap _foregroundTilemap;
        [SerializeField] private Tilemap _backgroundTilemap;

        public TileLayer FgLayer { get; private set; }
        public TileLayer BgLayer { get; private set; }
        public static Vector2Int MouseWorldTilePosition { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        public bool MouseOverUI { get; private set; }

        private void Awake()
        {
            Instance = this;
            
            FgLayer = new TileLayer(WorldWidth, WorldHeight, _foregroundTilemap);
            BgLayer = new TileLayer(WorldWidth, WorldHeight, _backgroundTilemap);
        }

        private void Start() 
        {
            GameInput.Instance.OnSecondaryActionPressed += InteractWithObject;
        
            InitializeWorld();
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnSecondaryActionPressed -= InteractWithObject;
        }

        private void Update()
        {
            MouseOverUI = EventSystem.current.IsPointerOverGameObject();

            MouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseWorldTilePosition = new(Mathf.FloorToInt(MouseWorldPosition.x), Mathf.FloorToInt(MouseWorldPosition.y));
        }

        private void InteractWithObject()
        {
            // Interact with interactable logic here
            if(MouseOverUI || !InventoryCursorManager.Instance.CursorSlot.IsEmpty) return; 
            
            var pos = MouseWorldTilePosition;
            var fgtd = FgLayer.GetTileData(pos.x, pos.y);
            
            if(fgtd.TileConfig is IInteractable i)
            {
                i.OnInteract(pos.x, pos.y);
            }
        }

        private void InitializeWorld()
        {
            for (int x = 0; x < WorldWidth; x++)
            {
                for (int y = 0; y < WorldHeight; y++)
                {
                    if (y > _tempY /* && y < _tempY + 4 */) continue;
                    if (x > _tempX) continue;

                    FgLayer.SetTileData(x, y, new TileData(_grassTc.GetId(), isSolid: _grassTc.IsSolid));
                    BgLayer.SetTileData(x, y, new TileData(_dirtTc.GetId(), isSolid: _dirtTc.IsSolid));
                }
            }

            PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
        }
    }
    
    
}

