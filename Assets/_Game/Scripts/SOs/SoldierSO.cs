using UnityEngine;

namespace LittleGuyGamePrototype
{
    [CreateAssetMenu(fileName = "New Solider Data", menuName = "LittleGuyGame/SoldierData")]
    public class SoldierSO : ScriptableObject
    {
        [field: Header("Stats")]
        [field: SerializeField] public int MaxHealth { get; private set; } = 8;
        [field: SerializeField] public int BaseDamage { get; private set; } = 1;
        [field: SerializeField] public float MoveSpeed { get; private set; } = 3f;
        [field: SerializeField] public float IFrameDuration { get; private set; } = 0.16f;
        [field: SerializeField] public bool CanKnockback { get; private set; } = true;
        [field: SerializeField] public float KnockbackForce { get; private set; } = 5;
        
        [field: Header("AI")]
        [field: SerializeField] public float MinIdleTime { get; private set; } = 3f;
        [field: SerializeField] public float MaxIdleTime { get; private set; } = 5f;
        [field: SerializeField] public float WanderDestinationSearchRadius { get; private set; } = 3f;
        [field: SerializeField] public float AgroRadius { get; private set; } = 4f;
    }
}
