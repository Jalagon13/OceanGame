using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class UnderSeaTerrainGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _limestoneSO;

        [Header("UnderSea Terrain Settings")]
        [SerializeField] private bool _generateUnderSeaTerrain = true;
        [SerializeField, Min(1)] private int _seaLevelOffset = 8; // subtracts it before the undersea generation starts
        [SerializeField, Min(1)] private int _fadeToGenTopYStart = 6;
        [SerializeField] private float _baseFrequency;
        [SerializeField] private float _detailFrequency;
        [SerializeField, Range(0f, 1f)] private float _detailInfluence = 0.15f;
        [Range(0f, 1f)][SerializeField] private float _shallowSolidThreshold;
        [Range(0f, 1f)][SerializeField] private float _deepSolidThreshold;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int width = ctx.Width;
            int height = ctx.Height;
            int seaLevel = ctx.SeaLevel;
            int columnsPerFrame = Mathf.Max(1, ctx.GenColumnsPerFrame);
            bool[,] terrain = new bool[ctx.Width, ctx.Height];

            // Under Sea Generation
            float seaTerrainBaseSeedX = ctx.Random.Next(-100000, 100000);
            float seaTerrainBaseSeedY = ctx.Random.Next(-100000, 100000);
            float seaTerrainDetailSeedX = ctx.Random.Next(-100000, 100000);
            float seaTerrainDetailSeedY = ctx.Random.Next(-100000, 100000);

            int generationTopY = seaLevel - _seaLevelOffset;
            int fadeStartY = generationTopY - _fadeToGenTopYStart;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < generationTopY; y++)
                {
                    // Calculate noise value
                    float baseSampleX = (x * _baseFrequency) + seaTerrainBaseSeedX;
                    float baseSampleY = (y * _baseFrequency) + seaTerrainBaseSeedY;
                    float baseNoise = Mathf.PerlinNoise(baseSampleX, baseSampleY);

                    float detailSampleX = (x * _detailFrequency) + seaTerrainDetailSeedX;
                    float detailSampleY = (y * _detailFrequency) + seaTerrainDetailSeedY;
                    float detailNoise = Mathf.PerlinNoise(detailSampleX, detailSampleY);

                    float finalNoise = Mathf.Lerp(baseNoise, detailNoise, _detailInfluence);

                    // Calculate solid threshold
                    float depth01 = Mathf.InverseLerp(seaLevel - 1, 0, y);
                    float shallow = Mathf.Clamp01(_shallowSolidThreshold);
                    float deep = Mathf.Clamp01(_deepSolidThreshold);
                    float solidThreshold = Mathf.Lerp(shallow, deep, depth01);

                    // Gradually fade it so its not a sharp cut off
                    if (y >= fadeStartY)
                    {
                        float fade01 = Mathf.InverseLerp(fadeStartY, generationTopY, y);
                        solidThreshold = Mathf.Lerp(solidThreshold, 1f, fade01);
                    }

                    terrain[x, y] |= finalNoise >= solidThreshold;
                }

                if (x % columnsPerFrame == 0)
                    yield return null;
            }

            // Setting the actual map after generation
            ushort sandId = _limestoneSO.GetId();
            for (int x = 0; x < ctx.Width; x++)
            {
                for (int y = 0; y < ctx.Height; y++)
                {
                    if (terrain[x, y] && ctx.FgGrid[x, y].IsAir)
                        ctx.FgGrid[x, y] = new TileData(sandId);
                }

                if (x % columnsPerFrame == 0)
                    yield return null;
            }
        }
        
        
    }
}
