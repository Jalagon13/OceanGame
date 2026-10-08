using System;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public struct LootDropEntry
    {
        [Tooltip("The item ScriptableObject to drop.")]
        [SerializeField] private ItemSO _item;

        [Tooltip("Minimum amount dropped when the roll succeeds.")]
        [SerializeField] private int _minAmount;

        [Tooltip("Maximum amount dropped when the roll succeeds.")]
        [SerializeField] private int _maxAmount;

        [Tooltip("Drop probability: 1 = 100%, 0.5 = 50%, 0.05 = 5%, etc.")]
        [SerializeField, Range(0f, 1f)] private float _dropChance;

        public readonly ItemSO Item => _item;
        public readonly float DropChance => _dropChance;
        public readonly int MinAmount => _minAmount;
        public readonly int MaxAmount => _maxAmount;

        public void TryDrop(Vector2 position)
        {
            if (_item == null || _dropChance <= 0f) return;

            // Roll against the drop chance (Random.value returns a float between 0.0 and 1.0)
            if (UnityEngine.Random.value <= _dropChance)
            {
                int min = Mathf.Max(1, _minAmount);
                int max = Mathf.Max(min, _maxAmount);
                int amountToDrop = UnityEngine.Random.Range(min, max + 1);

                GameManager.Instance.SpawnItem(_item, amountToDrop, position);
            }
        }
    }
}