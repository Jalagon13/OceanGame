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
        
        [Header("World References")]
        [field: SerializeField] public WorldGenerator WorldGen { get; private set; }
        [SerializeField] private Tilemap _foregroundTilemap;
        [SerializeField] private Tilemap _backgroundTilemap;

        public enum LayerType { Foreground, Background }
        public TileLayer FgLayer { get; private set; }
        public TileLayer BgLayer { get; private set; }
        public static Vector2Int MouseWorldTilePosition { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        public bool MouseOverUI { get; private set; }
        public bool IsWorldReady { get; private set; } = false;
        public event Action OnWorldReady;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            MouseOverUI = EventSystem.current.IsPointerOverGameObject();

            MouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseWorldTilePosition = new(Mathf.FloorToInt(MouseWorldPosition.x), Mathf.FloorToInt(MouseWorldPosition.y));
        }

        public void LoadGeneratedWorld(WorldGenContext context)
        {
            var width = context.Width;
            var height = context.Height;

            // Create fresh layers matching generated dimensions
            FgLayer = new TileLayer(width, height, _foregroundTilemap);
            BgLayer = new TileLayer(width, height, _backgroundTilemap);

            // Copy generated 2D tile arrays into layers
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    FgLayer.SetTileData(x, y, context.FgTiles[x, y]);
                    BgLayer.SetTileData(x, y, context.BgTiles[x, y]);
                }
            }

            IsWorldReady = true;
            OnWorldReady?.Invoke();

            // Refresh rendering and camera bounds
            PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
        }

        public void DamageTile(Vector2Int position, int damageAmount, LayerType layerType, bool refreshCurrentBounds = false)
        {
            TileLayer layer = layerType == LayerType.Foreground ? FgLayer : BgLayer;
            layer.DamageTile(position.x, position.y, damageAmount, refreshCurrentBounds);
        }
    }
    
    
}

