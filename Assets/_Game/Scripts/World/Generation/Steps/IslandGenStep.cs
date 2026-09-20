using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class IslandGenStep : WorldGenStep
    {
        [Header("Island Center & Size")]
        [Tooltip("If true, places test island at ctx.Width / 2. If false, uses _customCenterColumn.")]
        [SerializeField] private int _plateauHalfWidth = 15; // 30 tiles wide total
        [SerializeField] private int _slopeWidth = 20; // 20 tiles transition down to sea floor

        [Header("Elevation & Terrain Detail")]
        [Tooltip("Base height above ctx.SeaLevel")]
        [SerializeField] private int _islandBaseHeight = 6;

        [Tooltip("Detail noise frequency along the island top")]
        [SerializeField] private float _plateauDetailFrequency = 0.08f;

        [Tooltip("Detail noise height amplitude")]
        [SerializeField] private float _plateauDetailAmplitude = 4f;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int islandCenter = ctx.Width / 2; // Set it to world center in this example
            
            
            
            
        }
    }
}

