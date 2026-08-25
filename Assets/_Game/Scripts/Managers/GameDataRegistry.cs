using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    public class GameDataRegistry : MonoBehaviour
    {
        public static GameDataRegistry Instance { get; private set; }

        [SerializeField] private List<TileSO> _tileDatabase = new();
        [SerializeField] private List<ItemSO> _itemDatabase = new();

        private Dictionary<TileSO, ushort> _tileToIdMap = new();
        private Dictionary<ItemSO, ushort> _itemToIdMap = new();

        private void Awake()
        {
            Instance = this;
            InitializeRegistry();
        }

        private void InitializeRegistry()
        {
            _tileToIdMap.Clear();
            for (int i = 0; i < _tileDatabase.Count; i++)
            {
                if (_tileDatabase[i] != null)
                {
                    // +1 Offset: Index 0 gets ID 1, Index 1 gets ID 2, etc.
                    ushort id = (ushort)(i + 1);
                    _tileToIdMap.Add(_tileDatabase[i], id);
                }
            }

            _itemToIdMap.Clear();
            for (int i = 0; i < _itemDatabase.Count; i++)
            {
                if (_itemDatabase[i] != null)
                {
                    // +1 Offset: Index 0 gets ID 1, Index 1 gets ID 2, etc.
                    ushort id = (ushort)(i + 1);
                    _itemToIdMap.Add(_itemDatabase[i], id);
                }
            }
        }

        #region Tile Functions

        public ushort GetTileIdFromTileSO(TileSO tile)
        {
            if (tile == null) return TileData.AIR_ID; // Returns 0

            if (_tileToIdMap.TryGetValue(tile, out ushort id))
            {
                return id;
            }

            Debug.LogError($"Tile '{tile.name}' is not registered in GameDataRegistry!");
            return TileData.AIR_ID;
        }

        public TileSO GetTileSOFromTileId(ushort id)
        {
            // ID 0 (AIR) or OUT_OF_BOUNDS returns null
            if (id == TileData.AIR_ID || id >= TileData.OUT_OF_BOUNDS_ID) return null;

            int databaseIndex = id - 1; // Convert 1-based ID back to 0-based List Index

            if (databaseIndex < 0 || databaseIndex >= _tileDatabase.Count) return null;

            return _tileDatabase[databaseIndex];
        }

        #endregion

        #region Item Functions

        public ushort GetItemIdFromItemSO(ItemSO item)
        {
            if (item == null) return 0; // Returns 0 for empty slot

            if (_itemToIdMap.TryGetValue(item, out ushort id))
            {
                return id;
            }

            Debug.LogError($"Item '{item.ItemName}' is not registered in GameDataRegistry!");
            return 0;
        }

        public ItemSO GetItemSOFromItemId(ushort id)
        {
            if (id == 0) return null; // ID 0 represents an empty slot

            int databaseIndex = id - 1; // Convert 1-based ID back to 0-based List Index

            if (databaseIndex < 0 || databaseIndex >= _itemDatabase.Count) return null;

            return _itemDatabase[databaseIndex];
        }

        #endregion
    }
}