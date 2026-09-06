using System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace OceanGame
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        
        [field: Header("Player Settings")]
        [SerializeField] private CharacterSO _playerSO;
        public CharacterSO Data => _playerSO;
        
        [SerializeField] private ServerCharacter _playerCharacter;
        public ServerCharacter Character => _playerCharacter;

        [field: SerializeField] public float InteractRange { get; private set; } = 4.5f;
        [field: SerializeField] public Vector2 WalkingBoxColliderSize { get; private set; }
        [field: SerializeField] public Vector2 SwimmingBoxColliderSize { get; private set; }

        [field: Header("Swim Settings")]
        [field: SerializeField] public float SwimDashSpeed { get; private set; }
        [field: SerializeField] public float SwimDashCooldown { get; private set; }

        [field: Header("Jump Settings")]
        [field: SerializeField] public float FromWaterJumpSpeed { get; private set; }
        [field: SerializeField] public float MinJumpSpeed { get; private set; }
        [field: SerializeField] public float MaxJumpHoldDuration { get; private set; }
        [field: SerializeField] public float AirborneMoveSpeedMultiplier { get; private set; }
        [field: SerializeField] public float JumpBufferDuration { get; private set; }
        [field: SerializeField] public float CoyoteTimeBufferDuration { get; private set; }

        [field: Header("Gravity Settings")]
        [field: SerializeField] public float GravityForce { get; private set; }
        [field: SerializeField] public float TerminalVelocity { get; private set; }

        public bool JumpPressed { get; set; }
        public bool WaterJumpBuffered { get; set; }
        public float SwimDashCooldownTimer { get; set; }
        
        private void Awake() 
        {
            Instance = this;    
        }
        
        private void Start() 
        {
            GameInput.Instance.OnMoveInputPressed += OnMoveInputPressed;
            GameInput.Instance.OnJumpPressed += OnJumpPressed;
            GameInput.Instance.OnSecondaryActionPressed += InteractWithObject;
        }
        
        private void OnDestroy() 
        {
            GameInput.Instance.OnMoveInputPressed -= OnMoveInputPressed;
            GameInput.Instance.OnJumpPressed -= OnJumpPressed;
            GameInput.Instance.OnSecondaryActionPressed -= InteractWithObject;
        }

        private void Update()
        {
            if (!WorldManager.Instance.IsWorldReady) return;

            if (SwimDashCooldownTimer > 0f)
            {
                SwimDashCooldownTimer -= Time.deltaTime;
            }

        }

        private void InteractWithObject(InputAction.CallbackContext context)
        {
            // Interact with interactable logic here
            if (!WorldManager.Instance.IsWorldReady || WorldManager.Instance.MouseOverUI || !InventoryCursorManager.Instance.CursorSlot.IsEmpty || context.phase != InputActionPhase.Started) return;

            var pos = WorldManager.MouseWorldTilePosition;
            var fgtd = WorldManager.Instance.FgGrid.GetTileData(pos.x, pos.y);

            if(fgtd.HasTile && fgtd.TileConfig.InteractBehavior != null)
            {
                fgtd.TileConfig.InteractBehavior.Interact(pos.x, pos.y);
            }
        }

        private void OnJumpPressed()
        {
            if(!WorldManager.Instance.IsWorldReady) return;
        
            bool canJumpFromGround = _playerCharacter.CollisionResult.TouchingBottom;
            bool canJumpFromWater = _playerCharacter.Machine.Root.Leaf() is PlayerSwimmingState && IsHeadInAir();

            if (canJumpFromGround || canJumpFromWater)
            {
                JumpPressed = true;
            }
        }

        private void OnMoveInputPressed(Vector2 rawMoveInput)
        {
            if (!WorldManager.Instance.IsWorldReady) return;
            
            _playerCharacter.DesiredDirection = rawMoveInput;
        }

        public bool IsHeadInAir()
        {
            // Get top of head position using actual collider height
            float headY = _playerCharacter.transform.position.y + 1;
            Vector2Int playerHeadTop = new Vector2Int(Mathf.FloorToInt(_playerCharacter.transform.position.x), Mathf.FloorToInt(headY));

            // Head is above water if top tile is AIR (or not Water)
            return WorldManager.Instance.FluidGrid.GetFluidType(playerHeadTop.x, playerHeadTop.y) == FluidType.Air;
        }

        public bool IsInWater()
        {
            Vector2Int playerTilePos = new Vector2Int(Mathf.FloorToInt(_playerCharacter.transform.position.x), Mathf.FloorToInt(_playerCharacter.transform.position.y - 0.5f));
            return WorldManager.Instance.FluidGrid.GetFluidType(playerTilePos.x, playerTilePos.y) == FluidType.Water;
        }
    }
}
