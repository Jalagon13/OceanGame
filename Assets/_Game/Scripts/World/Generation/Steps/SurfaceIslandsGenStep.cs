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
        [SerializeField] private TileConfigSO _bgTile;
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
            IslandLayout centerLayout = CreateIslandLayout(ctx);
            IslandBounds centerBounds = GenerateIsland(ctx, centerLayout, ctx.Width / 2, ctx.SeaLevel);

            int rightEdge = centerBounds.RightEdge;
            while (rightEdge < ctx.Width - 1)
            {
                IslandLayout layout = CreateIslandLayout(ctx);
                
                int islandGap = GetRandomSize(_minIslandGap, _maxIslandGap, (float)ctx.Random.NextDouble());
                int islandLeft = rightEdge + islandGap + 1;
                int islandCenterX = islandLeft - layout.LeftOffset;
                
                IslandBounds bounds = GenerateIsland(ctx, layout, islandCenterX, ctx.SeaLevel);
                
                rightEdge = bounds.RightEdge;
            }

            int leftEdge = centerBounds.LeftEdge;
            while (leftEdge > 0)
            {
                IslandLayout layout = CreateIslandLayout(ctx);
                
                int islandGap = GetRandomSize(_minIslandGap, _maxIslandGap, (float)ctx.Random.NextDouble());
                int islandRight = leftEdge - islandGap - 1;
                int islandCenterX = islandRight - layout.RightOffset;
                
                IslandBounds bounds = GenerateIsland(ctx, layout, islandCenterX, ctx.SeaLevel);
                
                leftEdge = bounds.LeftEdge;
            }

            yield break;
        }

        private IslandLayout CreateIslandLayout(WorldGenContext ctx)
        {
            int minBlobCount = _minIslandBlobCount;
            int maxBlobCount = _maxIslandBlobCount;
            int blobCount = ctx.Random.Next(minBlobCount, maxBlobCount + 1);

            int[] blobWidths = new int[blobCount];
            int[] blobHeights = new int[blobCount];
            int[] blobGaps = new int[blobCount - 1];
            float[] blobCenterOffsets = new float[blobCount];
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

            float currentCenterOffset = -totalCenterDistance / 2f;
            int leftOffset = int.MaxValue;
            int rightOffset = int.MinValue;

            for (int i = 0; i < blobCount; i++)
            {
                blobCenterOffsets[i] = currentCenterOffset;
                float halfWidth = Mathf.Max(0.5f, (blobWidths[i] - 1) / 2f);
                leftOffset = Mathf.Min(leftOffset, Mathf.FloorToInt(currentCenterOffset - halfWidth));
                rightOffset = Mathf.Max(rightOffset, Mathf.CeilToInt(currentCenterOffset + halfWidth));

                if (i < blobGaps.Length)
                    currentCenterOffset += blobGaps[i];
            }

            return new IslandLayout(blobWidths, blobHeights, blobCenterOffsets, leftOffset, rightOffset);
        }

        private IslandBounds GenerateIsland(WorldGenContext ctx, IslandLayout layout, int islandCenterX, int islandCenterY)
        {
            for (int i = 0; i < layout.BlobWidths.Length; i++)
            {
                GenerateBlob(ctx, islandCenterX + layout.BlobCenterOffsets[i], islandCenterY,
                    layout.BlobWidths[i], layout.BlobHeights[i]);
            }

            return new IslandBounds(islandCenterX + layout.LeftOffset, islandCenterX + layout.RightOffset);
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

        private sealed class IslandLayout
        {
            public int[] BlobWidths { get; }
            public int[] BlobHeights { get; }
            public float[] BlobCenterOffsets { get; }
            public int LeftOffset { get; }
            public int RightOffset { get; }

            public IslandLayout(int[] blobWidths, int[] blobHeights, float[] blobCenterOffsets, int leftOffset, int rightOffset)
            {
                BlobWidths = blobWidths;
                BlobHeights = blobHeights;
                BlobCenterOffsets = blobCenterOffsets;
                LeftOffset = leftOffset;
                RightOffset = rightOffset;
            }
        }

        private readonly struct IslandBounds
        {
            public int LeftEdge { get; }
            public int RightEdge { get; }

            public IslandBounds(int left, int right)
            {
                LeftEdge = left;
                RightEdge = right;
            }
        }
    }
}
