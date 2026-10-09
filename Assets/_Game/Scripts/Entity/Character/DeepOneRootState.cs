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
        public float JumpSpeed = 8f;
        
        [Header("Gravity")]
        public float GravityForce = 20f;
        public float TerminalVelocity = -18f;

        public DeepOneLandPursuingState LandPursuing { get; private set; }
        public DeepOneWaterPursuingState WaterPursuing { get; private set; }

        public DeepOneRootState() : base() { }

        public override void Initialize(ServerCharacter ctx)
        {
            base.Initialize(ctx);

            LandPursuing = new DeepOneLandPursuingState(null, this, ctx);
            WaterPursuing = new DeepOneWaterPursuingState(null, this, ctx);
        }

        protected override State GetInitialState()
        {
            return IsFullySubmerged() ? WaterPursuing : LandPursuing;
        }

        protected override State GetTransition()
        {
            State desiredState = IsFullySubmerged() ? WaterPursuing : LandPursuing;
            return ActiveChild == desiredState ? null : desiredState;
        }

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

    #region Abstract Pursuing State

    public abstract class DeepOnePursuingState : State
    {
        protected readonly ServerCharacter Ctx;
        protected readonly DeepOneRootState Root;

        private ServerCharacter _target;
        private const float TurnDegreeThreshold = 90f;

        protected DeepOnePursuingState(State parent, ServerCharacter ctx) : base(null, parent)
        {
            Ctx = ctx;
            Root = (DeepOneRootState)parent;
        }

        protected ServerCharacter FindClosestPlayer()
        {
            if (EntityManager.Instance == null)
            {
                return null;
            }

            ServerCharacter closestPlayer = null;
            float rangeSquared = Root.DetectionRangeTiles * Root.DetectionRangeTiles;
            float closestDistanceSquared = rangeSquared;
            var entities = EntityManager.Instance.Entities;

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];

                if (entity == null || entity == Ctx)
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

                float distanceSquared =
                    ((Vector2)character.transform.position - (Vector2)Ctx.transform.position).sqrMagnitude;

                if (distanceSquared <= closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestPlayer = character;
                }
            }

            return closestPlayer;
        }

        protected void SetTarget(ServerCharacter target)
        {
            _target = target;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Ctx.VisualsGO == null)
            {
                return;
            }

            Vector2 desiredVisualRotation = Vector2.up;

            if (Root.IsInWater() && _target != null)
            {
                Vector2 directionToTarget = (Vector2)_target.transform.position - (Vector2)Ctx.transform.position;

                if (directionToTarget.sqrMagnitude > 0.0025f)
                {
                    desiredVisualRotation = directionToTarget.normalized;
                }
            }

            float angleDifference = Vector2.SignedAngle(Ctx.VisualsGO.transform.up, desiredVisualRotation);
            float lerpSpeed = Mathf.Abs(angleDifference) < TurnDegreeThreshold ? 10f : 40f;
            float currentAngle = Ctx.VisualsGO.transform.eulerAngles.z;
            float targetAngle = currentAngle + angleDifference;
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, lerpSpeed * deltaTime);

            Ctx.VisualsGO.transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }
    }

    #endregion

    #region Land Pursuing State

    public class DeepOneLandPursuingState : DeepOnePursuingState
    {
        public DeepOneLandPursuingState(StateMachine machine, State parent, ServerCharacter ctx) : base(parent, ctx) { }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            if (Ctx.IsKnockedBack)
            {
                // Like the player's grounded state, don't keep adding gravity after landing.
                if (!Root.IsInWater() && !Ctx.CollisionResult.TouchingBottom)
                {
                    Ctx.KnockbackVelocity.y -= Root.GravityForce * fixedDeltaTime;
                    Ctx.KnockbackVelocity.y = Mathf.Max(Ctx.KnockbackVelocity.y, Root.TerminalVelocity);
                }

                return;
            }

            ServerCharacter target = FindClosestPlayer();
            SetTarget(target);

            float direction = 0f;

            if (target != null)
            {
                float horizontalDifference = target.transform.position.x - Ctx.transform.position.x;

                if (Mathf.Abs(horizontalDifference) > 0.05f)
                {
                    direction = Mathf.Sign(horizontalDifference);
                }
            }

            Ctx.Velocity.x = Mathf.Lerp(Ctx.Velocity.x, direction * Ctx.Data.BaseSpeed, fixedDeltaTime * Ctx.Data.BaseTurnSharpness);

            bool grounded = Ctx.CollisionResult.TouchingBottom;
            bool hitWall = (direction < 0f && Ctx.CollisionResult.TouchingLeft) || (direction > 0f && Ctx.CollisionResult.TouchingRight);
            bool hitCeiling = Ctx.CollisionResult.TouchingTop && Ctx.Velocity.y > 0f;
            bool shouldJumpForWall = target != null && grounded && hitWall;
            bool playerIsAbove = target != null && target.transform.position.y > Ctx.transform.position.y;
            bool shouldJumpFromSurface = target != null && Root.IsInWater() && !Root.IsFullySubmerged() && playerIsAbove && Ctx.Velocity.y <= 0f;

            if (hitCeiling)
            {
                Ctx.Velocity.y = -0.1f;
            }
            else if (shouldJumpForWall || shouldJumpFromSurface)
            {
                Ctx.Velocity.y = Root.JumpSpeed;
            }
            else if (grounded && Ctx.Velocity.y <= 0f)
            {
                Ctx.Velocity.y = -0.1f;
            }
            else
            {
                Ctx.Velocity.y -= Root.GravityForce * fixedDeltaTime;
                Ctx.Velocity.y = Mathf.Max(Ctx.Velocity.y, Root.TerminalVelocity);
            }
        }
    }

    #endregion

    #region Land Pursuing State

    public class DeepOneWaterPursuingState : DeepOnePursuingState
    {
        public DeepOneWaterPursuingState(StateMachine machine, State parent, ServerCharacter ctx) : base(parent, ctx) { }

        protected override void OnFixedUpdate(float fixedDeltaTime)
        {
            if (Ctx.IsKnockedBack)
            {
                // The shared character tick decays knockback; water movement adds no gravity.
                return;
            }

            ServerCharacter target = FindClosestPlayer();
            SetTarget(target);

            Vector2 swimDirection = Vector2.zero;

            if (target != null)
            {
                Vector2 targetOffset = (Vector2)target.transform.position - (Vector2)Ctx.transform.position;

                if (targetOffset.sqrMagnitude > 0.0025f)
                {
                    swimDirection = targetOffset.normalized;
                }
            }

            float swimSpeed = Ctx.Stats.MoveSpeed.GetValue();

            Ctx.Velocity = Vector2.Lerp(Ctx.Velocity, swimDirection * swimSpeed, fixedDeltaTime * Ctx.Data.BaseTurnSharpness);
        }
    }

    #endregion
}
