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
        [field: SerializeField] public ItemSO DropItem { get; private set; }
    
        public int GetDrops() // Gets amount of item to spawn
        {
            return 1;
        }
    }
}
