using System.Collections;
using UnityEngine;

namespace OceanGame
{
    public class FillBgStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _sandBgTile;
    
        [Tooltip("How many tiles below the surface line should the background wall start appearing?")]
        [SerializeField] private int _belowSurfaceOffset;
        [SerializeField] private int _undergroundBottomLevel = 250;
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
                int highestSolidTile = GetHighestSolidTile(x, surfaceHeight, ctx);
                surfaceHeight -= _belowSurfaceOffset;

                int randomOffset = ctx.Random.Next(_minWallPlacementYOffset, _maxWallPlacementYOffset + 1);
                int deepFillCutoffY = surfaceHeight - randomOffset;

                int fillTopY = Mathf.Max(highestSolidTile, deepFillCutoffY);
                int bottomLevel = _undergroundBottomLevel + ctx.Random.Next(-_undergroundBottomLevelOffset, _undergroundBottomLevelOffset);

                for (int y = bottomLevel; y <= fillTopY; y++)
                {
                    ctx.BgTiles[x, y] = new TileData(sandBgTileId);
                }

                if (x % 32 == 0) yield return null;
            }
        }
        
        private int GetHighestSolidTile(int x, int surfaceValue, WorldGenContext ctx)
        {
            for (int y = surfaceValue; y >= 0; y--)
            {
                if(ctx.FgTiles[x, y].IsSolid)
                {
                    return y;
                }
            }
            
            return 0;
        }
    }
}