using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class CaveGenStep : WorldGenStep
    {
        [SerializeField, Range(0f, 1f)] private float _botThresh = 0.2f;
        [SerializeField, Range(0f, 1f)] private float _topThresh = 0.8f;
        
        [Header("Base Cave Shapes")]
        [SerializeField] private int _caveSurfaceFadeDistance = 5; // How many tiles deep the widening effect takes to fully blend
        [SerializeField, Range(0f, 1f)] private float _caveNoiseFrequency = 0.03f;
        [SerializeField, Range(0f, 1f)] private float _baseNoiseInfluencePercentage = 0.9f;
        
        [Header("Cheese Cave Shapes")]
        [SerializeField, Range(0f, 1f)] private float _cheeseCaveNoiseFrequency = 0.03f;
        [SerializeField, Range(0f, 1f)] private float _cheeseNoiseThresh = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _cheeseNoiseInfluencePercentage = 0.9f;
        [SerializeField] private int _cheeseStartDepth = 15; // Must be at least 15 blocks under surface
        [SerializeField] private int _cheeseFadeDistance = 10; // Takes 10 blocks to transition to full size

        [Header("Detail Octave")]
        [SerializeField, Range(0f, 1f)] private float _detailFrequency = 0.12f; // Usually much higher than base frequency

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int width = ctx.Width;
            int height = ctx.Height;

            float baseSeedX = ctx.Random.Next(-100000, 100000);
            float baseSeedY = ctx.Random.Next(-100000, 100000);
            float detailSeedX = ctx.Random.Next(-100000, 100000);
            float detailSeedY = ctx.Random.Next(-100000, 100000);
            float baseCheeseSeedX = ctx.Random.Next(-100000, 100000);
            float baseCheeseSeedY = ctx.Random.Next(-100000, 100000);

            for (int x = 0; x < width; x++)
            {
                int surfaceHeight = ctx.SurfaceHeightValues[x];

                for (int y = 0; y < height; y++)
                {
                    // Spaghetti Caves
                    float baseSampleX = (x * _caveNoiseFrequency) + baseSeedX;
                    float baseSampleY = (y * _caveNoiseFrequency) + baseSeedY;
                    float baseNoise = Mathf.PerlinNoise(baseSampleX, baseSampleY);

                    float detailSampleX = (x * _detailFrequency) + detailSeedX;
                    float detailSampleY = (y * _detailFrequency) + detailSeedY;
                    float detailNoise = Mathf.PerlinNoise(detailSampleX, detailSampleY);

                    float finalNoise = (baseNoise * _baseNoiseInfluencePercentage) + (detailNoise * (1 - _baseNoiseInfluencePercentage));

                    if (y <= surfaceHeight)
                    {
                        float distanceToSurface = surfaceHeight - y;

                        if (distanceToSurface <= _caveSurfaceFadeDistance)
                        {
                            // Calculate percentage close to the surface (1.0 at surface, 0.0 at 15 blocks deep)
                            float surfaceProximity = 1f - (distanceToSurface / _caveSurfaceFadeDistance);

                            // Smooth the transition out so it looks more organic
                            surfaceProximity = Mathf.SmoothStep(0f, 1f, surfaceProximity);

                            // Smoothly blend the natural cave noise toward 0.5f (dead center of your open cave thresh)
                            finalNoise = Mathf.Lerp(finalNoise, 0.5f, surfaceProximity);
                        }
                    }
                    else
                    {
                        // If we are strictly ABOVE the surface height, force solid air/no caves
                        finalNoise = 0f;
                    }

                    if (finalNoise >= _botThresh && finalNoise <= _topThresh)
                    {
                        ctx.CaveGrid[x, y] = true;
                    }

                    // Cheese Caves
                    float baseCheeseSampleX = (x * _cheeseCaveNoiseFrequency) + baseCheeseSeedX;
                    float baseCheeseSampleY = (y * _cheeseCaveNoiseFrequency) + baseCheeseSeedY;
                    float cheeseNoise = Mathf.PerlinNoise(baseCheeseSampleX, baseCheeseSampleY);

                    float finalCheeseNoise = (cheeseNoise * _cheeseNoiseInfluencePercentage) + (detailNoise * (1 - _cheeseNoiseInfluencePercentage));

                    // Masking logic to prevent surface craters
                    if (y <= surfaceHeight)
                    {
                        float depthBelowSurface = surfaceHeight - y;

                        // 1. If we are within the absolute buffer zone, completely wipe out cheese noise
                        if (depthBelowSurface < _cheeseStartDepth)
                        {
                            finalCheeseNoise = 0f;
                        }
                        // 2. If we are in the transition zone, smoothly scale the noise up from 0 to its full value
                        else if (depthBelowSurface < (_cheeseStartDepth + _cheeseFadeDistance))
                        {
                            // Calculate how far into the fade zone we are (0.0 to 1.0)
                            float fadeProgress = (depthBelowSurface - _cheeseStartDepth) / _cheeseFadeDistance;
                            fadeProgress = Mathf.SmoothStep(0f, 1f, fadeProgress);

                            // Scale down the noise based on depth (0% at start depth, 100% at full depth)
                            finalCheeseNoise = Mathf.Lerp(0f, finalCheeseNoise, fadeProgress);
                        }
                    }
                    else
                    {
                        // Strictly above ground level
                        finalCheeseNoise = 0f;
                    }

                    // Final evaluation
                    if (finalCheeseNoise > _cheeseNoiseThresh)
                    {
                        ctx.CaveGrid[x, y] = true;
                    }

                }

                if (x % 50 == 0) yield return null;
            }

        }
    }
}
