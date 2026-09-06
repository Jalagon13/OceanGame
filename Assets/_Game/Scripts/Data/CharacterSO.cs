using UnityEngine;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New Character SO", menuName = "OceanGame/CharacterSO")]
    public class CharacterSO : ScriptableObject
    {
        [Header("Core Stats")]
        [field: SerializeField] public float BaseSpeed { get; private set; }
        [field: SerializeField] public int BaseMaxHealth { get; private set; }
        [field: SerializeField] public int BaseDefense { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float BaseKbResist { get; private set; }
        [field: SerializeField] public float BaseIFrameDuration { get; private set; }
        [field: SerializeField] public float BaseTurnSharpness { get; private set; }
        [field: SerializeField] public bool CanDie { get; private set; } = true;
        [field: SerializeField] public bool CanBeKnockedBacked { get; private set; } = true;
        [field: SerializeField] public Vector2 BodyColliderSize { get; private set; } = Vector2.one;
    }
}
