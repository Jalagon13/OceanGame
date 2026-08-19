using System;
using UnityEngine;
using UnityEngine.AI;

namespace LittleGuyGamePrototype
{
    #region Root State

    public class SoldierRootState : State
    {
        public readonly SoldierGroundedState Grounded;
        public readonly SoldierAirborneState Airborne;

        private readonly SoldierContext _ctx;

        public SoldierRootState(StateMachine m, SoldierContext ctx) : base(m, null)
        {
            this._ctx = ctx;
            Grounded = new SoldierGroundedState(m, this, ctx);
            Airborne = new SoldierAirborneState(m, this, ctx);
        }

        protected override State GetInitialState() => Grounded;
        protected override State GetTransition()
        {
            return _ctx.Grounded ? null : Airborne;
        }
    }
    
    #endregion
    
    #region Airborne State

    public class SoldierAirborneState : State
    {
        private readonly SoldierContext _ctx;

        public SoldierAirborneState(StateMachine m, State parent, SoldierContext ctx) : base(m, parent)
        {
            this._ctx = ctx;
        }

        protected override State GetTransition()
        {
            return _ctx.Grounded ? ((SoldierRootState)Parent).Grounded : null;
        }
    }
    
    #endregion
    
    #region Grounded State

    public class SoldierGroundedState : State
    {
        public readonly SoldierIdleState Idle;
        public readonly SoldierMoveState Move;
        public readonly SoldierPursueState Pursue;
        public readonly SoldierKnockbackState Knockback;
        
        private readonly SoldierContext _ctx;

        public SoldierGroundedState(StateMachine m, State parent, SoldierContext ctx) : base(m, parent)
        {
            this._ctx = ctx;
            Idle = new SoldierIdleState(m, this, _ctx);
            Move = new SoldierMoveState(m, this, _ctx);
            Pursue = new SoldierPursueState(m, this, _ctx);
            Knockback = new SoldierKnockbackState(m, this, _ctx);
        }

        protected override State GetInitialState() => Idle;

        protected override State GetTransition()
        {
            return !_ctx.Grounded ? ((SoldierRootState)Parent).Airborne : null;
        }
    }
    
    #endregion
    
    #region Idle State

    public class SoldierIdleState : State
    {
        private readonly SoldierContext _ctx;
        private float _timer;
        private float _idleDuration => UnityEngine.Random.Range(_ctx.Data.MinIdleTime, _ctx.Data.MaxIdleTime);

        public SoldierIdleState(StateMachine m, State parent, SoldierContext ctx) : base(m, parent)
        {
            this._ctx = ctx;
        }

        protected override State GetTransition()
        {
            if(_ctx.IsKnockedBack)
            {
                return ((SoldierGroundedState)Parent).Knockback;
            }

            if (_ctx.TargetSoldier != null && _ctx.TargetSoldier.gameObject.activeSelf == true)
            {
                return ((SoldierGroundedState)Parent).Pursue;
            }

            if (_timer <= 0f)
            {
                return ((SoldierGroundedState)Parent).Move;
            }
            
            return null;
        }

        protected override void OnEnter()
        {
            _timer = _idleDuration;
        }
        
        protected override void OnUpdate(float deltaTime)
        {
            _timer -= deltaTime;
            if(_timer <= 0f)
            {
                _timer = 0;
            }
        }
    }
    
    #endregion
    
    #region Move State

    public class SoldierMoveState : State
    {
        private readonly SoldierContext _ctx;
        private bool _foundValidPath;

        public SoldierMoveState(StateMachine m, State parent, SoldierContext ctx) : base(m, parent)
        {
            this._ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (_ctx.IsKnockedBack)
            {
                return ((SoldierGroundedState)Parent).Knockback;
            }

            if (_ctx.TargetSoldier != null && _ctx.TargetSoldier.gameObject.activeSelf == true)
            {
                return ((SoldierGroundedState)Parent).Pursue;
            }

            if (!_foundValidPath)
            {
                return ((SoldierGroundedState)Parent).Idle;
            }
            
            if(!_ctx.Agent.pathPending && _ctx.Agent.remainingDistance <= _ctx.Agent.stoppingDistance)
            {
                return ((SoldierGroundedState)Parent).Idle;
            }
            
            return null;
        }
        
        protected override void OnEnter()
        {
            _foundValidPath = false;
            
            // Find a random path to walk towards
            Vector3 randomTarget;
            
            if(!TryGetRandomNavMeshPoint(out randomTarget))
            {
                return; // Could not find a valid random point
            }
            
            // Check if it is possible to walk to that path, if not, try again until a valid path is found or a maximum number of attempts is reached
            if(!IsDestinationReachable(randomTarget))
            {
                return; // Could not find a valid path
            }

            // If a valid path is found, set _foundValidPath to true
            _foundValidPath = true;

            // Fetch the NavMeshAgent component and set its destination to the found path
            _ctx.Agent.speed = _ctx.Data.MoveSpeed;
            _ctx.Agent.SetDestination(randomTarget);
        }

        private bool IsDestinationReachable(Vector3 randomTarget)
        {
            NavMeshPath path = new NavMeshPath();
            
            _ctx.Agent.CalculatePath(randomTarget, path);
            
            if(path.status == NavMeshPathStatus.PathComplete)
            {
                return true;
            }

            return false;
        }

