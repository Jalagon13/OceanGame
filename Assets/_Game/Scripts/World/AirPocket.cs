using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class AirPocket
    {
        private readonly HashSet<Vector2Int> _spaceTiles;
        private readonly HashSet<Vector2Int> _wallTiles;
        private readonly int _drainLimit;
        private readonly Vector2Int[] _directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        public AirPocket(HashSet<Vector2Int> spaces, HashSet<Vector2Int> walls, int drainLimit)
        {
            _spaceTiles = spaces;
            _wallTiles = walls;
            _drainLimit = drainLimit;
            int counter = 0;

            // Set fluid data
            foreach (Vector2Int pos in _spaceTiles)
            {
                counter++;
                WorldManager.Instance.FluidGrid.SetFluidData(pos.x, pos.y, FluidType.Air, counter == _spaceTiles.Count); // Only refresh it once at the last time you call it
            }

            // Register tiles in spatial lookup dictionary
            AirPocketManager.Instance.RegisterPocket(this, _spaceTiles, _wallTiles);
        }

        public void UpdatePocket(Vector2Int pos)
        {
            if (_wallTiles.Contains(pos))
            {
                HandleWallTileChanged(pos);
            }
            else if (_spaceTiles.Contains(pos))
            {
                HandleSpaceTileChanged(pos);
            }
        }

        private void HandleWallTileChanged(Vector2Int pos)
        {
            TileData td = WorldManager.Instance.FgGrid.GetTileData(pos.x, pos.y);

            // Wall block was broken or opened
            if (!td.HasTile || !td.IsSolid)
            {
                // 1. Check for perimeter water leak
                if (IsPerimeterBreach(pos))
                {
                    Collapse();
                    return;
                }

                // 2. Pillar destroyed! Check if new space exceeds pump capacity limit
                if (_spaceTiles.Count + 1 > _drainLimit)
                {
                    Debug.Log("Room expanded beyond drain limit! Collapsing...");
                    Collapse();
                    return;
                }

                // 3. Fits within capacity! Convert pillar into room air space
                _wallTiles.Remove(pos);
                _spaceTiles.Add(pos);
                WorldManager.Instance.FluidGrid.SetFluidData(pos.x, pos.y, FluidType.Air, true);

                // 4. Discover and register newly exposed perimeter wall tiles behind pos
                foreach (Vector2Int dir in _directions)
                {
                    Vector2Int n = pos + dir;
                    if (!_spaceTiles.Contains(n) && !_wallTiles.Contains(n))
                    {
                        // If neighbor is out of bounds or has a solid tile, add it as a new perimeter wall tile
                        if (!WorldManager.Instance.FgGrid.IsInBounds(n.x, n.y) || WorldManager.Instance.FgGrid.GetTileData(n.x, n.y).HasTile)
                        {
                            _wallTiles.Add(n);
                            AirPocketManager.Instance.RegisterTile(n, this);
                        }
                    }
                }
            }
        }

        private void HandleSpaceTileChanged(Vector2Int pos)
        {
            TileData td = WorldManager.Instance.FgGrid.GetTileData(pos.x, pos.y);

            // Player placed a solid block inside the air pocket room
            if (td.HasTile && td.IsSolid)
            {
                // Room shrinks by 1 space, block becomes a wall
                _spaceTiles.Remove(pos);
                _wallTiles.Add(pos);
                WorldManager.Instance.FluidGrid.SetFluidData(pos.x, pos.y, FluidType.Water, true);
            }
        }

        private bool IsPerimeterBreach(Vector2Int pos)
        {
            bool touchesAir = false;
            bool touchesWater = false;

            foreach (Vector2Int dir in _directions)
            {
                Vector2Int n = pos + dir;
                FluidType fluid = WorldManager.Instance.FluidGrid.GetFluidType(n.x, n.y);
                bool hasTile = WorldManager.Instance.FgGrid.GetTileData(n.x, n.y).HasTile;

                if (fluid == FluidType.Air) touchesAir = true;
                if (fluid == FluidType.Water && !hasTile) touchesWater = true;
            }

            return touchesAir && touchesWater;
        }

        public void Collapse()
        {
            int counter = 0;

            foreach (Vector2Int pos in _spaceTiles)
            {
                counter++;
                bool isLastTile = counter == _spaceTiles.Count;

                // Only turn back to Water if NO OTHER active air pocket is keeping it as Air!
                if (!AirPocketManager.Instance.IsTileInAnotherAirPocket(this, pos))
                {
                    WorldManager.Instance.FluidGrid.SetFluidData(pos.x, pos.y, FluidType.Water, isLastTile);
                }
            }

            // Unregister tiles from spatial lookup dictionary and remove pocket
            AirPocketManager.Instance.UnregisterPocket(this, _spaceTiles, _wallTiles);
            AirPocketManager.Instance.RemoveAirPocket(this);
        }

        public bool HasDrainedPos(Vector2Int pos)
        {
            return _spaceTiles.Contains(pos);
        }
    }
}