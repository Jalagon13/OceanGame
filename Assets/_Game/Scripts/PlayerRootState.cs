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
            if (_ctx.Swimming) return Swimming;

            if (_ctx.JumpPressed) return null;

            return _ctx.CollisionResult.TouchingBottom ? null : Airborne;
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
                _ctx.Velocity.y = _ctx.JumpSpeed;
                
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

        public PlayerAirborneState(StateMachine m, State parent, PlayerContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if(_ctx.CollisionResult.TouchingBottom && _ctx.Velocity.y <= 0f)
            {
                return (Parent as PlayerRootState).Grounded;
            }
        
            return null;
        }

        protected override void OnEnter()
        {
            // On Jump Animation set here
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            var currentSpeed = _ctx.MoveSpeed * _ctx.AirborneMoveSpeedMultiplier;
            _ctx.Velocity.x = Mathf.Lerp(_ctx.Velocity.x, _ctx.DesiredDirection.x * currentSpeed, fixedDeltaTime * _ctx.TurnSharpness);
            
            _ctx.Velocity.y -= _ctx.GravityForce * fixedDeltaTime;
            
            if(_ctx.Velocity.y < _ctx.TerminalVelocity)
            {
                _ctx.Velocity.y = _ctx.TerminalVelocity;
            }
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
