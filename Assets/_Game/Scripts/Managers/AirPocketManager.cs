using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OceanGame
{
    public class AirPocketManager : MonoBehaviour
    {
        public static AirPocketManager Instance { get; private set; }
        
        private List<AirPocket> _airPockets = new();
        private Dictionary<Vector2Int, HashSet<AirPocket>> _tileToPocketsMap = new();
        
        private readonly Vector2Int[] _directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        private void Awake() 
        {
            Instance = this;    
        }
        
        private void Start() 
        {
            WorldManager.Instance.OnWorldReady += SubScribeToEvents;
        }

        private void OnDestroy()
        {
            WorldManager.Instance.OnWorldReady -= SubScribeToEvents;
            
            if(WorldManager.Instance.IsWorldReady)
            {
                WorldManager.Instance.FgGrid.OnTileDestroyed -= UpdateAirPockets;
                WorldManager.Instance.FgGrid.OnTilePlaced -= UpdateAirPockets;
            }
        }

        private void SubScribeToEvents()
        {
            WorldManager.Instance.FgGrid.OnTileDestroyed += UpdateAirPockets;
            WorldManager.Instance.FgGrid.OnTilePlaced += UpdateAirPockets;
        }

        private void UpdateAirPockets(Vector2Int posToCheck)
        {
            if (_tileToPocketsMap.TryGetValue(posToCheck, out var pocketSet))
            {
                var pocketsToNotify = pocketSet.ToArray();
                foreach (var pocket in pocketsToNotify)
                {
                    pocket.UpdatePocket(posToCheck);
                }
            }
        }

        public void RegisterPocket(AirPocket pocket, IEnumerable<Vector2Int> spaces, IEnumerable<Vector2Int> walls)
        {
            foreach (var pos in spaces) RegisterTile(pos, pocket);
            foreach (var pos in walls) RegisterTile(pos, pocket);
        }

        public void UnregisterPocket(AirPocket pocket, IEnumerable<Vector2Int> spaces, IEnumerable<Vector2Int> walls)
        {
            foreach (var pos in spaces) UnregisterTile(pos, pocket);
            foreach (var pos in walls) UnregisterTile(pos, pocket);
        }

        public void RegisterTile(Vector2Int pos, AirPocket pocket)
        {
            if (!_tileToPocketsMap.TryGetValue(pos, out var set))
            {
                set = new HashSet<AirPocket>();
                _tileToPocketsMap[pos] = set;
            }
            
            set.Add(pocket);
        }

        public void UnregisterTile(Vector2Int pos, AirPocket pocket)
        {
            if (_tileToPocketsMap.TryGetValue(pos, out var set))
            {
                set.Remove(pocket);
                
                if (set.Count == 0)
                {
                    _tileToPocketsMap.Remove(pos);
                }
            }
        }

        public void TryToDrain(int posX, int posY, int drainLimit)
        {
            // Loop through the existing air pockets and see if any of them contain this drain position
            Vector2Int pos = new(posX, posY);
            
            if(_airPockets.Count > 0)
            {
                foreach(var pocket in _airPockets)
                {
                    if(pocket.HasDrainedPos(pos))
                    {
                        Debug.Log($"Already in pocket!");
                        return;
                    }
                }
            }
            
            // No air pockets have this position, try to drain
            if (ExecuteDrain(drainLimit, pos, out AirPocket airPocket))
            {
                _airPockets.Add(airPocket);
            }
        }

        private bool ExecuteDrain(int drainLimit, Vector2Int startPos, out AirPocket pocket)
        {
            // Run BFS search here 
            HashSet<Vector2Int> visited = new();
            HashSet<Vector2Int> walls = new();
            HashSet<Vector2Int> spaces = new();
            Queue<Vector2Int> queue = new();

            spaces.Add(startPos);
            visited.Add(startPos);
            queue.Enqueue(startPos);
            
            int currentVisitedSpaces = 1; // Start at one bc startPos
            
            while(queue.Count > 0)
            {
                var curr = queue.Dequeue();
                
                foreach (Vector2Int dir in _directions)
                {
                    Vector2Int n = curr + dir;
                    
                    if(visited.Contains(n)) continue;

                    if (!WorldManager.Instance.FgGrid.IsInBounds(n.x, n.y))
                    {
                        walls.Add(n); // Treat world edge as a boundary
                        visited.Add(n);
                        continue;
                    }

                    // Check if the neighbor is a space or solid
                    var td = WorldManager.Instance.FgGrid.GetTileData(n.x, n.y);
                    if (td.HasTile && td.IsSolid)
                    {
                        // Found a wall
                        walls.Add(n);
                        visited.Add(n);
                    }
                    else
                    {
                        // If empty space check if we are out of spaces to check
                        currentVisitedSpaces++;

                        if (currentVisitedSpaces > drainLimit)
                        {
                            pocket = null;
                            Debug.Log($"Exceded drain limit");
                            return false;
                        }

                        queue.Enqueue(n);
                        spaces.Add(n);
                        visited.Add(n);
                    }
                }
            }
            
            // Create air pocket
            pocket = new AirPocket(spaces, walls, drainLimit);
            return true;
        }

        public bool IsTileInAnotherAirPocket(AirPocket currentPocket, Vector2Int pos)
        {
            foreach (var pocket in _airPockets)
            {
                // Skip comparing against the pocket that is currently collapsing
                if (pocket != currentPocket && pocket.HasDrainedPos(pos))
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveAirPocket(AirPocket pocket)
        {
            _airPockets.Remove(pocket);
        }
    }
}
