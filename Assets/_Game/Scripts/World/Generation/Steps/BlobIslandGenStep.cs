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

        [Header("Island Center & Dimensions")]
        [Tooltip("Horizontal radius of the island blob.")]
        [SerializeField] private int _radiusX = 35;

        [Tooltip("Vertical radius of the island blob.")]
        [SerializeField] private int _radiusY = 20;

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
            int minSpacing = Mathf.Max(1, Mathf.Min(_minIslandSpacing, _maxIslandSpacing));
            int maxSpacing = Mathf.Max(minSpacing, _maxIslandSpacing);

            for (int centerX = 0; centerX < ctx.Width;)
            {
                GenerateIsland(ctx, centerX, centerY, seedX, seedY);
                centerX += ctx.Random.Next(minSpacing, maxSpacing + 1);
            }
        }
        
        private void GenerateIsland(WorldGenContext ctx, int centerX, int centerY, float seedX, float seedY)
        {
            // Bounding box for generation
            int minX = Mathf.Max(0, centerX - _radiusX);
            int maxX = Mathf.Min(ctx.Width - 1, centerX + _radiusX);
            int minY = Mathf.Max(0, centerY - _radiusY);
            int maxY = Mathf.Min(ctx.Height - 1, centerY + _radiusY);

            // Iterate through bounding box and calculate density
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // Normalized distance from center (0 at center, 1 at edge of ellipse)
                    float dx = (float)(x - centerX) / _radiusX;
                    float dy = (float)(y - centerY) / _radiusY;
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
        }
    }
}
