using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    public class DrainedCompartment
    {
        public HashSet<Vector2Int> AirTiles { get; }
        public HashSet<Vector2Int> PerimeterWalls { get; }
        public HashSet<Vector2Int> DrainPositions { get; }
        public int TotalCapacity { get; }

        public DrainedCompartment(HashSet<Vector2Int> airTiles, HashSet<Vector2Int> walls, HashSet<Vector2Int> drains, int capacity)
        {
            AirTiles = airTiles;
            PerimeterWalls = walls;
            DrainPositions = drains;
            TotalCapacity = capacity;
        }
    }

    public class DrainManager : MonoBehaviour
    {
        public static DrainManager Instance { get; private set; }

        private readonly Dictionary<Vector2Int, int> _activeDrains = new();
        private readonly HashSet<DrainedCompartment> _activeCompartments = new();
        private readonly Dictionary<Vector2Int, HashSet<DrainedCompartment>> _tileToCompartments = new();

        private readonly Vector2Int[] _directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (WorldManager.Instance != null)
            {
                if (WorldManager.Instance.IsWorldReady)
                {
                    SubscribeToEvents();
                }
                else
                {
                    WorldManager.Instance.OnWorldReady += SubscribeToEvents;
                }
            }
        }

        private void OnDestroy()
        {
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnWorldReady -= SubscribeToEvents;

                if (WorldManager.Instance.FgGrid != null)
                {
                    WorldManager.Instance.FgGrid.OnTileDestroyed -= HandleTileDestroyed;
                    WorldManager.Instance.FgGrid.OnTilePlaced -= HandleTilePlaced;
                }
            }
        }

        private void SubscribeToEvents()
        {
            if (WorldManager.Instance != null && WorldManager.Instance.FgGrid != null)
            {
                WorldManager.Instance.FgGrid.OnTileDestroyed -= HandleTileDestroyed;
                WorldManager.Instance.FgGrid.OnTilePlaced -= HandleTilePlaced;

                WorldManager.Instance.FgGrid.OnTileDestroyed += HandleTileDestroyed;
                WorldManager.Instance.FgGrid.OnTilePlaced += HandleTilePlaced;
            }
        }

        public void InteractWithDrain(int posX, int posY, int drainLimit)
        {
            Vector2Int pos = new(posX, posY);

            // Register or update drain
            _activeDrains[pos] = drainLimit;

            // Re-evaluate the compartment containing this drain
            ReevaluateAt(pos);
        }

        public void UnregisterDrain(Vector2Int pos)
        {
            if (_activeDrains.Remove(pos))
            {
                ReevaluateAt(pos);
            }
        }

        private void HandleTilePlaced(Vector2Int pos)
        {
            // If tile was placed inside or on the boundary of an active compartment
            if (_tileToCompartments.ContainsKey(pos) || HasAdjacentCompartmentTile(pos))
            {
                ReevaluateAt(pos);
            }
        }

        private void HandleTileDestroyed(Vector2Int pos)
        {
            // If an active drain was destroyed, remove it
            bool wasDrain = _activeDrains.Remove(pos);

            // Check if this destroyed tile was part of or adjacent to any active compartments
            if (wasDrain || _tileToCompartments.ContainsKey(pos) || HasAdjacentCompartmentTile(pos))
            {
                ReevaluateAt(pos);
            }
        }

        private bool HasAdjacentCompartmentTile(Vector2Int pos)
        {
            foreach (var dir in _directions)
            {
                if (_tileToCompartments.ContainsKey(pos + dir))
                {
                    return true;
                }
            }
            
            return false;
        }

        public void ReevaluateAt(Vector2Int triggerPos)
        {
            // Gather all compartments affected by this trigger
            HashSet<DrainedCompartment> affected = new();

            if (_tileToCompartments.TryGetValue(triggerPos, out var directComp))
            {
                affected.UnionWith(directComp);
            }

            foreach (var dir in _directions)
            {
                if (_tileToCompartments.TryGetValue(triggerPos + dir, out var neighborComp))
                {
                    affected.UnionWith(neighborComp);
                }
            }

            // Collect all drains that belong to the affected compartments
            HashSet<Vector2Int> drainsToProcess = new();
            
            foreach (var comp in affected)
            {
                foreach (var drainPos in comp.DrainPositions)
                {
                    if (_activeDrains.ContainsKey(drainPos))
                    {
                        drainsToProcess.Add(drainPos);
                    }
                }
            }

            // Also check if triggerPos itself is a registered drain
            if (_activeDrains.ContainsKey(triggerPos))
            {
                drainsToProcess.Add(triggerPos);
            }

            // Collapse all affected compartments before re-evaluating
            foreach (var comp in affected)
            {
                CollapseCompartment(comp);
            }

            // Re-evaluate from each collected drain
            HashSet<Vector2Int> processedDrains = new();

            foreach (var drainPos in drainsToProcess)
            {
                if (processedDrains.Contains(drainPos)) continue;

                if (TryEvaluateCompartment(drainPos, out DrainedCompartment newComp))
                {
                    ApplyCompartment(newComp);
                    processedDrains.UnionWith(newComp.DrainPositions);
                }
            }

            // Refresh camera / world renderer bounds
            if (PlayerCamera.Instance != null)
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
        }

        private bool TryEvaluateCompartment(Vector2Int startDrainPos, out DrainedCompartment compartment)
        {
            compartment = null;

            if (WorldManager.Instance == null || WorldManager.Instance.FgGrid == null || WorldManager.Instance.FluidGrid == null)
            {
                return false;
            }

            // If start position is inside an airtight barrier, it cannot drain
            if (IsAirtightBarrier(startDrainPos.x, startDrainPos.y))
            {
                return false;
            }

            // Compute total max possible capacity of all active drains to bound the search
            int maxPossibleWorldCap = 0;
            
            foreach (var kvp in _activeDrains)
            {
                maxPossibleWorldCap += kvp.Value;
            }
            
            int searchLimit = Mathf.Max(maxPossibleWorldCap + 20, 500);

            HashSet<Vector2Int> spaces = new();
            HashSet<Vector2Int> walls = new();
            HashSet<Vector2Int> discoveredDrains = new();
            Queue<Vector2Int> queue = new();

            spaces.Add(startDrainPos);
            queue.Enqueue(startDrainPos);

            if (_activeDrains.TryGetValue(startDrainPos, out int startCap))
            {
                discoveredDrains.Add(startDrainPos);
            }

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();

                foreach (var dir in _directions)
                {
                    Vector2Int n = curr + dir;

                    if (spaces.Contains(n) || walls.Contains(n)) continue;

                    if (!WorldManager.Instance.FgGrid.IsInBounds(n.x, n.y))
                    {
                        // World boundaries act as solid perimeter walls
                        walls.Add(n);
                        continue;
                    }

                    if (IsAirtightBarrier(n.x, n.y))
                    {
                        walls.Add(n);
                    }
                    else
                    {
                        // Open tile
                        if (_activeDrains.TryGetValue(n, out int otherCap))
                        {
                            discoveredDrains.Add(n);
                        }

                        spaces.Add(n);
                        queue.Enqueue(n);

                        // If spaces exceed hard search limit, room is breached/unbounded to ocean
                        if (spaces.Count > searchLimit)
                        {
                            return false;
                        }
                    }
                }
            }

            // Calculate pooled capacity of all discovered drains in this room
            int pooledCapacity = 0;
            
            foreach (var d in discoveredDrains)
            {
                if (_activeDrains.TryGetValue(d, out int cap))
                {
                    pooledCapacity += cap;
                }
            }

            // Must have at least 1 drain and spaces must fit within pooled capacity
            if (discoveredDrains.Count == 0 || spaces.Count > pooledCapacity)
            {
                return false;
            }

            compartment = new DrainedCompartment(spaces, walls, discoveredDrains, pooledCapacity);
            
            return true;
        }

        private void ApplyCompartment(DrainedCompartment comp)
        {
            _activeCompartments.Add(comp);

            // Register spatial mapping for air spaces
            foreach (var pos in comp.AirTiles)
            {
                RegisterTile(pos, comp);
                WorldManager.Instance.FluidGrid.SetFluidData(pos.x, pos.y, FluidType.Air);
            }

            // Register spatial mapping for perimeter walls
            foreach (var pos in comp.PerimeterWalls)
            {
                RegisterTile(pos, comp);
            }
        }

        private void CollapseCompartment(DrainedCompartment comp)
        {
            _activeCompartments.Remove(comp);

            // Unregister spatial mapping
            foreach (var pos in comp.AirTiles)
            {
                UnregisterTile(pos, comp);

                // Only restore to water if no other active compartment claims this tile
                if (!IsTileClaimedByAnotherCompartment(pos, comp))
                {
                    FluidType natural = (pos.y <= WorldManager.Instance.SeaLevel) ? FluidType.Water : FluidType.Air;
                    WorldManager.Instance.FluidGrid.SetFluidData(pos.x, pos.y, natural);
                }
            }

            foreach (var pos in comp.PerimeterWalls)
            {
                UnregisterTile(pos, comp);
            }
        }

        private bool IsTileClaimedByAnotherCompartment(Vector2Int pos, DrainedCompartment excludeComp)
        {
            if (_tileToCompartments.TryGetValue(pos, out var set))
            {
                foreach (var c in set)
                {
                    if (c != excludeComp && c.AirTiles.Contains(pos))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void RegisterTile(Vector2Int pos, DrainedCompartment comp)
        {
            if (!_tileToCompartments.TryGetValue(pos, out var set))
            {
                set = new HashSet<DrainedCompartment>();
                _tileToCompartments[pos] = set;
            }
            set.Add(comp);
        }

        private void UnregisterTile(Vector2Int pos, DrainedCompartment comp)
        {
            if (_tileToCompartments.TryGetValue(pos, out var set))
            {
                set.Remove(comp);
                if (set.Count == 0)
                {
                    _tileToCompartments.Remove(pos);
                }
            }
        }

        public bool IsAirtightBarrier(int x, int y)
        {
            if (!WorldManager.Instance.FgGrid.IsInBounds(x, y))
            {
                return true;
            }

            TileData td = WorldManager.Instance.FgGrid.GetTileData(x, y);
            
            if (!td.HasTile) return false;

            // Solid blocks act as airtight walls
            if (td.IsSolid) return true;

            // Doors in ANY state (open or closed) act as airtight perimeter walls
            if (td.TileConfig is DoorTileConfigSO || (td.TileConfig != null && td.TileConfig.InteractBehavior is DoorIB))
            {
                return true;
            }

            return false;
        }
    }
}
