using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class SurfaceIslandsGenStep : WorldGenStep
    {
        [Header("Surface Generation")]
        [SerializeField] private int _minIslandGap = 15;
        [SerializeField] private int _maxIslandGap = 30;
    
        [Header("Island Generation")]
        [SerializeField] private TileConfigSO _islandTile;
        [SerializeField, Min(1)] private int _minIslandBlobCount = 3;
        [SerializeField, Min(1)] private int _maxIslandBlobCount = 8;
        [SerializeField, Min(1)] private int _minBlobWidth = 15;
        [SerializeField, Min(1)] private int _maxBlobWidth = 39;
        [SerializeField, Min(1)] private int _minBlobHeight = 6;
        [SerializeField, Min(1)] private int _maxBlobHeight = 17;
        [SerializeField, Min(0)] private int _minBlobGap = 1;
        [SerializeField, Min(0)] private int _maxBlobGap = 4;

        [Header("Blob Settings")]
        [SerializeField] private float _noiseFrequency = 0.08f;
        [SerializeField, Range(0f, 1f)] private float _noiseInfluence = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _densityThreshold = 0.45f;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            GenerateIsland(ctx, ctx.Width / 2, ctx.SeaLevel);
            yield break;
        }

        private void GenerateIsland(WorldGenContext ctx, int islandCenterX, int islandCenterY)
        {
            int minBlobCount = _minIslandBlobCount;
            int maxBlobCount = _maxIslandBlobCount;
            int blobCount = ctx.Random.Next(minBlobCount, maxBlobCount + 1);

            int[] blobWidths = new int[blobCount];
            int[] blobHeights = new int[blobCount];
            int[] blobGaps = new int[blobCount - 1];
            int totalCenterDistance = 0;

            for (int i = 0; i < blobCount; i++)
            {
                float position = blobCount == 1 ? 0.5f : (float)i / (blobCount - 1);
                float centerBias = 1f - Mathf.Abs(position * 2f - 1f);
                float sizePercent = Mathf.Lerp(Mathf.Pow((float)ctx.Random.NextDouble(), 2f), 1f, centerBias);

                blobWidths[i] = Mathf.Max(1, GetRandomSize(_minBlobWidth, _maxBlobWidth, sizePercent));
                blobHeights[i] = Mathf.Max(1, GetRandomSize(_minBlobHeight, _maxBlobHeight, sizePercent));

                if (i < blobGaps.Length)
                {
                    blobGaps[i] = GetRandomSize(_minBlobGap, _maxBlobGap, (float)ctx.Random.NextDouble());
                    totalCenterDistance += blobGaps[i];
                }
            }

            float currentCenterX = islandCenterX - totalCenterDistance / 2f;

            for (int i = 0; i < blobCount; i++)
            {
                GenerateBlob(ctx, currentCenterX, islandCenterY, blobWidths[i], blobHeights[i]);

                if (i < blobGaps.Length)
                    currentCenterX += blobGaps[i];
            }
        }

        private int GetRandomSize(int configuredMin, int configuredMax, float percent)
        {
            int min = Mathf.Max(0, Mathf.Min(configuredMin, configuredMax));
            int max = Mathf.Max(min, Mathf.Max(configuredMin, configuredMax));
            return Mathf.RoundToInt(Mathf.Lerp(min, max, percent));
        }

        private void GenerateBlob(WorldGenContext ctx, float centerX, int centerY, int width, int height)
        {
            float seedX = ctx.Random.Next(-100000, 100000);
            float seedY = ctx.Random.Next(-100000, 100000);

            float halfWidth = Mathf.Max(0.5f, (width - 1) / 2f);
            int startY = centerY - height / 2;
            float blobCenterY = startY + (height - 1) / 2f;
            float halfHeight = Mathf.Max(0.5f, (height - 1) / 2f);

            int minX = Mathf.Max(0, Mathf.FloorToInt(centerX - halfWidth));
            int maxX = Mathf.Min(ctx.Width - 1, Mathf.CeilToInt(centerX + halfWidth));
            int minY = Mathf.Max(0, startY);
            int maxY = Mathf.Min(ctx.Height - 1, startY + height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float dx = (x - centerX) / halfWidth;
                    float dy = (y - blobCenterY) / halfHeight;
                    float normalizedDistance = Mathf.Sqrt(dx * dx + dy * dy);

                    if (normalizedDistance > 1.2f)
                        continue;

                    float noiseSample = Mathf.PerlinNoise(x * _noiseFrequency + seedX, y * _noiseFrequency + seedY);
                    float density = (1f - normalizedDistance) + ((noiseSample - 0.5f) * _noiseInfluence);

                    if (density >= _densityThreshold)
                        ctx.FgGrid[x, y] = new TileData(_islandTile.GetId());
                }
            }
        }
    }
}
