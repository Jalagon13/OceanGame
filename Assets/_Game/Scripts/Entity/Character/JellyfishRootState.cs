using System;
using UnityEngine;

namespace OceanGame
{
    #region Root State

    [Serializable]
    public class JellyfishRootState : CharacterRootState
    {
        // Child States
        public JellyfishSwimmingState Swimming { get; private set; }
        public JellyfishAirborneState Airborne { get; private set; }
        
        // Variables editable directly in the ServerCharacter prefab Inspector!
        [Header("Dash Settings")]
        public float DashStrength = 6f;
        public float DashDeceleration = 4f;
        public float TimeBetweenDashes = 1.5f;
        
        [Header("Air / Gravity Settings")]
        public float GravityForce = 20f;
        public float TerminalVelocity = -18f;
        
        // Parameterless constructor for Unity serialization
        public JellyfishRootState() : base() { }
        
        public override void Initialize(ServerCharacter ctx)
        {
            base.Initialize(ctx);
            // Construct child states once the context is available
            Swimming = new JellyfishSwimmingState(null, this, ctx);
            Airborne = new JellyfishAirborneState(null, this, ctx);
        }
        
        protected override State GetInitialState()
        {
            return IsInWater() ? Swimming : Airborne;
        }
        
        public bool IsInWater()
        {
            if (WorldManager.Instance == null || WorldManager.Instance.FluidGrid == null)
            {
                return false;
            }
            int tileX = Mathf.FloorToInt(_ctx.transform.position.x);
            int tileY = Mathf.FloorToInt(_ctx.transform.position.y);
            return WorldManager.Instance.FluidGrid.GetFluidType(tileX, tileY) == FluidType.Water;
        }
    }

    #endregion

    #region Swimming State

    public class JellyfishSwimmingState : State
    {
        private readonly ServerCharacter _ctx;
        private readonly JellyfishRootState _root;
        private float _dashCooldownTimer;

        public JellyfishSwimmingState(StateMachine m, State parent, ServerCharacter ctx) : base(m, parent)
        {
            _ctx = ctx;
            _root = (JellyfishRootState)parent;
        }

        protected override State GetTransition()
        {
            // If we leave the water, switch to airborne state
            if (!_root.IsInWater())
            {
                return _root.Airborne;
            }

            return null;
        }

        protected override void OnEnter()
        {
            // Begin with a short pause or initiate the first dash immediately
            _dashCooldownTimer = 0.2f;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            // Do not override velocity if the entity is currently taking knockback
            if (_ctx.IsKnockedBack)
            {
                return;
            }

            // Handle Wall Bounces (Reflection)
            HandleWallReflection();

            // Decelerate towards zero
            if (_ctx.Velocity.sqrMagnitude > 0.001f)
            {
                _ctx.Velocity = Vector2.MoveTowards(_ctx.Velocity, Vector2.zero, _root.DashDeceleration * fixedDeltaTime);
            }
            else
            {
                _ctx.Velocity = Vector2.zero;

                // Dash Cycle Timer (when completely halted)
                _dashCooldownTimer -= fixedDeltaTime;
                if (_dashCooldownTimer <= 0f)
                {
                    ExecuteRandomDash();
                    _dashCooldownTimer = _root.TimeBetweenDashes;
                }
            }

            // Orient visuals towards movement direction (optional visual polish)
            if (_ctx.VisualsGO != null && _ctx.Velocity.sqrMagnitude > 0.05f)
            {
                float targetAngle = Mathf.Atan2(_ctx.Velocity.y, _ctx.Velocity.x) * Mathf.Rad2Deg - 90f;
                _ctx.VisualsGO.transform.rotation = Quaternion.Euler(0, 0, targetAngle);
            }
        }

        private void ExecuteRandomDash()
        {
            // Pick a random normalized 2D direction
            Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
            if (randomDir == Vector2.zero)
            {
                randomDir = Vector2.up;
            }

            _ctx.Velocity = randomDir * _root.DashStrength;
        }

        private void HandleWallReflection()
        {
            var collision = _ctx.CollisionResult;

            // Horizontal bounces
            if (collision.TouchingLeft && _ctx.Velocity.x < 0f)
            {
                _ctx.Velocity.x = -_ctx.Velocity.x;
            }
            else if (collision.TouchingRight && _ctx.Velocity.x > 0f)
            {
                _ctx.Velocity.x = -_ctx.Velocity.x;
            }

            // Vertical bounces
            if (collision.TouchingBottom && _ctx.Velocity.y < 0f)
            {
                _ctx.Velocity.y = -_ctx.Velocity.y;
            }
            else if (collision.TouchingTop && _ctx.Velocity.y > 0f)
            {
                _ctx.Velocity.y = -_ctx.Velocity.y;
            }
        }
    }

    #endregion

    #region Airborne State

    public class JellyfishAirborneState : State
    {
        private readonly ServerCharacter _ctx;
        private readonly JellyfishRootState _root;

        public JellyfishAirborneState(StateMachine m, State parent, ServerCharacter ctx) : base(m, parent)
        {
            _ctx = ctx;
            _root = (JellyfishRootState)parent;
        }

        protected override State GetTransition()
        {
            // Return to swimming once back in water
            if (_root.IsInWater())
            {
                return _root.Swimming;
            }

            return null;
        }

        protected override void OnEnter()
        {
            // Reset visual orientation upright when out of water
            if (_ctx.VisualsGO != null)
            {
                _ctx.VisualsGO.transform.up = Vector2.up;
            }
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            // Handle knockback gravity override if active
            if (_ctx.IsKnockedBack)
            {
                _ctx.KnockbackVelocity.y -= _root.GravityForce * fixedDeltaTime;
                if (_ctx.KnockbackVelocity.y < _root.TerminalVelocity)
                {
                    _ctx.KnockbackVelocity.y = _root.TerminalVelocity;
                }
                
                return;
            }

            // Apply gravity downwards
            _ctx.Velocity.y -= _root.GravityForce * fixedDeltaTime;
            if (_ctx.Velocity.y < _root.TerminalVelocity)
            {
                _ctx.Velocity.y = _root.TerminalVelocity;
            }

            // Ground and ceiling stop checks
            if (_ctx.CollisionResult.TouchingBottom && _ctx.Velocity.y < 0f)
            {
                _ctx.Velocity.y = 0f;
            }
            if (_ctx.CollisionResult.TouchingTop && _ctx.Velocity.y > 0f)
            {
                _ctx.Velocity.y = 0f;
            }

            // Slow down any remaining horizontal drift while airborne
            _ctx.Velocity.x = Mathf.MoveTowards(_ctx.Velocity.x, 0f, _root.DashDeceleration * fixedDeltaTime);
        }
    }

    #endregion
}