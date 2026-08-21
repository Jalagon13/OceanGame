using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class WorldInteractionManager : MonoBehaviour
    {
        public static WorldInteractionManager Instance { get; private set; }
        
        [SerializeField] private TileBase _grassTile;
        
        public static Vector2Int MouseWorldTilePosition { get; private set; }
        public static Vector2 MouseWorldPosition { get; private set; }
        
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
            MouseWorldPosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            MouseWorldTilePosition = new(Mathf.FloorToInt(MouseWorldPosition.x), Mathf.FloorToInt(MouseWorldPosition.y));
        }

        private void OnPrimaryActionPressed()
        {
            Debug.Log($"1");
            if(WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] > TileLayer.AIR_ID)
            {
                Debug.Log($"Clearing {MouseWorldTilePosition} to empty");
                WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] = TileLayer.AIR_ID;
            }
        }

        private void OnSecondaryActionPressed()
        {
            Debug.Log($"2");
            if (WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] == TileLayer.AIR_ID)
            {
                Debug.Log($"Setting {MouseWorldTilePosition} to grass");
                WorldManager.Instance.ForegroundLayer[MouseWorldTilePosition.x, MouseWorldTilePosition.y] = GameDataRegistry.Instance.GetTileId(_grassTile);
            }
        }

        
    }
}
