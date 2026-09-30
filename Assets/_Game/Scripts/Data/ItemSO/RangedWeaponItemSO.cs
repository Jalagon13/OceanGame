using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New Ranged Weapon", menuName = "OceanGame/Item/RangedWeaponItemSO")]
    public class RangedWeaponItemSO : ItemSO
    {
        [field: Header("Ranged Weapon Settings")]
        [field: SerializeField] public float ShotCooldown { get; private set; } = 0.5f;
        [field: SerializeField] public ProjectileSO Projectile { get; private set; }
        [field: SerializeField] public ItemSO Aummunition { get; private set; } // If null, use no ammunition

        public override void OnPrimaryActionStarted(Player player)
        {
            player.RangedHandler.StartPrimaryAction(this);
        }

        public override void OnPrimaryActionHeld(Player player)
        {
            player.RangedHandler.HoldPrimaryAction(this);
        }

        public override void OnPrimaryActionRelease(Player player)
        {
            player.RangedHandler.ReleasePrimaryAction(this);
        }
    }
}