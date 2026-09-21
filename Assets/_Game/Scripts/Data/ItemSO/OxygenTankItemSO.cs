using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New OxygenTank", menuName = "OceanGame/Item/OxygenTankItemSO")]
    public class OxygenTankItemSO : ItemSO
    {
        [field: SerializeField] public int AdditionalHpBuff { get; private set; } = 20;
        [field: SerializeField] public float BuffDuration { get; private set; } = 300;
        [field: SerializeField] public float OxygenRecoveryDuration { get; private set; } = 2;
    }
}
