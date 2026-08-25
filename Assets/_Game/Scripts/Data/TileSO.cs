using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TileSO", menuName = "OceanGame/TileSO")]
    public class TileSO : RuleTile
    {
        [field: Header("Tile Settings")]
        [field: SerializeField] public TileItemSO TileItemSO { get; private set; }
        [field: SerializeField] public Vector2Int Size { get; private set; } = new(1, 1);
        
        public ushort GetId()
        {
            return GameDataRegistry.Instance.GetTileIdFromTileSO(this);
        }
    }
}
