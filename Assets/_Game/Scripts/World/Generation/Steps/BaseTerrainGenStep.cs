using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class BaseTerrainGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandSO;
        
        [Header("Starting Island Terrain Settings")]
        [SerializeField] private bool _generateStartingIsland = true;
        [SerializeField, Min(1)] private int _spawnIslandRadiusX = 5;
        [SerializeField, Min(1)] private int _spawnIslandRadiusY = 3;


        [Header("Island Terrain Settings")]
        [SerializeField] private bool _generateIslands = true;
        [SerializeField, Min(1)] private int _maxIslandHeightFromSL = 10;
        [SerializeField, Min(1)] private int _maxIslandDepthFromSL = 10;
        [SerializeField] private float _islandBaseFrequency;
        [SerializeField] private float _islandDetailFrequency;
        [SerializeField, Range(0f, 1f)] private float _islandDetailInfluence = 0.15f;
        [SerializeField] private float _islandSolidThreshold;

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
            
            // Generate a randomized, column-shaped blob inside an elliptical footprint.
            if (_generateStartingIsland)
            {
                float islandCenterX = (width - 1) * 0.5f;
                int minCoreX = Mathf.Max(0, Mathf.CeilToInt(islandCenterX - _spawnIslandRadiusX));
                int maxCoreX = Mathf.Min(width - 1, Mathf.FloorToInt(islandCenterX + _spawnIslandRadiusX));

                for (int x = minCoreX; x <= maxCoreX; x++)
                {
                    float normalizedX = (x - islandCenterX) / _spawnIslandRadiusX;
                    float ellipticalHeight = _spawnIslandRadiusY * Mathf.Sqrt(1f - normalizedX * normalizedX);
                    float heightVariation = ctx.Random.Next(75, 101) / 100f;
                    int columnHalfHeight = Mathf.RoundToInt(ellipticalHeight * heightVariation);
                    if (columnHalfHeight < 1)
                        continue;

                    int minCoreY = Mathf.Max(0, seaLevel - columnHalfHeight);
                    int maxCoreY = Mathf.Min(height - 1, seaLevel + columnHalfHeight);

                    for (int y = minCoreY; y <= maxCoreY; y++)
                    {
                        terrain[x, y] = true;
                    }
                }
            }

            int minIslandZoneY = seaLevel - _maxIslandDepthFromSL;
            int maxIslandZoneY = seaLevel + _maxIslandHeightFromSL;

            // Sea Level Island Generation
            if (_generateIslands)
            {
                float islandBaseSeedX = ctx.Random.Next(-100000, 100000);
                float islandBaseSeedY = ctx.Random.Next(-100000, 100000);
                float islandDetailSeedX = ctx.Random.Next(-100000, 100000);
                float islandDetailSeedY = ctx.Random.Next(-100000, 100000);

                for (int x = 0; x < width; x++)
                {
                    for (int y = minIslandZoneY; y <= maxIslandZoneY; y++)
                    {
                        // Calculate noise value
                        float baseSampleX = (x * _islandBaseFrequency) + islandBaseSeedX;
                        float baseSampleY = (y * _islandBaseFrequency) + islandBaseSeedY;
                        float islandBaseNoise = Mathf.PerlinNoise(baseSampleX, baseSampleY);

                        float detailSampleX = (x * _islandDetailFrequency) + islandDetailSeedX;
                        float detailSampleY = (y * _islandDetailFrequency) + islandDetailSeedY;
                        float detailNoise = Mathf.PerlinNoise(detailSampleX, detailSampleY);

                        float finalNoise = Mathf.Lerp(islandBaseNoise, detailNoise, _islandDetailInfluence);

                        // Fade the threshold toward open sea at both vertical edges of the island zone.
                        float vertical01 = y <= seaLevel ? Mathf.InverseLerp(minIslandZoneY, seaLevel, y) : Mathf.InverseLerp(maxIslandZoneY, seaLevel, y);
                        float islandEnvelope = Mathf.SmoothStep(0f, 1f, vertical01);
                        float solidThreshold = Mathf.Lerp(1f, _islandSolidThreshold, islandEnvelope);

                        terrain[x, y] |= finalNoise >= solidThreshold;
                    }
                }
            }


            // Under Sea Generation
            if (_generateUnderSeaTerrain)
            {
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
            }
            

            // Setting the actual map after generation
            ushort sandId = _sandSO.GetId();
            for (int x = 0; x < ctx.Width; x++)
            {
                for (int y = 0; y < ctx.Height; y++)
                {
                    ctx.FgGrid[x, y] = terrain[x, y] ? new TileData(sandId) : TileData.Air;
                }

                if (x % columnsPerFrame == 0)
                    yield return null;
            }
        }
        
        
    }
}
