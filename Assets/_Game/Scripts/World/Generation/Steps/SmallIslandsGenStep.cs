using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class SmallIslandsGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _islandTile;
        [SerializeField] private int _minIslandWidth;
        [SerializeField] private int _maxIslandWidth;
        [SerializeField] private int _minIslandHeight;
        [SerializeField] private int _maxIslandHeight;
        [SerializeField] private int _minIslandGap;
        [SerializeField] private int _maxIslandGap;
        [SerializeField, Range(1f, 5f)] private float _gapBiasPower = 2f;
        [SerializeField] private int _minIslandSeaLevelOffset = -5;
        [SerializeField] private int _maxIslandSeaLevelOffset = 5;
        [SerializeField] private float _noiseFrequency = 0.08f;
        [SerializeField, Range(0f, 1f)] private float _noiseInfluence = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _densityThreshold = 0.45f;
        
        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int x = 0;
            
            while(x < ctx.Width)
            {
                float percent = (float)ctx.Random.NextDouble();
                int seaLevelOffset = Mathf.RoundToInt(Mathf.Lerp(_minIslandSeaLevelOffset, _maxIslandSeaLevelOffset, percent));

                int islandWidth = Mathf.RoundToInt(Mathf.Lerp(_minIslandWidth, _maxIslandWidth, percent));
                int islandHeight = Mathf.RoundToInt(Mathf.Lerp(_minIslandHeight, _maxIslandHeight, percent));
                int halfWidth = Mathf.Max(1, islandWidth / 2);
                
                int islandCenterX = x + halfWidth;
                int islandCenterY = ctx.SeaLevel + seaLevelOffset;
                
                GenerateIsland(ctx, islandCenterX, islandCenterY, islandWidth, islandHeight);

                float gapPercent = (float)ctx.Random.NextDouble();

                if (gapPercent < 0.5f)
                {
                    // Map 0..0.5 to 0..1, square it, then map back to 0..0.5.
                    float normalized = gapPercent / 0.5f;
                    gapPercent = Mathf.Pow(normalized, _gapBiasPower) * 0.5f;
                }
                else
                {
                    // Mirror the same curve around the midpoint.
                    float normalized = (1f - gapPercent) / 0.5f;
                    gapPercent = 1f - (Mathf.Pow(normalized, _gapBiasPower) * 0.5f);
                }

                int gap = Mathf.RoundToInt(Mathf.Lerp(_minIslandGap, _maxIslandGap, gapPercent));
                
                x += Mathf.Max(1, /* islandWidth + */gap); // For now keep them potentially close because so there can organically be large compound islands
                
                yield return null;
            }
            
        }

        private void GenerateIsland(WorldGenContext ctx, int islandCenterX, int islandCenterY, int islandWidth, int islandHeight)
        {
            float seedX = ctx.Random.Next(-100000, 100000);
            float seedY = ctx.Random.Next(-100000, 100000);

            int islandHalfWidth = Mathf.Max(1, islandWidth / 2);
            int islandHalfHeight = Mathf.Max(1, islandHeight / 2);

            int minX = Mathf.Max(0, islandCenterX - islandHalfWidth);
            int maxX = Mathf.Min(ctx.Width - 1, islandCenterX + islandHalfWidth);
            int minY = Mathf.Max(0, islandCenterY - islandHalfHeight);
            int maxY = Mathf.Min(ctx.Height - 1, islandCenterY + islandHalfHeight);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float dx = (float)(x - islandCenterX) / islandHalfWidth;
                    float dy = (float)(y - islandCenterY) / islandHalfHeight;
                    float normalizedDistance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (normalizedDistance > 1.2f) continue;

                    float noiseSample = Mathf.PerlinNoise(x * _noiseFrequency + seedX, y * _noiseFrequency + seedY);
                    float density = (1f - normalizedDistance) + ((noiseSample - 0.5f) * _noiseInfluence);

                    if (density >= _densityThreshold)
                    {
                        ctx.FgGrid[x, y] = new TileData(_islandTile.GetId());
                    }
                }
            }
        }
    }
}
