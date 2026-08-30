using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class CaveGenStep : WorldGenStep
    {
        [Header("Cave Gen")]
        [SerializeField, Range(0, 1f)] private float _minFillProb = 0.325f;
        [SerializeField, Range(0, 1f)] private float _maxFillProb = 0.45f;
        [SerializeField] private float _caveNoiseFrequency = 0.015f; // Far fast it steps through the perlin noise
        [SerializeField, Range(0, 6f)] private int _smoothingPasses = 2;
    
        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int width = ctx.Width;
            int height = ctx.Height;
        
            float seedX = ctx.Random.Next(0, 100000);
            float seedY = ctx.Random.Next(0, 100000);
            
            // First populate the cave grid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sampleX = (x * _caveNoiseFrequency) + seedX;
                    float sampleY = (x * _caveNoiseFrequency) + seedY;
                    float noise = Mathf.PerlinNoise(sampleX, sampleY);
                    float fillProb = Mathf.Lerp(_minFillProb, _maxFillProb, noise);
                    
                    if(ctx.Random.NextDouble() >= fillProb)
                    {
                        ctx.CaveGrid[x, y] = true; // True in this case is an air space in the cave
                    }
                    
                }
            }
            
            // Next do CA smooth passes
            for (int passes = 0; passes < _smoothingPasses; passes++)
            {
                bool[,] bufferGrid = ctx.CaveGrid.Clone() as bool[,];
            
                for (int x = 0; x < ctx.Width; x++)
                {
                    for (int y = 0; y < ctx.Height; y++)
                    {
                        int totalNeighbors = GetNeighborWallCount(bufferGrid, x, y, ctx.Width, ctx.Height);
                        
                        if(totalNeighbors > 4) ctx.CaveGrid[x, y] = false; // False in this case is not air so solid
                        else if(totalNeighbors < 4) ctx.CaveGrid[x, y] = true; // True in this case is air
                    }
                }
                
                yield return null;
            }
        }

        private int GetNeighborWallCount(bool[,] map, int gridX, int gridY, int width, int height)
        {
            int wallCount = 0;
            for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
            {
                for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
                {
                    // Skip the target cell itself
                    if (neighborX == gridX && neighborY == gridY) continue;

                    // Border boundaries count as solid walls to keep caves inside the map
                    if (neighborX < 0 || neighborX >= width || neighborY < 0 || neighborY >= height)
                    {
                        wallCount++;
                    }
                    else if (map[neighborX, neighborY])
                    {
                        wallCount++;
                    }
                }
            }
            return wallCount;
        }
    }
}