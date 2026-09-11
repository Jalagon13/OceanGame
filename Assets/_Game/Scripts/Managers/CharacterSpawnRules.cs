using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class CharacterSpawnRules : MonoBehaviour
    {
        public static CharacterSpawnRules Instance { get; private set; }
        
        [SerializeField] private int _tempMaxCharCount = 6;
        [SerializeField] private float _tempPerSecSpawnChance = 0.1f;
        [SerializeField] private List<CharacterSpawnEntry> _tempSpawnEntries = new();
        
        private void Awake() 
        {
            Instance = this;
        }
        
        public CharacterSpawnCircumstance GetCircumstances(ServerCharacter host)
        {
            // Quiery various managers for time of day, biome, current events, game states, player buffs, game progression, etc...
            
            int maxCharacterCount = _tempMaxCharCount;
            float perSecondSpawnChance = _tempPerSecSpawnChance;
            CharacterSpawnPool spawnPool = BuildSpawnPool();
            
            return new CharacterSpawnCircumstance(maxCharacterCount, perSecondSpawnChance, spawnPool);
        }

        private CharacterSpawnPool BuildSpawnPool()
        {
            return new CharacterSpawnPool(_tempSpawnEntries);
        }
    }
}