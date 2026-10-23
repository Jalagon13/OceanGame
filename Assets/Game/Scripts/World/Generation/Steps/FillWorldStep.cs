using System;
using System.Collections;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public class FillWorldStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _grassTile;
        [SerializeField] private TileConfigSO _dirtTile;
        [SerializeField] private TileConfigSO _stoneTile;
        [SerializeField, Min(1)] private int _dirtDepth = 6;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int dirtDepth = Mathf.Max(1, _dirtDepth);
            int columnsPerFrame = Mathf.Max(1, ctx.GenColumnsPerFrame);

            for (int x = 0; x < ctx.Width; x++)
            {
                int surfaceHeight = ctx.SurfaceHeightMap[x];

                for (int y = 0; y <= surfaceHeight; y++)
                {
                    TileConfigSO tile = y > surfaceHeight - dirtDepth ? _dirtTile : _stoneTile;
                    if (y == surfaceHeight) tile = _grassTile;

                    ctx.FgGrid[x, y] = new TileData(tile.GetId());
                }

                if ((x + 1) % columnsPerFrame == 0) yield return null;
            }
        }

    }
}
