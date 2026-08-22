using System;
using System.Linq;
using UnityEngine;

namespace OceanGame
{
    public class Player : MonoBehaviour
    {
        public static Player Instance { get; private set; }
        
        public PlayerContext Ctx = new();
        
        private StateMachine _machine;
        private State _root;
        private string _lastPath;
        
        private void Awake() 
        {
            Instance = this;    
            
            _root = new PlayerRootState(null, Ctx);
            var builder = new StateMachineBuilder(_root);
            _machine = builder.Build();
            _machine.Start();

            Ctx.Transform = transform;
        }
        
        private void Start() 
        {
            GameInput.Instance.OnMoveInputPressed += OnMoveInputPressed;
            GameInput.Instance.OnJumpPressed += OnJumpPressed;
        }
        
        private void OnDestroy() 
        {
            GameInput.Instance.OnMoveInputPressed -= OnMoveInputPressed;
            GameInput.Instance.OnJumpPressed -= OnJumpPressed;
        }

        private void Update()
        {
            if (Ctx.SwimDashCooldownTimer > 0f)
            {
                Ctx.SwimDashCooldownTimer -= Time.deltaTime;
            }

            _machine.Tick(Time.deltaTime);
            
            var path = StatePath(_machine.Root.Leaf());
            if (path != _lastPath)
            {
                Debug.Log($"{name}: {path}");
                _lastPath = path;
            }
        }
        
        private void FixedUpdate()
        {
            _machine.FixedTick(Time.fixedDeltaTime);

            Vector2 boxSize = Ctx.PlayerBodyCollider.size; // Player's size
            Ctx.CollisionResult = GridPhysics.MoveAndResolve(transform.position, Ctx.Velocity, boxSize, Time.fixedDeltaTime);
            transform.position = Ctx.CollisionResult.NewPosition;
        }

        private void OnJumpPressed()
        {
            bool canJumpFromGround = Ctx.CollisionResult.TouchingBottom;
            bool canJumpFromWater = _machine.Root.Leaf() is PlayerSwimmingState && Ctx.IsHeadAboveWater();

            if (canJumpFromGround || canJumpFromWater)
            {
                Ctx.JumpPressed = true;
            }
        }

        private void OnMoveInputPressed(Vector2 rawMoveInput)
        {
            Ctx.DesiredDirection = rawMoveInput;
        }

        private static string StatePath(State s)
        {
            return string.Join(" > ", s.PathToRoot().Reverse().Select(n => n.GetType().Name));
        }
    }
    
    [Serializable]
    public class PlayerContext
    {
        public Transform VisualsTransform;
    
        [Header("Player Collider")]
        public BoxCollider2D PlayerBodyCollider;
        public Vector2 WalkingBoxColliderSize;
        public Vector2 SwimmingBoxColliderSize;
        
        [Header("Land")]
        public float MoveSpeed = 3.5f;
        public float LandTurnSharpness = 5f;
        public float AirborneMoveSpeedMultiplier = 0.5f;
        
        [Header("Gravity")]
        public float GravityForce = 25f;
        public float TerminalVelocity = -40f;
        
        [Header("Swimming")]
        public float SwimSpeed = 3.5f;
        public float SwimDashSpeed = 15f;
        public float SwimDashCooldown = 3f;
        public float SwimmingTurnSharpness = 5f;
        public float FromWaterJumpSpeed = 10f;
        
        [Header("Jumping")]
        public float MinJumpSpeed = 5f;
        public float MaxJumpHoldDuration = 0.4f;
        public float JumpBufferDuration = 0.15f;
        public float CoyoteTimeBufferDuration = 0.15f;
    
        [HideInInspector] public Transform Transform;
        [HideInInspector] public Vector2 DesiredDirection;
        [HideInInspector] public Vector2 Velocity;
        [HideInInspector] public GridPhysics.CollisionResult CollisionResult;
        [HideInInspector] public bool JumpPressed = false;
        [HideInInspector] public bool WaterJumpBuffered = false;
        [HideInInspector] public float SwimDashCooldownTimer;
        
        public bool IsHeadAboveWater()
        {
            if (Transform.position.y + 1f >= WorldManager.Instance.SeaLevel)
            {
                return true;
            }

            return false;
        }
        
        public bool IsInOcean()
        {
            if(Transform.position.y - 0.6f <= WorldManager.Instance.SeaLevel)
            {
                return true;
            }
            
            return false;
        }

    }
}
