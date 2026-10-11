using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class OceanFloraGenStep : WorldGenStep
    {
        [SerializeField] private TreeTileConfigSO _kelp;
        [SerializeField] private int _minKelpSpace = 1;
        [SerializeField] private int _maxKelpSpace = 4;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                // Start just below sea level, and keep y + 1 inside the grid.
                int startY = Mathf.Min(ctx.SeaLevel - 1, ctx.Height - 2);

                // Search downward for the first exposed, top-facing floor tile.
                for (int y = startY; y >= 0; y--)
                {
                    bool hasTile = ctx.FgGrid[x, y].HasTile;
                    bool hasTileAbove = ctx.FgGrid[x, y + 1].HasTile;

                    if (!hasTile || hasTileAbove)
                    {
                        continue;
                    }

                    // SeaLevel is the highest tile kelp is allowed to occupy.
                    int maxGrowthY = Mathf.Min(ctx.SeaLevel, ctx.Height - 1);

                    if (_kelp.TryGrowTree(ctx, x, y, maxGrowthY))
                    {
                        int space = ctx.Random.Next(_minKelpSpace, _maxKelpSpace + 1);
                        x += space;
                    }

                    // This is the first exposed floor tile in this column.
                    break;
                }

                if (x % ctx.GenColumnsPerFrame == 0) yield return null;
            }
        }
        
        
    }
}
