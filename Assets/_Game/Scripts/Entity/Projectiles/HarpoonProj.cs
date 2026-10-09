using UnityEngine;

namespace OceanGame
{
    public class HarpoonProj : Projectile
    {
        [Header("Gravity")]
        public float GravityForce = 20f;
        public float TerminalVelocity = -18f;

        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnFixedUpdateBehavior(float fixedDeltaTime)
        {
            Velocity.y -= GravityForce * fixedDeltaTime;

            if (Velocity.y < TerminalVelocity)
            {
                Velocity.y = TerminalVelocity;
            }

            base.OnFixedUpdateBehavior(fixedDeltaTime);
        }

        protected override void OnTileCollide(GridPhysics.CollisionResult collision)
        {
            base.OnTileCollide(collision);
        }

        protected override void OnHitEntity(Entity target)
        {
            base.OnHitEntity(target);
        }

        protected override void OnKill()
        {
            base.OnKill();
        }
    }
}
