using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class SurfaceHeightMapStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _landTile;
        
        [Header("Biome")]
        [SerializeField] private float _biomeFrequency = 0.01f;
        
        [Header("Land Settings")]
        [SerializeField] private int _approcLandLevel;
        [SerializeField] private int _landAmplitude = 25; // max height of height map
        [SerializeField] private float _landFrequency = 0.02f; // how far we step along the x axis of the perlin noise

        [Header("Ocean Floor Settings")]
        [SerializeField] private int _approxOceanFloorLevel;
        [SerializeField] private int _amplitude = 25; // max height of height map
        [SerializeField] private float _frequency = 0.02f; // how far we step along the x axis of the perlin noise
    
        public override IEnumerator Execute(WorldGenContext ctx)
        {
            float seedOffset = 0;
        
            for(int x = 0; x < ctx.Width; x++)
            {
                float biomeSampleX = (x * _biomeFrequency) + seedOffset;
                float biomeNoise = Mathf.PerlinNoise1D(biomeSampleX); // 0 is ocean and 1 is land

                float currentAmplitude = Mathf.Lerp(_amplitude, _landAmplitude, biomeNoise);
                float currentFrequency = Mathf.Lerp(_frequency, _landFrequency, biomeNoise);

                float terrainSample = (x * currentFrequency) + seedOffset;
                float rawNoise = Mathf.PerlinNoise1D(terrainSample);
                
                float baseHeight = Mathf.Lerp(_approxOceanFloorLevel, _approcLandLevel, biomeNoise);
                
                float approxHeight = (rawNoise * currentAmplitude) + baseHeight;
                int finalY = Mathf.RoundToInt(approxHeight);
            
                ctx.FgTiles[x, finalY] = new TileData(_landTile.GetId());

                if (x % 25 == 0) yield return null;
            }
            
            yield return null;
        }
    }
}
