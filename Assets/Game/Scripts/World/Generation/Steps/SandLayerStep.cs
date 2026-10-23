using System;
using System.Collections;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public class SandLayerStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandTile;
        [SerializeField] private int _depthOfSandLayer = 2;
        [SerializeField] private int _depthOfSandFromSurface = 50;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int sandDepth = Mathf.Max(1, _depthOfSandLayer);

            int startY = ctx.Height - 2;
            int minYForSand = Mathf.Max(0, ctx.SeaLevel - _depthOfSandFromSurface);

            for (int x = 0; x < ctx.Width; x++)
            {
                // Start from top of world down to seabed depth limit
                for (int y = startY; y >= minYForSand; y--)
                {
                    bool hasTile = ctx.FgGrid[x, y].HasTile;
                    bool hasTileAbove = ctx.FgGrid[x, y + 1].HasTile;

                    // Only place sand on top-facing exposed tiles
                    if (!hasTile || hasTileAbove)
                    {
                        continue;
                    }

                    for (int depth = 0; depth < sandDepth; depth++)
                    {
                        int fillY = y - depth;
                        if (fillY >= 0)
                        {
                            ctx.FgGrid[x, fillY] = new TileData(_sandTile.GetId());
                        }
                    }

                    y -= sandDepth - 1;
                }

                if (x % ctx.GenColumnsPerFrame == 0) yield return null;
            }
        }

    }
}
