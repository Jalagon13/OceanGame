using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class SetSandLayerStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandTile;
        [SerializeField] private int _depthOfSandLayer = 2;
        [SerializeField] private int _depthOfSandFromSurface = 50;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int sandDepth = Mathf.Max(1, _depthOfSandLayer);

            for (int x = 0; x < ctx.Width; x++)
            {
                int minYForSand = ctx.SurfaceHeightValues[x] - _depthOfSandFromSurface;

                for (int y = ctx.SurfaceHeightValues[x]; y >= minYForSand; y--)
                {
                    bool hasTile = ctx.FgGrid[x, y].HasTile;
                    bool hasTileAbove = ctx.FgGrid[x, y + 1].HasTile;

                    if (!hasTile || hasTileAbove)
                    {
                        continue;
                    }

                    for (int depth = 0; depth < sandDepth; depth++)
                    {
                        int fillY = y - depth;
                        ctx.FgGrid[x, fillY] = new TileData(_sandTile.GetId());
                    }

                    y -= sandDepth - 1;
                }

                if (x % ctx.GenColumnsPerFrame == 0) yield return null;
            }
        }

    }
}
