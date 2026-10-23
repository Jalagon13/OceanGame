using System;
using System.Collections;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public class SurfaceHeightMapStep : WorldGenStep
    {
        [SerializeField, Min(0f)] private float _amplitude = 20f;
        [SerializeField, Min(0f)] private float _frequency = 0.01f;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            float offsetX = (float)ctx.Random.NextDouble() * 10000f;
            float offsetY = (float)ctx.Random.NextDouble() * 10000f;
            int columnsPerFrame = Mathf.Max(1, ctx.GenColumnsPerFrame);

            for (int x = 0; x < ctx.Width; x++)
            {
                float noise = Mathf.PerlinNoise((x * _frequency) + offsetX, offsetY);
                int surfaceHeight = Mathf.RoundToInt(ctx.RoughSurfaceHeight + noise * _amplitude);
                ctx.SurfaceHeightMap[x] = Mathf.Clamp(surfaceHeight, 0, ctx.Height - 1);

                if ((x + 1) % columnsPerFrame == 0) yield return null;
            }
        }
    }
}
