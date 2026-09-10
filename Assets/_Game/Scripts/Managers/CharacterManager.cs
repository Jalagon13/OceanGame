using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        [SerializeField] private bool _enableSpawning = true;
        [SerializeField] private int _globalMaxCharacterCap = 200;
        [SerializeField] private float _durationTillCharacterDespawns = 20f;
        
        private List<CharacterSpawner> _spawners = new();

        public int CurrentCharacterCount
        {
            get
            {
                int count = 0;

                foreach (CharacterSpawner spawner in _spawners)
                {
                    count += spawner.CurrentCharacterCount;
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

                Gizmos.color = Color.orange;
                DrawRectIntGizmo(spawner.TimerSafeArea);
            }
        }

        // We will just used the fixed update for a consistent tick rate
        private void FixedUpdate() 
        {
            if(!WorldManager.Instance.IsWorldReady) return;
            
            if(_spawners.Count > 0)
            {
                for (int i = _spawners.Count - 1; i >= 0; i--)
                {
                    _spawners[i].TickSpawner();
                }
            }
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
