using System;
using UnityEngine;

namespace OceanGame
{
    #region Root State

    // Root state remains active permanently
    public class PlayerRootState : State
    {
        public readonly PlayerGroundedState Grounded;
        public readonly PlayerAirborneState Airborne;
        public readonly PlayerSwimmingState Swimming;


        private readonly PlayerContext _ctx;

        public PlayerRootState(StateMachine m, PlayerContext ctx) : base(m, null)
        {
            _ctx = ctx;
            Grounded = new PlayerGroundedState(m, this, ctx);
            Airborne = new PlayerAirborneState(m, this, ctx);
            Swimming = new PlayerSwimmingState(m, this, ctx);
        }

        protected override State GetInitialState() => Grounded;

        // protected override State GetTransition()
        // {
        //     if (_ctx.IsInOcean() && ActiveChild != Swimming) return Swimming;

        //     return null;
        // }
    }

    #endregion

    #region Grounded State

    public class PlayerGroundedState : State
    {
        public readonly PlayerIdleState Idle;
        public readonly PlayerMoveState Move;
        public readonly PlayerKnockbackState Knockback;
    
        private readonly PlayerContext _ctx;
    
        public PlayerGroundedState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;

            Idle = new PlayerIdleState(m, this, _ctx);
            Move = new PlayerMoveState(m, this, _ctx);
            Knockback = new PlayerKnockbackState(m, this, _ctx);
        }

        protected override State GetInitialState() => Idle;

        protected override State GetTransition()
        {
            if(_ctx.JumpPressed)
            {
                _ctx.JumpPressed = false;
                _ctx.Velocity.y = _ctx.MinJumpSpeed;
                
                return (Parent as PlayerRootState).Airborne;
            }
        
            if(!_ctx.CollisionResult.TouchingBottom)
            {
                return (Parent as PlayerRootState).Airborne;
            }
        
            return null;
        }

