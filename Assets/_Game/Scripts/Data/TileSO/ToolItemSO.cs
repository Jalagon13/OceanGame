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

        public override void OnPrimaryActionStarted()
        {
            MiningManager.Instance.StartMining(this);
        }

        public override void OnPrimaryActionHeld()
        {
            MiningManager.Instance.TickMining(this);
        }

        public override void OnPrimaryActionRelease()
        {
            MiningManager.Instance.StopMining();
        }
    }
}