using System;
using Unity.VisualScripting;
using UnityEngine;

namespace OceanGame
{
    #region Root State    

    public class ItemRootState : State
    {
        public readonly ItemGroundedState Grounded;
        public readonly ItemAirborneState Airborne;
        public readonly ItemAttractState Attract;
        
        private readonly ItemContext _ctx;
        
        public ItemRootState(StateMachine m, ItemContext ctx) : base(m, null)
        {
            _ctx = ctx;
            
            Grounded = new ItemGroundedState(m, this, ctx);
            Airborne = new ItemAirborneState(m, this, ctx);
            Attract = new ItemAttractState(m, this, ctx);
        }

        protected override State GetInitialState() => Grounded;

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            _ctx.Velocity.x = Mathf.Lerp(_ctx.Velocity.x, 0, fixedDeltaTime * _ctx.ThrowAirResistance);
        }
    }
    
    #endregion
    
    #region Grounded State
    
    public class ItemGroundedState : State
    {
        private readonly ItemContext _ctx;
    
        public ItemGroundedState(StateMachine m, State parent, ItemContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if(_ctx.CanBeCollected && _ctx.ClosestPlayer != null && _ctx.ItemSlot != null && !_ctx.HasBeenCollected)
            {
                return (Parent as ItemRootState).Attract;
            }
        
            if(!_ctx.CollisionResult.TouchingBottom)
            {
                return (Parent as ItemRootState).Airborne;
            }
        
            return null;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            _ctx.Velocity.y = -0.1f; // To firmly keep the Item firmly pressed to the floor
        }
    }

    #endregion

    #region Airborne State

    public class ItemAirborneState : State
    {
        private readonly ItemContext _ctx;

        public ItemAirborneState(StateMachine m, State parent, ItemContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (_ctx.CanBeCollected && _ctx.ClosestPlayer != null && _ctx.ItemSlot != null && !_ctx.HasBeenCollected)
            {
                return (Parent as ItemRootState).Attract;
            }

            if (_ctx.CollisionResult.TouchingBottom && _ctx.Velocity.y <= 0f)
            {
                return (Parent as ItemRootState).Grounded;
            }

            return null;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            _ctx.Velocity.y -= _ctx.GravityForce * fixedDeltaTime;

            if (_ctx.Velocity.y < _ctx.TerminalVelocity)
            {
                _ctx.Velocity.y = _ctx.TerminalVelocity;
            }
        }
    }

    #endregion

    #region Attract State

    public class ItemAttractState : State
    {
        private readonly ItemContext _ctx;

        public ItemAttractState(StateMachine m, State parent, ItemContext ctx) : base(m, parent)
        {
            _ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (!_ctx.CanBeCollected || _ctx.ClosestPlayer == null)
            {
                return _ctx.CollisionResult.TouchingBottom ? (Parent as ItemRootState).Grounded : (Parent as ItemRootState).Airborne;
            }
            
            return null;
        }

        protected override void OnEnter()
        {
            _ctx.IgnoreCollisions = true;
        }

        protected override void OnExit()
        {
            _ctx.IgnoreCollisions = false;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            if(_ctx.HasBeenCollected) return;
        
            _ctx.DesiredDirection = _ctx.ClosestPlayer.transform.position - _ctx.Transform.position;
            _ctx.DesiredDirection.Normalize();
            
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, _ctx.DesiredDirection * _ctx.AttractSpeed, _ctx.TurnSharpness * fixedDeltaTime);
            
            // After moving it, check to see if it is in collect range
            var distance = Vector2.Distance(_ctx.Transform.position, _ctx.ClosestPlayer.transform.position);
            if(distance <= _ctx.CollectRange && _ctx.CanBeCollected && _ctx.ItemSlot != null)
            {
                _ctx.Item.OnItemCollected();
            }
        }
    }

    #endregion
}