        protected override void OnEnter()
        {
            // On Grounded Animation set here
            _ctx.PlayerBodyCollider.size = _ctx.WalkingBoxColliderSize;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            _ctx.Velocity.y = -0.1f; // To firmly keep the character firmly pressed to the floor
        }
    }

    #endregion

    #region Airborne State

    public class PlayerAirborneState : State
    {
        private readonly PlayerContext _ctx;
        private float _jumpHoldTimer; // Used for keeping track of how long jump has been held.
        private bool _jumpHoldEnded;
        private float _jumpBufferTimer; // Used for keeping track of how long the player can buffer a jump after pressing jump while airborne
        private float _coyoteTimer;
        private bool _coyoteJumpRequested;
        private bool _topTouchedFlag; // Used for checking during this ariborne state if the player's head hit a ceiling once.

        public PlayerAirborneState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if(_ctx.CollisionResult.TouchingBottom && _ctx.Velocity.y <= 0f)
            {
                if (_jumpBufferTimer > 0f)
                {
                    _ctx.JumpPressed = true;
                }

                return (Parent as PlayerRootState).Grounded;
            }
            
            if(_ctx.IsInOcean())
            {
                if (_jumpBufferTimer > 0f)
                {
                    _ctx.WaterJumpBuffered = true;
                }

                return (Parent as PlayerRootState).Swimming;
            }
        
            return null;
        }

        protected override void OnEnter()
        {
            _ctx.PlayerBodyCollider.size = _ctx.WalkingBoxColliderSize;

            _jumpBufferTimer = 0;
            _coyoteTimer = _ctx.Velocity.y <= 0f ? _ctx.CoyoteTimeBufferDuration : 0f;
            _coyoteJumpRequested = false;
            _topTouchedFlag = false;

            // On Jump Animation set here
            if (GameInput.Instance.JumpHold && _ctx.Velocity.y > 0)
            {
                _jumpHoldTimer = _ctx.MaxJumpHoldDuration;
                _jumpHoldEnded = false;
            }
            
            GameInput.Instance.OnJumpPressed += OnJumpPressed;
        }

        protected override void OnExit()
        {
            _jumpHoldTimer = 0;
            _jumpBufferTimer = 0;
            _coyoteTimer = 0;
            _coyoteJumpRequested = false;
            _topTouchedFlag = false;

            GameInput.Instance.OnJumpPressed -= OnJumpPressed;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            if (_coyoteTimer > 0f)
            {
                _coyoteTimer -= fixedDeltaTime;
            }

            if (_coyoteJumpRequested)
            {
                _coyoteJumpRequested = false;
                _ctx.Velocity.y = _ctx.MinJumpSpeed;
                _jumpHoldTimer = GameInput.Instance.JumpHold ? _ctx.MaxJumpHoldDuration : 0f;
                _jumpHoldEnded = !GameInput.Instance.JumpHold;
            }

            if (_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= fixedDeltaTime;
            }

            if (_ctx.CollisionResult.TouchingTop && !_topTouchedFlag) // Player bangs his head during jump, set velocity to -0.1 once
            {
                _jumpHoldEnded = true;
                _topTouchedFlag = true;
                _ctx.Velocity.y = -0.1f;
            }

            if (GameInput.Instance.JumpHold && _ctx.Velocity.y > 0 && _jumpHoldTimer > 0 && !_jumpHoldEnded)
            {
                _jumpHoldTimer -= fixedDeltaTime;
                
                // Here we do not set velocity y because the jump value from grounded and swimming are different values so just keep the y the same as what it was set to prior to switching to this state
                // _ctx.Velocity.y = _ctx.Velocity.y;
            }
            else
            {
                _jumpHoldEnded = true;

                _ctx.Velocity.y -= _ctx.GravityForce * fixedDeltaTime;

                if (_ctx.Velocity.y < _ctx.TerminalVelocity)
                {
                    _ctx.Velocity.y = _ctx.TerminalVelocity;
                }
            }
        
            // Allow horizontal movement while airborne
            var currentSpeed = _ctx.MoveSpeed * _ctx.AirborneMoveSpeedMultiplier;
            _ctx.Velocity.x = Mathf.Lerp(_ctx.Velocity.x, _ctx.DesiredDirection.x * currentSpeed, fixedDeltaTime * _ctx.LandTurnSharpness);
        }

        private void OnJumpPressed()
        {
            if (_coyoteTimer > 0f)
            {
                _coyoteJumpRequested = true;
                return;
            }

            _jumpBufferTimer = _ctx.JumpBufferDuration;
        }

    }

    #endregion

    #region Swimming State

    public class PlayerSwimmingState : State
    {
        private readonly PlayerContext _ctx;
        
        private Vector2 _desiredVisualRotation;

        public PlayerSwimmingState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (_ctx.IsHeadAboveWater() && (_ctx.JumpPressed || _ctx.WaterJumpBuffered))
            {
                _ctx.JumpPressed = false;
                _ctx.WaterJumpBuffered = false;
                _ctx.Velocity.y = _ctx.FromWaterJumpSpeed;

                return (Parent as PlayerRootState).Airborne;
            }
        
            if(!_ctx.IsInOcean())
            {
                if(!_ctx.CollisionResult.TouchingBottom)
                {
                    return (Parent as PlayerRootState).Airborne;
                }
                else
                {
                    return (Parent as PlayerRootState).Grounded;
                }
            }
        
            return null;
        }

        protected override void OnEnter()
        {
            _ctx.PlayerBodyCollider.size = _ctx.SwimmingBoxColliderSize;

            GameInput.Instance.OnJumpPressed += ExecuteDash;
        }

        protected override void OnExit()
        {
            _ctx.VisualsTransform.up = Vector2.up;

            GameInput.Instance.OnJumpPressed -= ExecuteDash;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            var currentSpeed = _ctx.SwimSpeed;
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, _ctx.DesiredDirection * currentSpeed, fixedDeltaTime * _ctx.SwimmingTurnSharpness);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if(_ctx.Velocity.sqrMagnitude <= 1f && _ctx.DesiredDirection == Vector2.zero)
            {
                _desiredVisualRotation = Vector2.up;
            }
            else
            {
                _desiredVisualRotation = _ctx.DesiredDirection;
            }
            
            _ctx.VisualsTransform.up = Vector2.Lerp(_ctx.VisualsTransform.up, _desiredVisualRotation, 15f * deltaTime).normalized;
        }

        private void ExecuteDash()
        {
            if (_ctx.SwimDashCooldownTimer > 0f || _ctx.IsHeadAboveWater()) // If head is above water, do not execute dash only dash when i am underwater
            {
                return;
            }

            _ctx.Velocity = _ctx.DesiredDirection * _ctx.SwimDashSpeed;
            _ctx.SwimDashCooldownTimer = _ctx.SwimDashCooldown;
        }
    }

    #endregion

    #region Idle State

    public class PlayerIdleState : State
    {
        private readonly PlayerContext _ctx;

        public PlayerIdleState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (!_ctx.CollisionResult.TouchingBottom)
            {
                return ((PlayerGroundedState)Parent).Parent is PlayerRootState root ? root.Airborne : null;
            }

            if (_ctx.Velocity.sqrMagnitude > 0.1f)
            {
                return ((PlayerGroundedState)Parent).Move;
            }
        
            return null;
        }

        protected override void OnEnter()
        {
            _ctx.Velocity.y = 0f;
        }
        
        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            var currentSpeed = _ctx.MoveSpeed;
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, _ctx.DesiredDirection * currentSpeed, fixedDeltaTime * _ctx.LandTurnSharpness);
        }
    }

    #endregion

    #region Move State

    public class PlayerMoveState : State
    {
        private readonly PlayerContext _ctx;

        public PlayerMoveState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (!_ctx.CollisionResult.TouchingBottom)
            {
                return ((PlayerGroundedState)Parent).Parent is PlayerRootState root ? root.Airborne : null;
            }
        
            if (_ctx.Velocity.sqrMagnitude <= 0.1f)
            {
                return ((PlayerGroundedState)Parent).Idle;
            }

            return null; 
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            var currentSpeed = _ctx.MoveSpeed;
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, _ctx.DesiredDirection * currentSpeed, fixedDeltaTime * _ctx.LandTurnSharpness);
        }
    }

    #endregion

    #region Knockback State

    public class PlayerKnockbackState : State
    {
        private readonly PlayerContext _ctx;

        public PlayerKnockbackState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            return null; // WIP
        }
    }

    #endregion
}
