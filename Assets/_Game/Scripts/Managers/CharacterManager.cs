using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [SerializeField] private bool _enableSpawning = true;
        
        [SerializeField] private int _globalMaxCharacterCap = 200;
        public int GlobalMaxCharCap => _globalMaxCharacterCap;
        
        [SerializeField] private float _durationTillCharacterDespawns = 20f;
        
        private List<CharacterSpawner> _spawners = new();
        private readonly Dictionary<ServerCharacter, CharacterLifetime> _characterDespawnTimers = new();
        private class CharacterLifetime
        {
            public CharacterSpawner Owner;
            public float RemainingTime;
            
            public CharacterLifetime(CharacterSpawner owner, float remainingTime)
            {
                Owner = owner;
                RemainingTime = remainingTime;
            }
        }

        public int CurrentCharacterCount
        {
            get
            {
                int count = 0;

                foreach (CharacterSpawner spawner in _spawners)
                {
                    count += spawner.LocalCurrentCharCount;
                }

                return count;
            }
        }

        private void Awake() 
        {
            Instance = this;
        }
        
        private void Start() 
        {
            RegisterSpawner(Player.Instance.Character);    
        }

        private void OnDrawGizmos()
        {
            if (_spawners == null || _spawners.Count == 0) return;

            for (int i = 0; i < _spawners.Count; i++)
            {
                CharacterSpawner spawner = _spawners[i];
                if (spawner == null) continue;

                Gizmos.color = Color.green;
                DrawRectIntGizmo(spawner.SpawnArea);

                Gizmos.color = Color.red;
                DrawRectIntGizmo(spawner.NoSpawnArea);

                Gizmos.color = Color.black;
                DrawRectIntGizmo(spawner.ActiveArea);

                Gizmos.color = Color.white;
                DrawRectIntGizmo(spawner.TimerSafeArea);
            }
        }

        // Ticks after everything in the entitymanager so it uses up to date positions
        public void TickCharacters() 
        {
            if(!WorldManager.Instance.IsWorldReady) return;
            
            if(_spawners.Count > 0)
            {
                for (int i = _spawners.Count - 1; i >= 0; i--)
                {
                    _spawners[i].TickSpawner();
                }
            }
            
            TickCharacterLifetimes();
        }

        private void TickCharacterLifetimes()
        {
            Dictionary<ServerCharacter, CharacterSpawner> charactersToDespawn = new();

            // Loop through all spawned characters
            foreach (KeyValuePair<ServerCharacter, CharacterLifetime> entry in _characterDespawnTimers)
            {
                ServerCharacter character = entry.Key;

                // Check if null for any reason
                if (character == null)
                {
                    charactersToDespawn.Add(character, entry.Value.Owner);
                    continue;
                }
                
                // Check if dead
                if(character.Health.CurrentLifeState.Value == LifeState.Dead)
                {
                    charactersToDespawn.Add(character, entry.Value.Owner);
                    continue;
                }

                Vector2Int position = new(Mathf.FloorToInt(character.transform.position.x), Mathf.FloorToInt(character.transform.position.y));

                bool isInActiveArea = false;
                bool isInTimerSafeArea = false;

                foreach (CharacterSpawner spawner in _spawners)
                {
                    if (spawner.ActiveArea.Contains(position))
                        isInActiveArea = true;

                    if (spawner.TimerSafeArea.Contains(position))
                        isInTimerSafeArea = true;

                    if (isInActiveArea && isInTimerSafeArea)
                        break;
                }

                if (!isInActiveArea)
                {
                    charactersToDespawn.Add(character, entry.Value.Owner);
                    continue;
                }

                if (isInTimerSafeArea)
                {
                    _characterDespawnTimers[character].RemainingTime = _durationTillCharacterDespawns;
                    continue;
                }

                // Decrement it and check if should despawn
                float remainingTime = entry.Value.RemainingTime - Time.fixedDeltaTime;

                if (remainingTime <= 0f)
                {
                    charactersToDespawn.Add(character, entry.Value.Owner);
                }
                else
                {
                    _characterDespawnTimers[character].RemainingTime = remainingTime;
                }
            }

            foreach (KeyValuePair<ServerCharacter, CharacterSpawner> entry in charactersToDespawn)
            {
                ServerCharacter character = entry.Key;
                CharacterSpawner owner = entry.Value;

                _characterDespawnTimers.Remove(character);
                owner.UnregisterOwnedCharacter(character);

                if (character != null)
                {
                    Debug.Log($"{character.name} despawned.");
                    Debug.Log($"Owner character count: {owner.LocalCurrentCharCount}/{owner.CurrentCircumstance.MaxCharacterCount}");
                    Destroy(character.gameObject);
                }
            }
        }

        public bool TrySpawnCharacter(CharacterSpawner owner, ServerCharacter prefab, Vector2 spawnPosition, out ServerCharacter spawnedCharacter)
        {
            spawnedCharacter = null;
            
            if(!CanSpawnCharacter())
                return false;

            spawnedCharacter = Instantiate(prefab, spawnPosition, Quaternion.identity);

            CharacterLifetime lifetime = new(owner, _durationTillCharacterDespawns);
            _characterDespawnTimers.Add(spawnedCharacter, lifetime);
            Debug.Log($"Spawned {spawnedCharacter.name} at {spawnPosition}!");
            return true;
        }

        // Register and unregister spawners when players enter and leave the world
        public void RegisterSpawner(ServerCharacter host)
        {
            for (int i = 0; i < _spawners.Count; i++)
            {
                if (_spawners[i].Host == host)
                {
                    return; // Already registered, exit early
                }
            }

            CharacterSpawner spawner = new(host);
            _spawners.Add(spawner);
        }
        
        public void UnRegisterSpawner(ServerCharacter host)
        {
            for (int i = 0; i < _spawners.Count; i++)
            {
                if (_spawners[i].Host == host)
                {
                    _spawners.RemoveAt(i);
                    return; // Found and removed, exit early
                }
            }
        }

        public bool CanSpawnCharacter()
        { 
            return _enableSpawning && CurrentCharacterCount < _globalMaxCharacterCap;
        }

        private void DrawRectIntGizmo(RectInt rect)
        {
            Vector3 center = new(rect.center.x, rect.center.y, 0f);
            Vector3 size = new(rect.size.x, rect.size.y, 0f);

            Gizmos.DrawWireCube(center, size);
        }
    }
}