        private bool TryGetRandomNavMeshPoint(out Vector3 randomTarget)
        {
            float radius = _ctx.Data.WanderDestinationSearchRadius;
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
            randomDirection += _ctx.Transform.position;

            NavMeshHit hit;
            
            if(NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                randomTarget = hit.position;
                return true;
            }
            
            randomTarget = Vector3.zero;
            return false;
        }

        protected override void OnExit()
        {
            _foundValidPath = false;
        }
    }
    
    #endregion

    #region Pursue State

    public class SoldierPursueState : State
    {
        private SoldierContext _ctx;
        private float _attackCooldown => UnityEngine.Random.Range(0.025f, 0.15f);
        private float _attackCooldownTimer;

        public SoldierPursueState(StateMachine m, State parent, SoldierContext ctx) : base(m, parent)
        {
            this._ctx = ctx;
        }

        protected override State GetTransition()
        {
            if (_ctx.IsKnockedBack)
            {
                return ((SoldierGroundedState)Parent).Knockback;
            }

            if (_ctx.TargetSoldier == null || _ctx.TargetSoldier.gameObject.activeSelf == false)
            {
                return UnityEngine.Random.value < 0.5f ? ((SoldierGroundedState)Parent).Move : ((SoldierGroundedState)Parent).Idle;
            }

            return null;
        }

        protected override void OnEnter()
        {
            _ctx.Agent.speed = _ctx.Data.MoveSpeed;

            _attackCooldownTimer = _attackCooldown;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_ctx.TargetSoldier != null && _ctx.TargetSoldier.gameObject.activeSelf == true)
            {
                _ctx.Agent.SetDestination(_ctx.TargetSoldier.transform.position);
            }

            _attackCooldownTimer -= deltaTime;
            if (_attackCooldownTimer <= 0f)
            {
                TryToAttack();
            }
        }

        private void TryToAttack()
        {
            // Attack logic
            var colliders = Physics.OverlapBox(_ctx.AttackCollider.bounds.center, _ctx.AttackCollider.bounds.extents);
            foreach(var c in colliders)
            {
                if(c.transform.root.TryGetComponent(out Soldier soldier))
                {
                    if(soldier == _ctx.TargetSoldier)
                    {
                        soldier.Ctx.HealthHandler.TryTakeDamage(_ctx.Transform.gameObject, _ctx.Data.BaseDamage, _ctx.Data.CanKnockback, _ctx.Data.KnockbackForce);
                        
                        _attackCooldownTimer = _attackCooldown;
                        return;
                    }
                }
            }

            _attackCooldownTimer = _attackCooldown;
        }
    }
    
    #endregion

    #region Knockback State

    public class SoldierKnockbackState : State
    {
        private SoldierContext _ctx;
        private float _timer;

        public SoldierKnockbackState(StateMachine m, State parent, SoldierContext ctx) : base(m, parent)
        {
            this._ctx = ctx;
        }

        protected override State GetTransition()
        {
            if(_timer > 0f) return null;
            
            bool isVelocityNearZero = _ctx.Rb.linearVelocity.sqrMagnitude <= 2f;

            if (isVelocityNearZero)
            {
                if (_ctx.TargetSoldier != null && _ctx.TargetSoldier.gameObject.activeSelf == true)
                {
                    return ((SoldierGroundedState)Parent).Pursue;
                }

                return UnityEngine.Random.value < 0.5f ? ((SoldierGroundedState)Parent).Idle : ((SoldierGroundedState)Parent).Move;
            }

            return null;
        }

        protected override void OnEnter()
        {
            _timer = _ctx.Data.IFrameDuration;

            GameObject inflictor = _ctx.DamageInfo.Inflicter;
            GameObject self = _ctx.Transform.gameObject;
            float knockbackForce = _ctx.DamageInfo.KnockbackForce;

            _ctx.Agent.enabled = false;
            _ctx.Rb.isKinematic = false;

            // 1. Get the pure horizontal direction (flattened on Y)
            Vector3 horizontalDirection = self.transform.position - inflictor.transform.position;
            horizontalDirection.y = 0f;
            horizontalDirection = horizontalDirection.normalized;

            // 2. Define a fixed upward push value (Tweak this to change the jump height)
            float upwardForceAmount = 3f;
            Vector3 upwardDirection = Vector3.up * upwardForceAmount;

            // 3. Combine horizontal momentum with the upward burst
            Vector3 finalForce = (horizontalDirection * knockbackForce) + upwardDirection;

            // 4. Reset existing vertical velocity first so consecutive hits don't skyrocket the NPC
            Vector3 currentVelocity = _ctx.Rb.linearVelocity; // Use .velocity in older Unity versions
            currentVelocity.y = 0f;
            _ctx.Rb.linearVelocity = currentVelocity;

            // 5. Apply the standard Minecraft-style impulse knockback
            _ctx.Rb.AddForce(finalForce, ForceMode.Impulse);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_timer > 0f)
            {
                _timer -= deltaTime;
            }
        }

        protected override void OnExit()
        {
            _ctx.IsKnockedBack = false;
            _ctx.Rb.linearVelocity = Vector3.zero;
            _ctx.Rb.isKinematic = true;
            _ctx.Agent.Warp(_ctx.Transform.position);
            _ctx.Agent.enabled = true;
        }
    }

    #endregion

}