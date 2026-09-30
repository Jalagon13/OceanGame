using UnityEngine;

namespace OceanGame
{
    public class PlayerRangedHandler : MonoBehaviour
    {
        private float _shotCooldownTimer;

        private void Update()
        {
            // Tick down the cooldown every frame
            if (_shotCooldownTimer > 0f)
            {
                _shotCooldownTimer -= Time.deltaTime;
            }
        }

        public void StartPrimaryAction(RangedWeaponItemSO weapon)
        {
            // Fire immediately on first press
            TryShoot(weapon);
        }

        public void HoldPrimaryAction(RangedWeaponItemSO weapon)
        {
            // Fire repeatedly while held, respecting cooldown
            TryShoot(weapon);
        }

        public void ReleasePrimaryAction(RangedWeaponItemSO weapon)
        {
            // Nothing needed on release for a basic ranged weapon
        }

        private void TryShoot(RangedWeaponItemSO weapon)
        {
            if (_shotCooldownTimer > 0f) return;
            if (weapon.Projectile == null) return;
            if (weapon.Aummunition != null && InventoryManager.Instance.GetTotalItemCount(weapon.Aummunition) <= 0) return;

            // Aim direction from player toward the mouse
            Vector2 spawnPos = transform.position;
            Vector2 aimDir = (WorldManager.MouseWorldPosition - spawnPos).normalized;
            Vector2 velocity = aimDir * weapon.Projectile.BaseSpeed;

            ProjectileManager.SpawnProjectile(
                projectileSO: weapon.Projectile,
                position: spawnPos,
                initialVelocity: velocity,
                sourceEntity: Player.Instance.Character
            );

            // Start the cooldown
            _shotCooldownTimer = weapon.ShotCooldown;
            
            // Take away ammunition if needed
            if(weapon.Aummunition != null)
            {
                InventoryManager.Instance.RemoveItem(weapon.Aummunition, 1);
            }
        }
    }
}