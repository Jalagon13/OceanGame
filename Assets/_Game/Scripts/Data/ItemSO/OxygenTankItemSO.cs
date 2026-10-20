using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New OxygenTank", menuName = "OceanGame/Item/OxygenTankItemSO")]
    public class OxygenTankItemSO : ItemSO
    {
        [field: Header("Oxygen Tank Settings")]
        [field: SerializeField] public int OxygenDuration { get; private set; } = 30;
        [field: SerializeField] public int AdditionalHealth { get; private set; } = 20;
    }
}