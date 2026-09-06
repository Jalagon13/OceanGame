using System;
using UnityEngine;

namespace OceanGame
{
    public class FluidGrid
    {
        private readonly FluidType[] _tiles;
        private readonly int _width;
        private readonly int _height;

        public FluidGrid(int width, int height)
        {
            _width = width;
            _height = height;
        
            _tiles = new FluidType[_width * _height];
        }

        public FluidType GetFluidType(int x, int y)
        {
            if (!IsInBounds(x, y))
            {
                return FluidType.Nothing;
            }

            return _tiles[y * _width + x];
        }

        public void SetFluidData(int x, int y, FluidType fluidType, bool refreshCurrentBounds = false)
        {
            if(!IsInBounds(x, y)) return;
            
            _tiles[y * _width + x] = fluidType;

            if (refreshCurrentBounds && PlayerCamera.Instance.PositionExistsInBounds(x, y))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }

        public bool IsInBounds(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
            {
                return false;
            }

            return true;
        }
    }
    
    public enum FluidType
    {
        Water,
        Air,
        Nothing
    }
}