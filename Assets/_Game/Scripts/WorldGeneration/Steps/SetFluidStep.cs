using System;
using System.Collections;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class SetFluidStep : WorldGenStep
    {
        public override IEnumerator Execute(WorldGenContext ctx)
        {
            int width = ctx.Width;
            int height = ctx.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int seaLevel = ctx.SeaLevel;
                    FluidType fluid = FluidType.Nothing;

                    if(y <= seaLevel) fluid = FluidType.Water;
                    else if(y > seaLevel) fluid = FluidType.Air;

                    // Set fluid type for each tile
                    ctx.FluidGrid[x, y] = fluid;
                }
                
                if( x % ctx.GenColumnsPerFrame == 0 ) yield return null;
            }
        }
    }
}