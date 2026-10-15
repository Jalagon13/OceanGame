using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OceanGame
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        
        public event Action<ServerCharacter> PlayerReady;
        
        [field: Header("Player Settings")]
        [SerializeField] private CharacterSO _playerSO;
        public CharacterSO Data => _playerSO;
        
        [field: SerializeField] public float InteractRange { get; private set; } = 4.5f;
        [field: SerializeField] public Vector2 WalkingBoxColliderSize { get; private set; }
        [field: SerializeField] public Vector2 SwimmingBoxColliderSize { get; private set; }

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
        public Vector2 RespawnPoint { get; private set; }
        public PlayerArmHandler ArmHandler { get; private set; }
        public PlayerRangedHandler RangedHandler { get; private set; }
        public ServerCharacter Character { get; private set; }
        public PlayerEquipment Equipment { get; private set; }
        public PlayerDepthTracker DepthTracker { get; private set; }

        private bool _wasInWater;
        private bool _hasInitializedAmbience;
        private bool _hasInitialSpawn;

        private void Awake() 
        {
            Instance = this;    
        }
        
        private void Start() 
        {
            GameInput.Instance.OnMoveInputPressed += OnMoveInputPressed;
            GameInput.Instance.OnJumpPressed += OnJumpPressed;
            GameInput.Instance.OnSecondaryActionPressed += InteractWithObject;
            WorldManager.Instance.OnWorldReady += InitializeSpawn;
            InitializeSpawn();
        }
        
        private void OnDestroy() 
        {
            GameInput.Instance.OnMoveInputPressed -= OnMoveInputPressed;
            GameInput.Instance.OnJumpPressed -= OnJumpPressed;
            GameInput.Instance.OnSecondaryActionPressed -= InteractWithObject;
            WorldManager.Instance.OnWorldReady -= InitializeSpawn;

            if (Character != null) Character.Health.CurrentLifeState.OnValueChanged -= OnLifeStateChanged;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopOceanAmbience();
                AudioManager.Instance.StopSurfaceAmbience();
            }
        }

        private void Update()
        {
            if (!WorldManager.Instance.IsWorldReady || Character == null) return;

            // Code for checking water ambience
            bool isInWater = IsInWater();

            if (!_hasInitializedAmbience || isInWater != _wasInWater)
            {
                if (isInWater)
                {
                    AudioManager.Instance.StartOceanAmbience(FMODEvents.Instance.OceanAmbience);
                    AudioManager.Instance.StopSurfaceAmbience();
                }
                else
                {
                    AudioManager.Instance.StopOceanAmbience();
                    AudioManager.Instance.StartSurfaceAmbience(FMODEvents.Instance.SurfaceAmbience);
                }

                _wasInWater = isInWater;
                _hasInitializedAmbience = true;
            }

            // If player is not dead and is holding down move input keep updating desired direction with it
            if (Character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            if (GameInput.Instance.MoveInput != Vector2.zero)
            {
                Character.DesiredDirection = GameInput.Instance.MoveInput;
            }
        }

        private void InitializeSpawn()
        {
            if (_hasInitialSpawn || Character == null || !WorldManager.Instance.IsWorldReady)
                return;

            if (!WorldManager.Instance.TryGetPlayerSpawnPosition(WalkingBoxColliderSize, out Vector2 spawnPosition))
            {
                Debug.LogWarning($"Could not find a spawn position for player, defaulting to prefab position");
                Character.transform.position = Character.transform.position;
                Character.Velocity = Vector2.zero;
                Character.DesiredDirection = Vector2.zero;
                Character.KnockbackVelocity = Vector2.zero;

                RespawnPoint = Character.transform.position;
                _hasInitialSpawn = true;
                return;
            }

            Character.transform.position = spawnPosition;
            Character.Velocity = Vector2.zero;
            Character.DesiredDirection = Vector2.zero;
            Character.KnockbackVelocity = Vector2.zero;

            RespawnPoint = spawnPosition;
            _hasInitialSpawn = true;
        }

        public void BindCharacter(ServerCharacter character)
        {
            if (Character == character) return;

            Character = character;
            Character.Health.CurrentLifeState.OnValueChanged += OnLifeStateChanged;

            Equipment = Character.GetComponent<PlayerEquipment>();
            ArmHandler = Character.GetComponent<PlayerArmHandler>();
            RangedHandler = Character.GetComponent<PlayerRangedHandler>();
            DepthTracker = Character.GetComponent<PlayerDepthTracker>();

            InitializeSpawn();
            PlayerReady?.Invoke(Character);
        }

        private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
        {
            if(newValue == LifeState.Dead)
            {
                Character.KnockbackVelocity = Vector2.zero;
                Character.DesiredDirection = Vector2.zero;
                Character.Velocity = Vector2.zero;
            }
            
            if(previousValue == LifeState.Dead && newValue == LifeState.Alive)
            {
                // respawn
                Character.transform.position = RespawnPoint;
                Character.Health.CurrentHealth.Value = Character.Stats.MaxHealth.GetValue();
            }
        }

        private void InteractWithObject(InputAction.CallbackContext context)
        {
            // Interact with interactable logic here
            if (!WorldManager.Instance.IsWorldReady || WorldManager.Instance.MouseOverUI || !InventoryCursorManager.Instance.CursorSlot.IsEmpty || context.phase != InputActionPhase.Started || Character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            var pos = WorldManager.MouseWorldTilePosition;
            var fgtd = WorldManager.Instance.FgGrid.GetTileData(pos.x, pos.y);

            if(fgtd.HasTile && fgtd.TileConfig.InteractBehavior != null)
            {
                fgtd.TileConfig.InteractBehavior.Interact(pos.x, pos.y);
            }
        }

        private void OnJumpPressed()
        {
            if(!WorldManager.Instance.IsWorldReady || Character.Health.CurrentLifeState.Value == LifeState.Dead) return;
        
            bool canJumpFromGround = Character.CollisionResult.TouchingBottom;
            bool canJumpFromWater = Character.Machine.Root.Leaf() is PlayerSwimmingState && IsHeadInAir();

            if (canJumpFromGround || canJumpFromWater)
            {
                JumpPressed = true;
            }
        }

        private void OnMoveInputPressed(Vector2 rawMoveInput)
        {
            if (!WorldManager.Instance.IsWorldReady || Character.Health.CurrentLifeState.Value == LifeState.Dead) return;

            Character.DesiredDirection = rawMoveInput;
        }

        public bool IsHeadInAir()
        {
            // Get top of head position using actual collider height
            float headY = Character.transform.position.y + 1;
            Vector2Int playerHeadTop = new Vector2Int(Mathf.FloorToInt(Character.transform.position.x), Mathf.FloorToInt(headY));

            // Head is above water if top tile is AIR (or not Water)
            return WorldManager.Instance.FluidGrid.GetFluidType(playerHeadTop.x, playerHeadTop.y) == FluidType.Air;
        }

        public bool IsInWater()
        {
            Vector2Int playerTilePos = new Vector2Int(Mathf.FloorToInt(Character.transform.position.x), Mathf.FloorToInt(Character.transform.position.y - 0.5f));
            return WorldManager.Instance.FluidGrid.GetFluidType(playerTilePos.x, playerTilePos.y) == FluidType.Water;
        }
    }
}
