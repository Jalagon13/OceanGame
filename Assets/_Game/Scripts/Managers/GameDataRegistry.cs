using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class GameDataRegistry : MonoBehaviour
    {
        public static GameDataRegistry Instance { get; private set; }

        [SerializeField] private List<TileBase> _tileDatabase = new();

        private Dictionary<TileBase, int> _tileToIdMap = new();

        private void Awake()
        {
            Instance = this;
            InitializeRegistry();
        }

        private void InitializeRegistry()
        {
            _tileToIdMap.Clear();

            // Loop through your inspector list and map each asset to its list index (ID)
            for (int i = 0; i < _tileDatabase.Count; i++)
            {
                if (_tileDatabase[i] != null)
                {
                    // If a tile is accidentally added twice, log a warning
                    if (_tileToIdMap.ContainsKey(_tileDatabase[i]))
                    {
                        Debug.LogError($"Duplicate tile found in registry: {_tileDatabase[i].name} at index {i}");
                        continue;
                    }

                    _tileToIdMap.Add(_tileDatabase[i], i);
                }
                else
                {
                    Debug.LogError($"Null tile found in registry at index {i}");
                }
            }
        }

        #region Tile Functions

        public int GetTileId(TileBase tile)
        {
            if (tile == null) return 0;

            if (_tileToIdMap.TryGetValue(tile, out int id))
            {
                return id;
            }

            Debug.LogError($"Tile '{tile.name}' is not registered in the GameDataRegistry!");
            return 0;
        }

        public TileBase GetTileFromId(int id)
        {
            if (id <= 0 || id >= _tileDatabase.Count) return null;
            return _tileDatabase[id];
        }
        
        #endregion
    }
}
