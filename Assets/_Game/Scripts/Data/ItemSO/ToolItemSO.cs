using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New ToolItemSO", menuName = "OceanGame/Item/ToolItemSO")]
    public class ToolItemSO : ItemSO
    {
        [field: Header("Tool Settings")]
        [field: SerializeField] public int MiningDamage { get; private set; } = 15;
        [field: SerializeField] public float MineTicksPerSecond { get; private set; } = 4;
        [field: SerializeField] public WorldManager.LayerType TargetLayer { get; private set; } = WorldManager.LayerType.Foreground;

        [Header("Melee & Hitbox Settings")]
        [SerializeField] public int MeleeDamage = 10;
        [SerializeField] public int KnockbackForce = 15;
        [SerializeField] public float HitboxOffset = 1.0f; // Distance from pivot to blade center
        [SerializeField] public Vector2 HitboxSize = new Vector2(1.2f, 1.2f);

        [field: Header("Swing Settings")]
        [field: SerializeField] public int SwingArcAngle { get; private set; } = 120;
        [field: SerializeField] public float SwingDuration { get; private set; } = 0.5f;
        [field: SerializeField] public int SwingEndArcAngle { get; private set; } = 10;
        [field: SerializeField] public float SwingEndDuration { get; private set; } = 0.25f;

        public override void OnPrimaryActionStarted(Player player)
        {
            player.ArmHandler.StartPrimaryAction(this);
            MiningManager.Instance.StartMining(this);
        }
        public override void OnPrimaryActionHeld(Player player)
        {
            player.ArmHandler.HoldPrimaryAction(this);
            MiningManager.Instance.TickMining(this);
        }
        public override void OnPrimaryActionRelease(Player player)
        {
            player.ArmHandler.ReleasePrimaryAction(this);
            MiningManager.Instance.StopMining();
        }
    }
}