using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class LargeIslandsGenStep : WorldGenStep
    {
        [SerializeField] private int _fromCenterIslandDistance = 400;
    
        [Header("Plateau")]
        [SerializeField] private int _baseIslandHeightAboveSL = 4;
        [SerializeField] private int _minPlateauWidth = 20;
        [SerializeField] private int _maxPlateauWidth = 30;
        [SerializeField] private float _plateauDetailFrequency = 0.08f;
        [SerializeField] private float _plateauDetailAmplitude = 4f;

        [Header("Slope")]
        [SerializeField] private int _minSlopeWidth = 10;
        [SerializeField] private int _maxSlopeWidth = 15;
        
    
        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int islandCenter = ctx.Width / 2;
            
            GenerateIsland(ctx, islandCenter - _fromCenterIslandDistance);
            yield return null;
            
            GenerateIsland(ctx, islandCenter);
            yield return null;
            
            GenerateIsland(ctx, islandCenter + _fromCenterIslandDistance);
            yield return null;
        }

        private void GenerateIsland(WorldGenContext ctx, int islandCenter)
        {
            int detailSeed = ctx.Random.Next(0, 100000);
            
            int plateauWidth = ctx.Random.Next(_minPlateauWidth, _maxPlateauWidth + 1);
            int plateauHalfWidth = Mathf.RoundToInt(plateauWidth / 2);
            
            int plateauLeftX = islandCenter - plateauHalfWidth;
            int plateauRightX = islandCenter + plateauHalfWidth;
            
            int slopeWidth = ctx.Random.Next(_minSlopeWidth, _maxSlopeWidth + 1);
            int slopeLeftStartX = plateauLeftX - slopeWidth;
            int slopeRightEndX = plateauRightX + slopeWidth;
            
            int totalIslandWidth = slopeRightEndX - slopeLeftStartX + 1;
            int[] islandHeights = new int[totalIslandWidth];
            
            float leftEdgeSample = Mathf.PerlinNoise((plateauLeftX * _plateauDetailFrequency) + detailSeed, 0);
            float leftEdgeHeight = ctx.SeaLevel + _baseIslandHeightAboveSL + (leftEdgeSample * _plateauDetailAmplitude);

            float rightEdgeSample = Mathf.PerlinNoise((plateauRightX * _plateauDetailFrequency) + detailSeed, 0);
            float rightEdgeHeight = ctx.SeaLevel + _baseIslandHeightAboveSL + (rightEdgeSample * _plateauDetailAmplitude);
            
            // Next iterate across the island
            for(int x = slopeLeftStartX; x <= slopeRightEndX; x++ )
            {
                if (x < 0 || x >= ctx.Width) continue;
                
                float currentHeight;
                int arrayIndex = x - slopeLeftStartX;
                
                if(x < plateauLeftX) // Left Slope
                {
                    float percent = (float)(x - slopeLeftStartX) / slopeWidth;
                    float smoothStep = Mathf.SmoothStep(0f, 1f, percent);
                    int oceanFloorY = ctx.OceanFloorHeightValues[x];
                    currentHeight = Mathf.Lerp(oceanFloorY, leftEdgeHeight, smoothStep);
                }
                else if(x <= plateauRightX) // Plateau
                {
                    float noiseSample = Mathf.PerlinNoise((x * _plateauDetailFrequency) + detailSeed, 0);
                    currentHeight = ctx.SeaLevel + _baseIslandHeightAboveSL + (noiseSample * _plateauDetailAmplitude);
                }
                else // Right Slope
                {
                    float percent = (float)(x - plateauRightX) / slopeWidth;
                    float smoothStep = Mathf.SmoothStep(0f, 1f, percent);
                    int oceanFloorY = ctx.OceanFloorHeightValues[x];
                    currentHeight = Mathf.Lerp(rightEdgeHeight, oceanFloorY, smoothStep);
                }
                
                islandHeights[arrayIndex] = Mathf.Clamp(Mathf.RoundToInt(currentHeight), 0, ctx.Height - 1);
            }
            
            // next alter the ocean floor heights to create the island
            for(int x = slopeLeftStartX; x <= slopeRightEndX; x++)
            {
                if(x < 0 || x >= ctx.Width) continue;

                int arrayIndex = x - slopeLeftStartX;
                ctx.OceanFloorHeightValues[x] = Mathf.Max(ctx.OceanFloorHeightValues[x], islandHeights[arrayIndex]); // Makes sure it is above the ocean floor
            }
        }
    }
}
