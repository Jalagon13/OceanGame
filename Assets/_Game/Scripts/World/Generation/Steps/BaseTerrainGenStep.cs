using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class BaseTerrainGenStep : WorldGenStep
    {
        [Header("Terrain Tile")]
        [Tooltip("The solid tile used for all terrain produced by this step.")]
        [SerializeField] private TileConfigSO _sandSO;

        [Header("Underwater Cave Shape")]
        [Tooltip("Scale of the large cave shapes. Lower values produce larger caves and land masses.")]
        [SerializeField, Min(0.001f)] private float _caveFrequency = 0.06f;

        [Tooltip("Scale of the smaller irregularities added to each cave wall.")]
        [SerializeField, Min(0.001f)] private float _caveDetailFrequency = 0.15f;

        [Tooltip("How much of the cave shape comes from the large noise layer. A lower value makes walls rougher.")]
        [SerializeField, Range(0f, 1f)] private float _caveBaseNoiseWeight = 0.72f;

        [Header("Depth Density")]
        [Tooltip("Noise value required to create terrain immediately below sea level. Higher values leave more open water near the surface.")]
        [SerializeField, Range(0f, 1f)] private float _shallowSolidThreshold = 0.58f;

        [Tooltip("Noise value required to create terrain at the bottom of the map. Lower values make the deep ocean denser.")]
        [SerializeField, Range(0f, 1f)] private float _deepSolidThreshold = 0.30f;

        [Tooltip("Controls where the density increase happens. Values above 1 keep the upper ocean open for longer; values below 1 make it densify sooner.")]
        [SerializeField, Min(0.01f)] private float _depthDensityExponent = 1.1f;

        [Header("Cave Polish")]
        [Tooltip("Number of cellular-automata cleanup passes applied to underwater terrain. More passes create rounder, less noisy caves.")]
        [SerializeField, Min(0)] private int _smoothPasses = 2;

        [Tooltip("A tile becomes solid during a cleanup pass when it has at least this many solid neighbours. Five is a balanced starting point.")]
        [SerializeField, Range(1, 8)] private int _solidNeighboursToFill = 5;

        [Header("Sea-Level Islands")]
        [Tooltip("Horizontal scale of the island groups along sea level. Lower values create wider, fewer island groups.")]
        [SerializeField, Min(0.001f)] private float _islandProfileFrequency = 0.075f;

        [Tooltip("Scale of the ragged edge noise used to break island groups into natural-looking shapes.")]
        [SerializeField, Min(0.001f)] private float _islandDetailFrequency = 0.18f;

        [Tooltip("How strong a horizontal island signal must be before an island group appears. Higher values produce fewer islands.")]
        [SerializeField, Range(0f, 1f)] private float _islandThreshold = 0.45f;

        [Tooltip("Maximum number of tiles an island can rise above Sea Level.")]
        [SerializeField, Min(0)] private int _maxIslandHeight = 10;

        [Tooltip("How far the sea-level island noise reaches below Sea Level to form reefs and shallow overhangs.")]
        [SerializeField, Min(0)] private int _islandSubmergedDepth = 14;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            if (_sandSO == null)
            {
                Debug.LogError("[BaseTerrainGenStep] A terrain TileConfigSO must be assigned before generating a world.");
                yield break;
            }

            int seaLevel = Mathf.Clamp(ctx.SeaLevel, 0, ctx.Height - 1);
            int columnsPerFrame = Mathf.Max(1, ctx.GenColumnsPerFrame);
            bool[,] terrain = new bool[ctx.Width, ctx.Height];

            // Each noise layer receives deterministic offsets so a seed always recreates the same world.
            Vector2 caveBaseOffset = CreateNoiseOffset(ctx);
            Vector2 caveDetailOffset = CreateNoiseOffset(ctx);
            Vector2 islandProfileOffset = CreateNoiseOffset(ctx);
            Vector2 islandDetailOffset = CreateNoiseOffset(ctx);

            // 1. Carve the broad underwater cave field. The solid threshold drops with depth,
            //    leaving spacious water near Sea Level and increasingly dense terrain below it.
            for (int x = 0; x < ctx.Width; x++)
            {
                for (int y = 0; y <= seaLevel; y++)
                {
                    float depth01 = Mathf.InverseLerp(seaLevel, 0f, y);
                    float density01 = Mathf.Pow(depth01, _depthDensityExponent);
                    float solidThreshold = Mathf.Lerp(_shallowSolidThreshold, _deepSolidThreshold, density01);
                    float caveNoise = SampleBlendedNoise(x, y, _caveFrequency, _caveDetailFrequency,
                        _caveBaseNoiseWeight, caveBaseOffset, caveDetailOffset);

                    terrain[x, y] = caveNoise >= solidThreshold;
                }

                if (x % columnsPerFrame == 0)
                    yield return null;
            }

            // 2. Smooth only the submerged terrain. This joins stray pixels into readable caves
            //    without eroding the deliberately jagged island silhouettes above the water.
            for (int pass = 0; pass < _smoothPasses; pass++)
            {
                bool[,] smoothedTerrain = new bool[ctx.Width, ctx.Height];

                for (int x = 0; x < ctx.Width; x++)
                {
                    for (int y = 0; y <= seaLevel; y++)
                    {
                        smoothedTerrain[x, y] = CountSolidNeighbours(terrain, ctx, x, y, seaLevel) >= _solidNeighboursToFill;
                    }

                    if (x % columnsPerFrame == 0)
                        yield return null;
                }

                terrain = smoothedTerrain;
            }

            // 3. Overlay a separate, two-noise island band centred on Sea Level. The horizontal
            //    profile decides where an island group exists; detail noise makes its coast and
            //    submerged reef uneven instead of a flat, continuous shoreline.
            for (int x = 0; x < ctx.Width; x++)
            {
                float profileNoise = Mathf.PerlinNoise((x * _islandProfileFrequency) + islandProfileOffset.x, islandProfileOffset.y);
                float islandProfile = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_islandThreshold, 1f, profileNoise));

                if (islandProfile > 0f)
                {
                    int minY = Mathf.Max(0, seaLevel - _islandSubmergedDepth);
                    int maxY = Mathf.Min(ctx.Height - 1, seaLevel + _maxIslandHeight);

                    for (int y = minY; y <= maxY; y++)
                    {
                        int distanceFromSeaLevel = y - seaLevel;
                        float verticalFade = distanceFromSeaLevel >= 0
                            ? 1f - (distanceFromSeaLevel / (float)Mathf.Max(1, _maxIslandHeight + 1))
                            : 1f - (-distanceFromSeaLevel / (float)Mathf.Max(1, _islandSubmergedDepth + 1));

                        float islandCoverage = islandProfile * Mathf.Clamp01(verticalFade);
                        float edgeNoise = Mathf.PerlinNoise((x * _islandDetailFrequency) + islandDetailOffset.x,
                            (y * _islandDetailFrequency) + islandDetailOffset.y);

                        if (edgeNoise <= islandCoverage)
                            terrain[x, y] = true;
                    }
                }

                if (x % columnsPerFrame == 0)
                    yield return null;
            }

            // 4. Convert the finished boolean terrain mask into the foreground tiles used by the rest of the pipeline.
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

        private Vector2 CreateNoiseOffset(WorldGenContext ctx)
        {
            return new Vector2(ctx.Random.Next(-100000, 100001), ctx.Random.Next(-100000, 100001));
        }

        private float SampleBlendedNoise(int x, int y, float baseFrequency, float detailFrequency,
            float baseWeight, Vector2 baseOffset, Vector2 detailOffset)
        {
            float baseNoise = Mathf.PerlinNoise((x * baseFrequency) + baseOffset.x, (y * baseFrequency) + baseOffset.y);
            float detailNoise = Mathf.PerlinNoise((x * detailFrequency) + detailOffset.x, (y * detailFrequency) + detailOffset.y);
            return Mathf.Lerp(detailNoise, baseNoise, baseWeight);
        }

        private int CountSolidNeighbours(bool[,] terrain, WorldGenContext ctx, int x, int y, int seaLevel)
        {
            int solidNeighbours = 0;

            for (int neighbourX = x - 1; neighbourX <= x + 1; neighbourX++)
            {
                for (int neighbourY = y - 1; neighbourY <= y + 1; neighbourY++)
                {
                    if (neighbourX == x && neighbourY == y)
                        continue;

                    // Anything outside the world, including the water above Sea Level, stays open.
                    // This allows caves to naturally reach the map sides instead of being sealed by a rim.
                    bool insideTerrainBounds = neighbourX >= 0 && neighbourX < ctx.Width && neighbourY >= 0 && neighbourY <= seaLevel;
                    if (insideTerrainBounds && terrain[neighbourX, neighbourY])
                    {
                        solidNeighbours++;
                    }
                }
            }

            return solidNeighbours;
        }
    }
}
