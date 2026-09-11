using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public readonly struct CharacterSpawnCircumstance
    {
        public int MaxCharacterCount { get; }
        public float PerSecondSpawnChance { get; }
        public CharacterSpawnPool SpawnPool { get; }

        public CharacterSpawnCircumstance(int maxCharacterCount, float perSecondSpawnChance, CharacterSpawnPool spawnPool)
        {
            MaxCharacterCount = maxCharacterCount;
            PerSecondSpawnChance = perSecondSpawnChance;
            SpawnPool = spawnPool;
        }
    }

    public sealed class CharacterSpawnPool
    {
        private readonly List<CharacterSpawnEntry> _entries = new();

        public CharacterSpawnPool(IEnumerable<CharacterSpawnEntry> entries)
        {
            _entries.AddRange(entries);
        }

        public bool TrySelectCharacter(out CharacterSpawnEntry selectedEntry)
        {
            float totalWeight = 0f;

            foreach (CharacterSpawnEntry entry in _entries)
            {
                if (entry.Weight > 0f)
                    totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f)
            {
                selectedEntry = default;
                Debug.LogError($"Character Spawn Pool [{this}] total weight is 0 or negative so no mob can be found");
                return false;
            }

            float roll = UnityEngine.Random.value * totalWeight;

            foreach (CharacterSpawnEntry entry in _entries)
            {
                if (entry.Weight <= 0f)
                    continue;

                roll -= entry.Weight;

                if (roll <= 0f)
                {
                    selectedEntry = entry;
                    return true;
                }
            }

            selectedEntry = default;
            return false;
        }
    }

    [Serializable]
    public sealed class CharacterSpawnEntry
    {
        [SerializeField] private CharacterSO _character;
        [SerializeField, Min(0f)] private float _weight = 1f;

        public CharacterSO Character => _character;
        public float Weight => _weight;
    }
}