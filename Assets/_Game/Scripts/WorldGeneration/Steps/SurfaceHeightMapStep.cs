using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class SurfaceHeightMapStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _landTile;
        [SerializeField] private int _baseHeight;
        
        [Header("Main Terrain Shape")]
        [SerializeField] private float _largeShapeFrequency = 0.03f;
        [SerializeField] private float _largeShapeAmplitude = 6;

        [Header("Main Terrain Detail")]
        [SerializeField] private float _detailFrequency = 0.03f;
        [SerializeField] private float _detailAmplitude = 6;


        public override IEnumerator Execute(WorldGenContext ctx)
        {
            float seedLarge = ctx.Random.Next(0, 100000);
            float seedDetail = ctx.Random.Next(0, 100000);
        
            for(int x = 0; x < ctx.Width; x++)
            {
                float largeSample = (x * _largeShapeFrequency) + seedLarge;
                largeSample = Mathf.PerlinNoise(largeSample, 0);
                float largeHeight = largeSample * _largeShapeAmplitude;
                
                float detailSample = (x * _detailFrequency) + seedDetail;
                detailSample = Mathf.PerlinNoise(detailSample, 0);
                float detailHeight = detailSample * _detailAmplitude;
                
                float combinedHeight = _baseHeight + largeHeight + detailHeight;
                
                ctx.SurfaceYValues[x] = Mathf.Clamp(Mathf.RoundToInt(combinedHeight), 0, ctx.Height);

                if (x % 32 == 0) yield return null;
            }
        }
    }
}
