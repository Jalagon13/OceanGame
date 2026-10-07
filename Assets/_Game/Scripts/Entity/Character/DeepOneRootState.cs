using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    #region Root State

    [Serializable]
    public class DeepOneRootState : CharacterRootState
    {
        [Header("Detection")]
        public float DetectionRangeTiles = 100f;

        [Header("Movement")]
        public float MoveSpeed = 3f;
        public float JumpSpeed = 8f;
        
        [Header("Gravity")]
        public float GravityForce = 20f;
        public float TerminalVelocity = -18f;

        public DeepOnePursuingState Pursuing { get; private set; }

        public DeepOneRootState() : base() { }

        public override void Initialize(ServerCharacter ctx)
        {
            base.Initialize(ctx);
            Pursuing = new DeepOnePursuingState(null, this, ctx);
        }

        protected override State GetInitialState() => Pursuing;

        private bool IsWaterAt(int tileX, int tileY)
        {
            if (WorldManager.Instance == null || WorldManager.Instance.FluidGrid == null)
            {
                return false;
            }

            return WorldManager.Instance.FluidGrid.GetFluidType(tileX, tileY) == FluidType.Water;
        }

        public bool IsInWater()
        {
            int tileX = Mathf.FloorToInt(_ctx.transform.position.x);
            int bodyY = Mathf.FloorToInt(_ctx.transform.position.y - 0.5f);

            return IsWaterAt(tileX, bodyY);
        }

        public bool IsFullySubmerged()
        {
            int tileX = Mathf.FloorToInt(_ctx.transform.position.x);
            int headY = Mathf.FloorToInt(_ctx.transform.position.y + 1f);

            return IsInWater() && IsWaterAt(tileX, headY);
        }
    }

    #endregion

    #region Pursuing State

    public class DeepOnePursuingState : State
    {
        private readonly ServerCharacter _ctx;
        private readonly DeepOneRootState _root;

        private ServerCharacter _target;
        private Vector2 _desiredVisualRotation = Vector2.up;
        private const float TurnDegreeThreshold = 90f;

        public DeepOnePursuingState(StateMachine machine, State parent, ServerCharacter ctx) : base(machine, parent)
        {
            _ctx = ctx;
            _root = (DeepOneRootState)parent;
        }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            // Let knockback control movement, but still apply gravity to it.
            if (_ctx.IsKnockedBack)
            {
                _ctx.KnockbackVelocity.y -= _root.GravityForce * fixedDeltaTime;
                _ctx.KnockbackVelocity.y = Mathf.Max(
                    _ctx.KnockbackVelocity.y,
                    _root.TerminalVelocity);
                return;
            }

            // Find closest player and establish direction
            float direction = 0f;
            ServerCharacter target = FindClosestPlayer();
            _target = target;

            if (target != null)
            {
                float horizontalDifference = target.transform.position.x - _ctx.transform.position.x;

                if (Mathf.Abs(horizontalDifference) > 0.05f)
                {
                    direction = Mathf.Sign(horizontalDifference);
                }
            }

            // If fully submerged use the swimming movement
            if (_root.IsFullySubmerged())
            {
                Vector2 swimDirection = Vector2.zero;

                if (target != null)
                {
                    Vector2 targetOffset =
                        (Vector2)target.transform.position - (Vector2)_ctx.transform.position;

                    if (targetOffset.sqrMagnitude > 0.0025f)
                    {
                        swimDirection = targetOffset.normalized;
                    }
                }

                float swimSpeed = _ctx.Stats.MoveSpeed.GetValue();

                _ctx.Velocity = Vector2.Lerp(
                    _ctx.Velocity,
                    swimDirection * swimSpeed,
                    fixedDeltaTime * _ctx.Data.BaseTurnSharpness);

                return;
            }

            // Move and try to jump
            _ctx.Velocity.x = direction * _root.MoveSpeed;

            bool grounded = _ctx.CollisionResult.TouchingBottom;
            bool hitWall = (direction < 0f && _ctx.CollisionResult.TouchingLeft) || (direction > 0f && _ctx.CollisionResult.TouchingRight);
            bool hitCeiling = _ctx.CollisionResult.TouchingTop && _ctx.Velocity.y > 0f;

            bool shouldJumpForWall = target != null && grounded && hitWall;
            bool playerIsAbove = target != null && target.transform.position.y > _ctx.transform.position.y;
            bool shouldJumpFromSurface = target != null && _root.IsInWater() && !_root.IsFullySubmerged() && playerIsAbove && _ctx.Velocity.y <= 0f;

            if (hitCeiling)
            {
                _ctx.Velocity.y = -0.1f;
            }
            else if (shouldJumpForWall || shouldJumpFromSurface)
            {
                _ctx.Velocity.y = _root.JumpSpeed;
            }
            else if (grounded && _ctx.Velocity.y <= 0f)
            {
                _ctx.Velocity.y = -0.1f;
            }
            else
            {
                _ctx.Velocity.y -= _root.GravityForce * fixedDeltaTime;
                _ctx.Velocity.y = Mathf.Max(_ctx.Velocity.y, _root.TerminalVelocity);
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_ctx.VisualsGO == null)
            {
                return;
            }

            // Roate sprite visuals
            _desiredVisualRotation = Vector2.up;

            if (_root.IsInWater() && _target != null)
            {
                Vector2 directionToTarget = (Vector2)_target.transform.position - (Vector2)_ctx.transform.position;

                if (directionToTarget.sqrMagnitude > 0.0025f)
                {
                    _desiredVisualRotation = directionToTarget.normalized;
                }
            }

            float angleDifference = Vector2.SignedAngle(_ctx.VisualsGO.transform.up, _desiredVisualRotation);
            float lerpSpeed = Mathf.Abs(angleDifference) < TurnDegreeThreshold ? 10f : 40f;
            float currentAngle = _ctx.VisualsGO.transform.eulerAngles.z;
            float targetAngle = currentAngle + angleDifference;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, lerpSpeed * deltaTime);

            _ctx.VisualsGO.transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        private ServerCharacter FindClosestPlayer()
        {
            if (EntityManager.Instance == null)
            {
                return null;
            }

            ServerCharacter closestPlayer = null;
            float rangeSquared = _root.DetectionRangeTiles * _root.DetectionRangeTiles;
            float closestDistanceSquared = rangeSquared;

            // Find the closest player by looping through the entities
            var entities = EntityManager.Instance.Entities;

            // NTFS: This might not scale well
            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];

                if (entity == null || entity == _ctx)
                {
                    continue;
                }

                if (entity is not ServerCharacter character || character.gameObject.layer != 3)
                {
                    continue;
                }

                if (character.Health == null || character.Health.CurrentLifeState.Value != LifeState.Alive)
                {
                    continue;
                }

                float distanceSquared = ((Vector2)character.transform.position - (Vector2)_ctx.transform.position).sqrMagnitude;

                if (distanceSquared <= closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestPlayer = character;
                }
            }

            return closestPlayer;
        }
    }

    #endregion

}
