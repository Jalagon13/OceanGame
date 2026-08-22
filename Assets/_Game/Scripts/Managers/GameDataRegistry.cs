using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class GameDataRegistry : MonoBehaviour
    {
        public static GameDataRegistry Instance { get; private set; }

        [SerializeField] private List<TileSO> _tileDatabase = new();
        [Space(25)]
        [SerializeField] private List<ItemSO> _itemDatabase = new();

        private Dictionary<TileSO, int> _tileToIdMap = new();
        private Dictionary<ItemSO, int> _itemToIdMap = new();

        private void Awake()
        {
            Instance = this;
            InitializeRegistry();
        }

        private void InitializeRegistry()
        {
            _tileToIdMap.Clear();

            // Loop through the tile list and map each tile asset to its list index (ID)
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

            // Loop through the item list and map each item asset to its list index (ID)
            for (int i = 0; i < _itemDatabase.Count; i++)
            {
                if (_itemDatabase[i] != null)
                {
                    // If a tile is accidentally added twice, log a warning
                    if (_itemToIdMap.ContainsKey(_itemDatabase[i]))
                    {
                        Debug.LogError($"Duplicate item found in registry: {_itemDatabase[i].name} at index {i}");
                        continue;
                    }

                    _itemToIdMap.Add(_itemDatabase[i], i);
                }
                else
                {
                    Debug.LogError($"Null item found in registry at index {i}");
                }
            }
        }

        #region Tile Functions

        public int GetTileId(TileSO tile)
        {
            if (tile == null) 
            {
                Debug.LogError("Attempted to get ID for a null tile.");
                return -3;
            }

            if (_tileToIdMap.TryGetValue(tile, out int id))
            {
                return id;
            }

            Debug.LogError($"Tile '{tile.name}' is not registered in the GameDataRegistry!");
            return -3;
        }

        public TileSO GetTileFromId(int id)
        {
            if (id <= -3 || id >= _tileDatabase.Count) return null;
            return _tileDatabase[id];
        }

        #endregion

        #region Item Functions

        public int GetItemId(ItemSO item)
        {
            if (item == null) return InventorySlot.EMPTY_SLOT_ID; // -1 represents empty inventory slot

            if (_itemToIdMap.TryGetValue(item, out int id)) return id;

            Debug.LogError($"Item '{item.ItemName}' is not registered in the GameDataRegistry!");
            return InventorySlot.EMPTY_SLOT_ID;
        }

        public ItemSO GetItemFromId(int id)
        {
            if (id < 0 || id >= _itemDatabase.Count) return null;
            return _itemDatabase[id];
        }

        #endregion
    }
}
