using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class BaseTerrainGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandSO;
        [SerializeField, Min(0)] private int _smoothPasses = 3;
        [SerializeField, Min(0.001f)] private float _frequency = 20f;
        [SerializeField, Min(0.001f)] private float _detailFrequency = 0.01f;
        [SerializeField, Min(1)] private int _seaLevelFadeHeight = 20;
        [SerializeField, Range(0f, 1f)] private float _solidThreshold = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _airThreshold = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _baseNoiseInfluencePercentage = 0.9f;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            bool[,] terrain = new bool[ctx.Width, ctx.Height];
            int noiseOffsetX = ctx.Random.Next(0, 100000);
            int noiseOffsetY = ctx.Random.Next(0, 100000);
            float detailSeedX = ctx.Random.Next(-100000, 100000);
            float detailSeedY = ctx.Random.Next(-100000, 100000);

            for (int x = 0; x < ctx.Width; x++)
            {
                for (int y = 0; y < ctx.Height; y++)
                {
                    float xPos = (x * _frequency) + noiseOffsetX;
                    float yPos = (y * _frequency) + noiseOffsetY;
                    float baseNoise = Mathf.PerlinNoise(xPos, yPos);

                    float detailSampleX = (x * _detailFrequency) + detailSeedX;
                    float detailSampleY = (y * _detailFrequency) + detailSeedY;
                    float detailNoise = Mathf.PerlinNoise(detailSampleX, detailSampleY);

                    float finalNoise = (baseNoise * _baseNoiseInfluencePercentage) + (detailNoise * (1f - _baseNoiseInfluencePercentage));

                    if (y > ctx.SeaLevel)
                    {
                        float heightAboveSeaLevel = y - ctx.SeaLevel;
                        float fade = 1f - heightAboveSeaLevel / _seaLevelFadeHeight;
                        finalNoise *= Mathf.Clamp01(fade);
                    }

                    if (finalNoise > _solidThreshold)
                    {
                        terrain[x, y] = true;
                    }
                    else if (finalNoise < _airThreshold)
                    {
                        terrain[x, y] = false;
                    }
                    else
                    {
                        terrain[x, y] = ctx.Random.NextDouble() < finalNoise;
                    }
                }

                if (x % Mathf.Max(1, ctx.GenColumnsPerFrame) == 0) yield return null;
            }

            // CA pass throughs
            for (int pass = 0; pass < _smoothPasses; pass++)
            {
                bool[,] smoothedTerrain = new bool[ctx.Width, ctx.Height];

                for (int x = 0; x < ctx.Width; x++)
                {
                    for (int y = 0; y < ctx.Height; y++)
                    {
                        int filledNeighbors = 0;

                        for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
                        {
                            for (int neighborY = y - 1; neighborY <= y + 1; neighborY++)
                            {
                                if (neighborX == x && neighborY == y) continue;
                                if (neighborX < 0 || neighborX >= ctx.Width || neighborY < 0 || neighborY >= ctx.Height) continue;
                                if (terrain[neighborX, neighborY])
                                {
                                    filledNeighbors++;
                                }
                            }
                        }

                        smoothedTerrain[x, y] = filledNeighbors >= 5;
                    }

                    if (x % Mathf.Max(1, ctx.GenColumnsPerFrame) == 0) yield return null;
                }

                terrain = smoothedTerrain;
            }

            ushort sandId = _sandSO.GetId();
            for (int x = 0; x < ctx.Width; x++)
            {
                for (int y = 0; y < ctx.Height; y++)
                {
                    if (terrain[x, y])
                    {
                        ctx.FgGrid[x, y] = new TileData(sandId);
                    }
                }

                if (x % Mathf.Max(1, ctx.GenColumnsPerFrame) == 0) yield return null;
            }
        }
        
        
    }
}
