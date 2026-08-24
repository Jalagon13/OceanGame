using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldInteractionManager : MonoBehaviour
    {
        public static WorldInteractionManager Instance { get; private set; }
        
        [SerializeField] private TileSO _grassTile;
        
        public static Vector2Int MouseWorldTilePosition { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        
        public bool MouseOverUI { get; private set; }
        
        private void Awake() 
        {
            Instance = this;    
        }
        
        private void Start() 
        {
            GameInput.Instance.OnPrimaryActionPressed += OnPrimaryActionPressed;
            GameInput.Instance.OnSecondaryActionPressed += OnSecondaryActionPressed;
        }
        
        private void OnDestroy() 
        {
            GameInput.Instance.OnPrimaryActionPressed -= OnPrimaryActionPressed;
            GameInput.Instance.OnSecondaryActionPressed -= OnSecondaryActionPressed;
        }
        
        private void Update() 
        {
            MouseOverUI = EventSystem.current.IsPointerOverGameObject();

            MouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseWorldTilePosition = new(Mathf.FloorToInt(MouseWorldPosition.x), Mathf.FloorToInt(MouseWorldPosition.y));
        }

        private void OnPrimaryActionPressed()
        {
            if(MouseOverUI) return;
        
            if(WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] > TileLayer.AIR_ID)
            {
                Debug.Log($"Clearing {MouseWorldTilePosition} to empty");
                TileSO tileBeingDestroy = GameDataRegistry.Instance.GetTileSOFromTileId(WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y]);
                WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] = TileLayer.AIR_ID;
                
                Vector2 spawnPosition = new(MouseWorldTilePosition.x + 0.5f, MouseWorldTilePosition.y + 0.5f);
                GameManager.Instance.SpawnItem(tileBeingDestroy.DropItem, tileBeingDestroy.GetDrops(), spawnPosition);
            }
        }

        private void OnSecondaryActionPressed()
        {
            if (MouseOverUI) return;

            if (WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] == TileLayer.AIR_ID)
            {
                Debug.Log($"Setting {MouseWorldTilePosition} to grass");
                WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] = GameDataRegistry.Instance.GetTileIdFromTileSO(_grassTile);
            }
        }

        
    }
}
