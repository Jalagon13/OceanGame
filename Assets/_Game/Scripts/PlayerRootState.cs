using UnityEngine;

namespace OceanGame
{
    #region Root State

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
            return _ctx.Swimming ? Swimming : _ctx.Grounded ? Grounded : Airborne;
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
            return null; // WIP
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
            return null; // WIP
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
            return null; // WIP
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
            return null; // WIP
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
            return null; // WIP
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
