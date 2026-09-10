using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class CharacterSpawner
    {
        private RectInt _spawnArea; // Area around player characters can spawn
        public RectInt SpawnArea => _spawnArea;

        private RectInt _noSpawnArea; // Area around the player where mobs cannots spawn (so they dont just spawn within the view of the player)
        public RectInt NoSpawnArea => _noSpawnArea;

        private RectInt _activeArea; // any characters out any active areas will instantly despawn
        public RectInt ActiveArea => _activeArea;

        private RectInt _timerSafeArea; // Any characters outside this area will tick a despawn timer
        public RectInt TimerSafeArea => _timerSafeArea;

        private readonly ServerCharacter _host;
        public ServerCharacter Host => _host;

        private readonly HashSet<ServerCharacter> _ownedCharacters = new();
        public int CurrentCharacterCount => _ownedCharacters.Count;

        public CharacterSpawnCircumstance CurrentCircumstance { get; private set; }

        private static readonly Vector2Int _spawnAreaDimensions = new(84, 46);
        private static readonly Vector2Int _noSpawnAreaDimensions = new(62, 35);
        private static readonly Vector2Int _activeAreaDimensions = new(252, 142);
        private static readonly Vector2Int _timerSafeAreaDimensions = new(60, 34);

        public CharacterSpawner(ServerCharacter host)
        {
            _host = host;
        }

        public void TickSpawner()
        {
            UpdateAreas();

            CurrentCircumstance = GetSpawnCircumstances();

            float newPerSecondSpawnChance = CalculatePerSecondSpawnChance();
            float tickChance = ConvertSpawnChancePerSecondToPerTick(newPerSecondSpawnChance);

            if (UnityEngine.Random.value >= tickChance)
                return;

            TrySpawnCharacter();
        }

        private void TrySpawnCharacter()
        {
            if (!CharacterManager.Instance.CanSpawnCharacter())
                return;

            Debug.Log($"Trying to spawn character");
            
            
        }

        private float CalculatePerSecondSpawnChance()
        {
            float perSecondChance = CurrentCircumstance.PerSecondSpawnChance;
            float populationRatio = CurrentCircumstance.MaxCharacterCount > 0 ? (float)CurrentCharacterCount / CurrentCircumstance.MaxCharacterCount : 1f;
            float chanceMultiplier;

            // Increase the odds of spawning when this spawner's owned population is low.
            if (populationRatio < 0.2f)
                chanceMultiplier = 1.8f;
            else if (populationRatio < 0.4f)
                chanceMultiplier = 1.6f;
            else if (populationRatio < 0.6f)
                chanceMultiplier = 1.4f;
            else if (populationRatio < 0.8f)
                chanceMultiplier = 1.2f;
            else
                chanceMultiplier = 1f;

            return Mathf.Clamp01(perSecondChance * chanceMultiplier);
        }

        public void RegisterOwnedCharacter(ServerCharacter character)
        {
            if (character != null)
            {
                _ownedCharacters.Add(character);
            }
        }

        public void UnregisterOwnedCharacter(ServerCharacter character)
        {
            _ownedCharacters.Remove(character);
        }

        private CharacterSpawnCircumstance GetSpawnCircumstances()
        {
            // Quieries something and gets a characterspawncircumstance from a different place maybe WorldManager? idk TODO for now

            return new CharacterSpawnCircumstance(6, 0.1f);
        }

        private float ConvertSpawnChancePerSecondToPerTick(float perSecondChance)
        {
            float spawnChancePerSecond = Mathf.Clamp01(perSecondChance);

            // Convert per second chance to per tick chance
            float perTickChance = 1f - Mathf.Pow(1f - spawnChancePerSecond, Time.fixedDeltaTime);

            return perTickChance;
        }

        private void UpdateAreas()
        {
            Vector3 hostPos = _host.transform.position;
            Vector2Int hostPosInt = new((int)hostPos.x, (int)hostPos.y);

            _spawnArea = CreateRectCenteredAt(hostPosInt, _spawnAreaDimensions);
            _noSpawnArea = CreateRectCenteredAt(hostPosInt, _noSpawnAreaDimensions);
            _activeArea = CreateRectCenteredAt(hostPosInt, _activeAreaDimensions);
            _timerSafeArea = CreateRectCenteredAt(hostPosInt, _timerSafeAreaDimensions);
        }

        private RectInt CreateRectCenteredAt(Vector2Int center, Vector2Int size)
        {
            Vector2Int position = center - (size / 2);
            return new RectInt(position, size);
        }
    }

    public readonly struct CharacterSpawnCircumstance
    {
        public int MaxCharacterCount { get; }
        public float PerSecondSpawnChance { get; }

        public CharacterSpawnCircumstance(int maxCharacterCount, float perSecondSpawnChance)
        {
            MaxCharacterCount = maxCharacterCount;
            PerSecondSpawnChance = perSecondSpawnChance;
        }
    }
}