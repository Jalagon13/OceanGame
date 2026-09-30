using UnityEngine;

namespace OceanGame
{
    public class Harpoon : Projectile
    {
        protected override void OnInit()
        {
            base.OnInit();
        }

        protected override void OnUpdateBehavior(float fixedDeltaTime)
        {
            base.OnUpdateBehavior(fixedDeltaTime);
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
