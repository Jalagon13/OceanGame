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

        protected override State GetTransition()
        {
            if (_ctx.Swimming && ActiveChild != Swimming) return Swimming;

            return null;
        }
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
        private float _jumpHoldTracker; // Used for keeping track of how long jump has been held.
        private bool _jumpHoldEnded;
        private float _jumpBufferTimer; // Used for keeping track of how long the player can buffer a jump after pressing jump while airborne

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
        
            return null;
        }

        protected override void OnEnter()
        {
            _jumpBufferTimer = 0;

            // On Jump Animation set here
            if (GameInput.Instance.JumpHold && _ctx.Velocity.y > 0)
            {
                _jumpHoldTracker = _ctx.MaxJumpHoldDuration;
                _jumpHoldEnded = false;
            }
            
            GameInput.Instance.JumpPressed += OnJumpPressed;
        }

        protected override void OnExit()
        {
            _jumpHoldTracker = 0;
            _jumpBufferTimer = 0;
            
            GameInput.Instance.JumpPressed -= OnJumpPressed;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            if(_jumpBufferTimer > 0)
            {
                _jumpBufferTimer -= fixedDeltaTime;
            }
        
            // TODO Make it so when you tap jump you do the min jump speed but when you hold it down for like 0.25 seconds you jump to max jump speed somehow
            if(GameInput.Instance.JumpHold && _ctx.Velocity.y > 0 && _jumpHoldTracker > 0 && !_jumpHoldEnded)
            {
                _jumpHoldTracker -= fixedDeltaTime;
                
                _ctx.Velocity.y = _ctx.MinJumpSpeed;
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
            _ctx.Velocity.x = Mathf.Lerp(_ctx.Velocity.x, _ctx.DesiredDirection.x * currentSpeed, fixedDeltaTime * _ctx.TurnSharpness);
        }

        private void OnJumpPressed()
        {
            _jumpBufferTimer = _ctx.JumpBufferDuration;
        }

    }

    #endregion

    #region Swimming State

    public class PlayerSwimmingState : State
    {
        private readonly PlayerContext _ctx;

        public PlayerSwimmingState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            return null;
        }

        protected override void OnEnter()
        {
            // On Swimming Animation set here
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
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, _ctx.DesiredDirection * currentSpeed, fixedDeltaTime * _ctx.TurnSharpness);
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
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, _ctx.DesiredDirection * currentSpeed, fixedDeltaTime * _ctx.TurnSharpness);
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
