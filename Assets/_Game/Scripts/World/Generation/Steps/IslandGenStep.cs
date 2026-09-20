using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class IslandGenStep : WorldGenStep
    {
        [Header("Island Center & Size")]
        [Tooltip("If true, places test island at ctx.Width / 2. If false, uses _customCenterColumn.")]
        [SerializeField] private int _plateauHalfWidth = 15; // 30 tiles wide total
        [SerializeField] private int _slopeWidth = 20; // 20 tiles transition down to sea floor

        [Header("Elevation & Terrain Detail")]
        [Tooltip("Base height above ctx.SeaLevel")]
        [SerializeField] private int _islandBaseHeight = 6;

        [Tooltip("Detail noise frequency along the island top")]
        [SerializeField] private float _plateauDetailFrequency = 0.08f;

        [Tooltip("Detail noise height amplitude")]
        [SerializeField] private float _plateauDetailAmplitude = 4f;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int seedDetail = ctx.Random.Next(0, 100000);

            // 1. Define Key X Boundaries for the Island
            int islandCenter = ctx.Width / 2;
            int plateauLeftX = islandCenter - _plateauHalfWidth;
            int plateauRightX = islandCenter + _plateauHalfWidth;

            int slopeLeftStartX = plateauLeftX - _slopeWidth;
            int slopeRightEndX = plateauRightX + _slopeWidth;

            // Total width of the island footprint (slopes + plateau)
            int totalIslandWidth = slopeRightEndX - slopeLeftStartX + 1;

            // 2. Temporary array to hold our generated island heights
            int[] islandHeights = new int[totalIslandWidth];

            // 3. First, calculate the height at the edges of the plateau so slopes connect seamlessly
            float leftEdgeSample = Mathf.PerlinNoise((plateauLeftX * _plateauDetailFrequency) + seedDetail, 0);
            float leftEdgeHeight = ctx.SeaLevel + _islandBaseHeight + (leftEdgeSample * _plateauDetailAmplitude);

            float rightEdgeSample = Mathf.PerlinNoise((plateauRightX * _plateauDetailFrequency) + seedDetail, 0);
            float rightEdgeHeight = ctx.SeaLevel + _islandBaseHeight + (rightEdgeSample * _plateauDetailAmplitude);

            // 4. Generate heights across the entire island footprint
            for (int x = slopeLeftStartX; x <= slopeRightEndX; x++)
            {
                // Make sure we stay within world bounds
                if (x < 0 || x >= ctx.Width) continue;

                int arrayIndex = x - slopeLeftStartX;
                float currentHeight = 0f;

                // --- ZONE A: Left Slope (Seabed -> Plateau Left Edge) ---
                if (x < plateauLeftX)
                {
                    float t = (float)(x - slopeLeftStartX) / _slopeWidth; // 0.0 to 1.0
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    int oceanFloorY = ctx.SurfaceHeightValues[x];
                    currentHeight = Mathf.Lerp(oceanFloorY, leftEdgeHeight, smoothT);
                }
                // --- ZONE B: Plateau (Island Top with Noise) ---
                else if (x <= plateauRightX)
                {
                    float noiseSample = Mathf.PerlinNoise((x * _plateauDetailFrequency) + seedDetail, 0);
                    currentHeight = ctx.SeaLevel + _islandBaseHeight + (noiseSample * _plateauDetailAmplitude);
                }
                // --- ZONE C: Right Slope (Plateau Right Edge -> Seabed) ---
                else
                {
                    float t = (float)(slopeRightEndX - x) / _slopeWidth; // 1.0 to 0.0
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    int oceanFloorY = ctx.SurfaceHeightValues[x];
                    currentHeight = Mathf.Lerp(oceanFloorY, rightEdgeHeight, smoothT);
                }

                islandHeights[arrayIndex] = Mathf.Clamp(Mathf.RoundToInt(currentHeight), 0, ctx.Height - 1);
            }

            // 5. Replace the section in ctx.SurfaceHeightValues with our island heights
            for (int x = slopeLeftStartX; x <= slopeRightEndX; x++)
            {
                if (x < 0 || x >= ctx.Width) continue;

                int arrayIndex = x - slopeLeftStartX;
                
                // Use Max so we only elevate terrain, never cut below existing seabed
                ctx.SurfaceHeightValues[x] = Mathf.Max(ctx.SurfaceHeightValues[x], islandHeights[arrayIndex]);
            }

            yield return null;
        }
    }
}

