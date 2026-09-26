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

        public event Action OnWorldReady;

        [Header("World References")]
        [field: SerializeField] public WorldGenerator WorldGen { get; private set; }
        [SerializeField] private Tilemap _foregroundTilemap;
        [SerializeField] private Tilemap _backgroundTilemap;

        [Header("Debug Testing")]
        [SerializeField] private TreeTileConfigSO _debugTreeConfig;

        public enum LayerType { Foreground, Background }
        public FluidGrid FluidGrid { get; private set; }
        public TileGrid FgGrid { get; private set; }
        public TileGrid BgGrid { get; private set; }
        public static Vector2Int MouseWorldTilePosition { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        public bool MouseOverUI { get; private set; }
        public bool IsWorldReady { get; private set; } = false;
        public int SeaLevel { get; private set; }
       

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            MouseOverUI = EventSystem.current.IsPointerOverGameObject();

            MouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseWorldTilePosition = new(Mathf.FloorToInt(MouseWorldPosition.x), Mathf.FloorToInt(MouseWorldPosition.y));

            if (Keyboard.current.tKey.wasPressedThisFrame && _debugTreeConfig != null)
            {
                Vector2Int mousePos = MouseWorldTilePosition;

                // Treat mouse position as the ground tile
                bool success = _debugTreeConfig.TryGrowTree(FgGrid, mousePos.x, mousePos.y, refreshBounds: true);
                Debug.Log(success ? "Tree planted!" : "Could not plant tree here (obstructed or out of bounds).");
            }
        }

        public void LoadGeneratedWorld(WorldGenContext context)
        {
            var width = context.Width;
            var height = context.Height;
            SeaLevel = context.SeaLevel;

            // Create fresh layers matching generated dimensions
            FgGrid = new TileGrid(width, height, _foregroundTilemap);
            BgGrid = new TileGrid(width, height, _backgroundTilemap);
            FluidGrid = new FluidGrid(width, height);

            // Copy generated 2D tile arrays into layers
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    FgGrid.SetTileData(x, y, context.FgGrid[x, y]);
                    BgGrid.SetTileData(x, y, context.BgGrid[x, y]);
                    FluidGrid.SetFluidData(x, y, context.FluidGrid[x, y]);
                }
            }

            IsWorldReady = true;
            OnWorldReady?.Invoke();

            // Refresh rendering and camera bounds
            PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
        }

        public void DamageTile(Vector2Int position, int damageAmount, LayerType layerType, bool refreshCurrentBounds = false)
        {
            TileGrid layer = layerType == LayerType.Foreground ? FgGrid : BgGrid;
            layer.DamageTile(position.x, position.y, damageAmount, refreshCurrentBounds);
        }
    }
    
    
}

