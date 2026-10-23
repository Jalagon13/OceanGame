using System;
using System.Collections;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public class CaveGenStep : WorldGenStep
    {
        [SerializeField, Min(0.01f)] private float _scale = 50f;
        [SerializeField, Min(0f)] private float _frequency = 1f;
        [SerializeField, Range(0f, 1f)] private float _caveThreshold = 0.6f;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            float offsetX = (float)ctx.Random.NextDouble() * 10000f;
            float offsetY = (float)ctx.Random.NextDouble() * 10000f;
            int columnsPerFrame = Mathf.Max(1, ctx.GenColumnsPerFrame);

            for (int x = 0; x < ctx.Width; x++)
            {
                for (int y = 0; y < ctx.SurfaceHeightMap[x] + 1; y++)
                {
                    float noise = Mathf.PerlinNoise((x * _frequency / _scale) + offsetX, (y * _frequency / _scale) + offsetY);

                    if (noise > _caveThreshold)
                    {
                        ctx.FgGrid[x, y] = TileData.Air;
                    }
                }

                if ((x + 1) % columnsPerFrame == 0) yield return null;
            }
        }
    }
}
