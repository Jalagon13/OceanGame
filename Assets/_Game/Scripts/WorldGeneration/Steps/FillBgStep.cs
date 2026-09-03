using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class FillBgStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandBgTile;
    
        [Tooltip("How many tiles below the surface line should the background wall start appearing?")]
        [SerializeField] private int _belowSurfaceOffset;
        [SerializeField] private int _undergroundBottomLevelOffset = 2;

        [Header("Deep Core Fill Cutoff")]
        [Tooltip("The minimum height offset from the surface where background walls become completely forced solid.")]
        [SerializeField] private int _minWallPlacementYOffset = 12;
        [SerializeField] private int _maxWallPlacementYOffset = 17;

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            ushort sandBgTileId = _sandBgTile.GetId();
            
            for (int x = 0; x < ctx.Width; x++)
            {
                int surfaceHeight = ctx.SurfaceHeightValues[x];
                int highestSolidTile = GetHighestFlankedSolidTile(x, surfaceHeight, ctx);
                surfaceHeight -= _belowSurfaceOffset;

                int randomOffset = ctx.Random.Next(_minWallPlacementYOffset, _maxWallPlacementYOffset + 1);
                int deepFillCutoffY = surfaceHeight - randomOffset;

                int fillTopY = Mathf.Max(highestSolidTile, deepFillCutoffY);
                int bottomLevel = ctx.UndergroundBottomLevel + ctx.Random.Next(-_undergroundBottomLevelOffset, _undergroundBottomLevelOffset);

                for (int y = bottomLevel; y <= fillTopY; y++)
                {
                    ctx.BgGrid[x, y] = new TileData(sandBgTileId);
                }

                if (x % ctx.GenColumnsPerFrame == 0) yield return null;
            }
        }
        
        private int GetHighestFlankedSolidTile(int x, int surfaceValue, WorldGenContext ctx)
        {
            surfaceValue = Mathf.Clamp(surfaceValue, 0, ctx.Height - 1);

            for (int y = surfaceValue; y >= 0; y--)
            {
                if (ctx.FgGrid[x, y].IsSolid)
                {
                    if (!IsSolidTile(x - 1, y, ctx) || !IsSolidTile(x + 1, y, ctx)) continue;
                
                    return y;
                }
            }
            
            return 0;
        }

        private bool IsSolidTile(int x, int y, WorldGenContext ctx)
        {
            if (x < 0 || x >= ctx.Width || y < 0 || y >= ctx.Height)
                return false;

            return ctx.FgGrid[x, y].IsSolid;
        }
    }
}