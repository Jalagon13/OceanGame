using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class CaveGenStep : WorldGenStep
    {
        [Header("Base Cave Shapes")]
        [SerializeField, Range(0f, 1f)] private float _caveNoiseFrequency = 0.03f;
        [SerializeField, Range(0f, 1f)] private float _botThresh = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _topThresh = 0.8f;

        [Header("Detail Octave")]
        [SerializeField, Range(0f, 1f)] private float _detailFrequency = 0.12f; // Usually much higher than base frequency

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int width = ctx.Width;
            int height = ctx.Height;

            float baseSeedX = ctx.Random.Next(-100000, 100000);
            float baseSeedY = ctx.Random.Next(-100000, 100000);
            float detailSeedX = ctx.Random.Next(-100000, 100000);
            float detailSeedY = ctx.Random.Next(-100000, 100000);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float baseSampleX = (x * _caveNoiseFrequency) + baseSeedX;
                    float baseSampleY = (y * _caveNoiseFrequency) + baseSeedY;
                    float baseNoise = Mathf.PerlinNoise(baseSampleX, baseSampleY);

                    float detailSampleX = (x * _detailFrequency) + detailSeedX;
                    float detailSampleY = (y * _detailFrequency) + detailSeedY;
                    float detailNoise = Mathf.PerlinNoise(detailSampleX, detailSampleY);

                    float finalNoise = baseNoise + detailNoise;

                    if (finalNoise >= _botThresh && finalNoise <= _topThresh)
                    {
                        ctx.CaveGrid[x, y] = true;
                    }
                }

                if (x % 50 == 0) yield return null;
            }
        }
    }
}
