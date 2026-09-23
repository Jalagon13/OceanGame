using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class BlobIslandGenStep : WorldGenStep
    {
        [Header("Tile Settings")]
        [SerializeField] private TileConfigSO _islandTile;
        [SerializeField] private TileConfigSO _islandBgTile;

        [Header("Island Center & Dimensions")]
        [Tooltip("Minimum horizontal radius of an island blob.")]
        [SerializeField] private int _minRadiusX = 20;

        [Tooltip("Maximum horizontal radius of an island blob.")]
        [SerializeField] private int _maxRadiusX = 35;

        [Tooltip("Minimum vertical radius of an island blob.")]
        [SerializeField] private int _minRadiusY = 12;

        [Tooltip("Maximum vertical radius of an island blob.")]
        [SerializeField] private int _maxRadiusY = 20;

        [Tooltip("Vertical offset from SeaLevel (positive shifts center upward, negative downward).")]
        [SerializeField] private int _yOffset = 0;

        [Header("Island Spacing")]
        [Tooltip("Minimum horizontal distance between island centers.")]
        [SerializeField] private int _minIslandSpacing = 15;

        [Tooltip("Maximum horizontal distance between island centers.")]
        [SerializeField] private int _maxIslandSpacing = 30;

        [Header("Perlin Noise Shape Settings")]
        [Tooltip("Frequency of the 2D Perlin noise. Higher values create more jagged edges.")]
        [SerializeField] private float _noiseFrequency = 0.08f;

        [Tooltip("How strongly the Perlin noise perturbs the shape (0 = pure ellipse, 1 = heavily distorted).")]
        [SerializeField, Range(0f, 1f)] private float _noiseInfluence = 0.5f;

        [Tooltip("Density cutoff threshold to place a tile. Lower values make the island bigger/fuller.")]
        [SerializeField, Range(0f, 1f)] private float _densityThreshold = 0.45f;

        [Header("Island Background Settings")]
        [Tooltip("Horizontal distance over which the island background slopes down to the ocean floor.")]
        [SerializeField] private int _backgroundSlopeWidth = 10;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            if (_islandTile == null)
            {
                Debug.LogWarning("[BlobIslandGenStep] Island TileConfigSO is not assigned!");
                yield break;
            }

            // Generate random seeds for 2D Perlin noise sampling
            float seedX = ctx.Random.Next(-100000, 100000);
            float seedY = ctx.Random.Next(-100000, 100000);
            
            int centerY = ctx.SeaLevel + _yOffset;
            int minGap = Mathf.Max(1, Mathf.Min(_minIslandSpacing, _maxIslandSpacing));
            int maxGap = Mathf.Max(minGap, _maxIslandSpacing);
            int minRadiusX = Mathf.Max(1, Mathf.Min(_minRadiusX, _maxRadiusX));
            int maxRadiusX = Mathf.Max(minRadiusX, _maxRadiusX);
            int minRadiusY = Mathf.Max(1, Mathf.Min(_minRadiusY, _maxRadiusY));
            int maxRadiusY = Mathf.Max(minRadiusY, _maxRadiusY);

            int previousCenterX = -1;
            int previousRadiusX = 0;
            while (true)
            {
                int radiusX = ctx.Random.Next(minRadiusX, maxRadiusX + 1);
                float widthPercent = maxRadiusX == minRadiusX
                    ? 0f
                    : Mathf.InverseLerp(minRadiusX, maxRadiusX, radiusX);
                int radiusY = Mathf.RoundToInt(Mathf.Lerp(minRadiusY, maxRadiusY, widthPercent));
                int gap = ctx.Random.Next(minGap, maxGap + 1);
                int centerX = previousCenterX < 0
                    ? radiusX
                    : previousCenterX + previousRadiusX + gap + radiusX;

                if (centerX - radiusX >= ctx.Width) break;

                GenerateIsland(ctx, centerX, centerY, radiusX, radiusY, seedX, seedY);
                previousCenterX = centerX;
                previousRadiusX = radiusX;
            }
        }
        
        private void GenerateIsland(WorldGenContext ctx, int centerX, int centerY, int radiusX, int radiusY, float seedX, float seedY)
        {
            // Bounding box for generation
            int minX = Mathf.Max(0, centerX - radiusX);
            int maxX = Mathf.Min(ctx.Width - 1, centerX + radiusX);
            int minY = Mathf.Max(0, centerY - radiusY);
            int maxY = Mathf.Min(ctx.Height - 1, centerY + radiusY);

            // Iterate through bounding box and calculate density
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Normalized distance from center (0 at center, 1 at edge of ellipse)
                    float dx = (float)(x - centerX) / radiusX;
                    float dy = (float)(y - centerY) / radiusY;
                    float normalizedDist = Mathf.Sqrt((dx * dx) + (dy * dy));

                    // Skip positions completely outside our maximum possible noise reach
                    if (normalizedDist > 1.2f) continue;

                    // Sample 2D Perlin noise in [0, 1] range
                    float noiseSample = Mathf.PerlinNoise((x * _noiseFrequency) + seedX, (y * _noiseFrequency) + seedY);

                    // Calculate density: radial falloff + noise variation (centered around 0)
                    float density = (1f - normalizedDist) + ((noiseSample - 0.5f) * _noiseInfluence);

                    // If density exceeds threshold, fill foreground tile
                    if (density >= _densityThreshold)
                    {
                        ctx.FgGrid[x, y] = new TileData(_islandTile.GetId());
                        // ctx.SurfaceHeightValues[x] = Mathf.Max(ctx.SurfaceHeightValues[x], y);
                    }
                }
            }
            
            if (_islandBgTile == null)
            {
                return;
            }

            int slopeWidth = Mathf.Max(1, _backgroundSlopeWidth);
            int islandLeftX = FindBlobEdgeX(ctx, centerX, centerY, radiusX, radiusY, seedX, seedY, true);
            int islandRightX = FindBlobEdgeX(ctx, centerX, centerY, radiusX, radiusY, seedX, seedY, false);
            if (islandLeftX < 0 || islandRightX < 0) return;

            int slopeLeftStartX = islandLeftX - slopeWidth;
            int slopeRightEndX = islandRightX + slopeWidth;
            int backgroundBottomY = Mathf.Clamp(ctx.UndergroundBottomLevel, 0, ctx.Height - 1);
            int leftEdgeTopY = GetBlobSurfaceY(ctx, centerX, islandLeftX, centerY, radiusX, radiusY, seedX, seedY);
            int rightEdgeTopY = GetBlobSurfaceY(ctx, centerX, islandRightX, centerY, radiusX, radiusY, seedX, seedY);

            for (int x = Mathf.Max(0, slopeLeftStartX); x <= Mathf.Min(ctx.Width - 1, slopeRightEndX); x++)
            {
                int backgroundTopY;

                if (x < islandLeftX)
                {
                    float t = Mathf.Clamp01((float)(x - slopeLeftStartX) / slopeWidth);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    backgroundTopY = Mathf.RoundToInt(Mathf.Lerp(ctx.SurfaceHeightValues[x], leftEdgeTopY, smoothT));
                }
                else if (x > islandRightX)
                {
                    float t = Mathf.Clamp01((float)(slopeRightEndX - x) / slopeWidth);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    backgroundTopY = Mathf.RoundToInt(Mathf.Lerp(ctx.SurfaceHeightValues[x], rightEdgeTopY, smoothT));
                }
                else
                {
                    backgroundTopY = GetBlobSurfaceY(ctx, centerX, x, centerY, radiusX, radiusY, seedX, seedY);
                }

                if (backgroundTopY < 0) continue;

                backgroundTopY = Mathf.Clamp(backgroundTopY, backgroundBottomY, ctx.Height - 1);
                for (int y = backgroundBottomY; y <= backgroundTopY; y++)
                {
                    ctx.BgGrid[x, y] = new TileData(_islandBgTile.GetId());
                }
            }
        }

        private int GetBlobSurfaceY(WorldGenContext ctx, int centerX, int x, int centerY, int radiusX, int radiusY, float seedX, float seedY)
        {
            if (x < 0 || x >= ctx.Width || radiusX <= 0 || radiusY <= 0)
                return -1;

            int minY = Mathf.Max(0, centerY - radiusY);
            int maxY = Mathf.Min(ctx.Height - 1, centerY + radiusY);

            for (int y = maxY; y >= minY; y--)
            {
                float dx = (float)(x - centerX) / radiusX;
                float dy = (float)(y - centerY) / radiusY;
                float normalizedDist = Mathf.Sqrt((dx * dx) + (dy * dy));

                if (normalizedDist > 1.2f) continue;

                float noiseSample = Mathf.PerlinNoise((x * _noiseFrequency) + seedX, (y * _noiseFrequency) + seedY);
                float density = (1f - normalizedDist) + ((noiseSample - 0.5f) * _noiseInfluence);

                if (density >= _densityThreshold)
                    return y;
            }

            return -1;
        }

        private int FindBlobEdgeX(WorldGenContext ctx, int centerX, int centerY, int radiusX, int radiusY, float seedX, float seedY, bool searchLeft)
        {
            int startX = searchLeft ? Mathf.Max(0, centerX - radiusX) : Mathf.Min(ctx.Width - 1, centerX + radiusX);
            int endX = searchLeft ? Mathf.Min(ctx.Width - 1, centerX) : Mathf.Max(0, centerX);
            int step = searchLeft ? 1 : -1;

            for (int x = startX; searchLeft ? x <= endX : x >= endX; x += step)
            {
                if (GetBlobSurfaceY(ctx, centerX, x, centerY, radiusX, radiusY, seedX, seedY) >= 0)
                    return x;
            }

            return -1;
        }
    }
}
