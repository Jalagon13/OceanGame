using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    #region Root State

    [Serializable]
    public class BubbleBuddyRootState : CharacterRootState
    {
        // Child States
        public BubbleBuddyPursuitState Pursuit { get; private set; }
        public BubbleBuddyIdleState Idle { get; private set; }

        [Header("Flight & Pursuit Settings")]
        [Tooltip("How strongly the creature bounces off tiles when colliding (0 = dead stop, 1 = full elastic bounce).")]
        [Range(0f, 1f)]
        public float BounceDamping = 0.6f;

        [Tooltip("Maximum detection range for targeting players. Set to 0 or negative for infinite range.")]
        public float DetectionRadius = 30f;

        [Tooltip("Fallback turn sharpness if CharacterSO is missing BaseTurnSharpness.")]
        public float DefaultTurnSharpness = 3f;

        [Header("Visual Settings")]
        [Tooltip("Whether to orient the VisualsGO towards the current movement direction.")]
        public bool OrientVisualsToVelocity = true;

        [Tooltip("Offset angle in degrees for the visual rotation (e.g., -90 if sprite heads upwards by default).")]
        public float VisualAngleOffset = 0f;

        // Parameterless constructor for Unity serialization
        public BubbleBuddyRootState() : base() { }

        public override void Initialize(ServerCharacter ctx)
        {
            base.Initialize(ctx);

            // Construct child states once the context is available
            Pursuit = new BubbleBuddyPursuitState(null, this, ctx);
            Idle = new BubbleBuddyIdleState(null, this, ctx);
        }

        protected override State GetInitialState()
        {
            return Pursuit;
        }

        /// <summary>
        /// Finds the nearest alive player in the world.
        /// </summary>
        public ServerCharacter FindNearestPlayer()
        {
            if (EntityManager.Instance == null) return null;

            ServerCharacter closestPlayer = null;
            float closestDistanceSqr = DetectionRadius > 0f ? DetectionRadius * DetectionRadius : float.MaxValue;
            Vector2 currentPos = _ctx.transform.position;

            IReadOnlyList<Entity> entities = EntityManager.Instance.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                if (entity == null || entity == _ctx) continue;

                if (entity is ServerCharacter character && character.CompareTag("Player"))
                {
                    if (character.Health != null && character.Health.CurrentLifeState.Value != LifeState.Alive)
                    {
                        continue;
                    }

                    float distSqr = (currentPos - (Vector2)character.transform.position).sqrMagnitude;
                    if (distSqr < closestDistanceSqr)
                    {
                        closestDistanceSqr = distSqr;
                        closestPlayer = character;
                    }
                }
            }

            return closestPlayer;
        }
    }

    #endregion

    #region Pursuit State

    public class BubbleBuddyPursuitState : State
    {
        private readonly ServerCharacter _ctx;
        private readonly BubbleBuddyRootState _root;
        private ServerCharacter _targetPlayer;

        public BubbleBuddyPursuitState(StateMachine m, State parent, ServerCharacter ctx) : base(m, parent)
        {
            _ctx = ctx;
            _root = (BubbleBuddyRootState)parent;
        }

        protected override State GetTransition()
        {
            // Transition to Idle if no target player is found
            if (_targetPlayer == null)
            {
                return _root.Idle;
            }

            return null;
        }

        protected override void OnEnter()
        {
            _targetPlayer = _root.FindNearestPlayer();
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            // Re-acquire nearest player
            _targetPlayer = _root.FindNearestPlayer();
            if (_targetPlayer == null) return;

            // Handle knockback override: don't steer while under external knockback
            if (_ctx.IsKnockedBack)
            {
                return;
            }

            // 1. Calculate Desired Direction towards the player
            Vector2 toPlayer = (Vector2)_targetPlayer.transform.position - (Vector2)_ctx.transform.position;
            if (toPlayer.sqrMagnitude > 0.001f)
            {
                _ctx.DesiredDirection = toPlayer.normalized;
            }

            // 2. Determine target flight velocity and turning rate
            float speed = _ctx.Stats?.MoveSpeed.GetValue() ?? (_ctx.Data != null ? _ctx.Data.BaseSpeed : 4f);
            float turnSharpness = _ctx.Data != null && _ctx.Data.BaseTurnSharpness > 0f
                ? _ctx.Data.BaseTurnSharpness
                : _root.DefaultTurnSharpness;

            Vector2 targetVelocity = _ctx.DesiredDirection * speed;

            // 3. Steer velocity slowly towards target velocity (Terraria-style arcing pursuit)
            _ctx.Velocity = Vector2.MoveTowards(_ctx.Velocity, targetVelocity, turnSharpness * fixedDeltaTime);

            // 4. Handle light bounces against solid tiles
            HandleTileBounces();

            // 5. Update visual rotation if enabled
            UpdateVisuals();
        }

        private void HandleTileBounces()
        {
            var collision = _ctx.CollisionResult;
            float bounce = _root.BounceDamping;

            // Horizontal tile collisions (reflect X velocity lightly)
            if (collision.TouchingLeft && _ctx.Velocity.x < 0f)
            {
                _ctx.Velocity.x = -_ctx.Velocity.x * bounce;
            }
            else if (collision.TouchingRight && _ctx.Velocity.x > 0f)
            {
                _ctx.Velocity.x = -_ctx.Velocity.x * bounce;
            }

            // Vertical tile collisions (reflect Y velocity lightly)
            if (collision.TouchingBottom && _ctx.Velocity.y < 0f)
            {
                _ctx.Velocity.y = -_ctx.Velocity.y * bounce;
            }
            else if (collision.TouchingTop && _ctx.Velocity.y > 0f)
            {
                _ctx.Velocity.y = -_ctx.Velocity.y * bounce;
            }
        }

        private void UpdateVisuals()
        {
            if (!_root.OrientVisualsToVelocity || _ctx.VisualsGO == null || _ctx.Velocity.sqrMagnitude < 0.05f)
            {
                return;
            }

            float angle = Mathf.Atan2(_ctx.Velocity.y, _ctx.Velocity.x) * Mathf.Rad2Deg + _root.VisualAngleOffset;
            _ctx.VisualsGO.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    #endregion

    #region Idle State

    public class BubbleBuddyIdleState : State
    {
        private readonly ServerCharacter _ctx;
        private readonly BubbleBuddyRootState _root;
        private float _searchTimer;

        public BubbleBuddyIdleState(StateMachine m, State parent, ServerCharacter ctx) : base(m, parent)
        {
            _ctx = ctx;
            _root = (BubbleBuddyRootState)parent;
        }

        protected override State GetTransition()
        {
            // If a player is found, switch back to Pursuit
            if (_root.FindNearestPlayer() != null)
            {
                return _root.Pursuit;
            }

            return null;
        }

        protected override void OnEnter()
        {
            _ctx.DesiredDirection = Vector2.zero;
            _searchTimer = 0f;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            if (_ctx.IsKnockedBack) return;

            // Slowly drift to a stop when idle
            _ctx.Velocity = Vector2.MoveTowards(_ctx.Velocity, Vector2.zero, 2f * fixedDeltaTime);

            // Periodically check for nearby players
            _searchTimer -= fixedDeltaTime;
            if (_searchTimer <= 0f)
            {
                _searchTimer = 0.5f;
                if (_root.FindNearestPlayer() != null)
                {
                    Machine.Sequencer.RequestTransition(this, _root.Pursuit);
                }
            }
        }
    }

    #endregion
}
