using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New OxygenTank", menuName = "OceanGame/Item/OxygenTankItemSO")]
    public class OxygenTankItemSO : ItemSO
    {
        [field: Header("Oxygen Tank Settings")]
        [field: SerializeField] public int AdditionalOxygen { get; private set; } = 30;
    }
}