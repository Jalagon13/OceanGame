using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class FillTerrainStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _limestoneTile;
    
        public override IEnumerator Execute(WorldGenContext ctx)
        {
            for (int x = 0; x < ctx.Width; x++)
            {
                int currentSurfaceHeight = ctx.SurfaceHeightValues[x];
            
                for (int y = 0; y < currentSurfaceHeight; y++)
                {
                    if(!ctx.CaveGrid[x, y])
                    {
                        ctx.FgTiles[x, y] = new TileData(_limestoneTile.GetId());
                    }
                }

                if (x % 32 == 0) yield return null;
            }
        }
    }
}