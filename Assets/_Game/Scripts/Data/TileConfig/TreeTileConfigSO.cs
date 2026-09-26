using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OceanGame
{
    [CreateAssetMenu(fileName = "New TreeTileConfigSO", menuName = "OceanGame/TileConfig/TreeTileConfigSO")]
    public class TreeTileConfigSO : TileConfigSO
    {
        [Header("Tree Drop Settings")]
        [SerializeField] private ItemSO _woodItem;

        [Header("Segment Visuals (TileBase for each segment)")]
        [SerializeField] private TileBase _baseTile;
        [SerializeField] private TileBase _trunkTile;
        [SerializeField] private TileBase _branchLeftTile;
        [SerializeField] private TileBase _branchRightTile;
        [SerializeField] private TileBase _topTile;

        [Header("Tree Generation Settings")]
        [SerializeField] private int _minHeight = 5;
        [SerializeField] private int _maxHeight = 10;
        [SerializeField, Range(0f, 1f)] private float _branchChance = 0.25f;

        // Selects the proper sprite/tile according to TileData.State.
        public override TileBase GetStateInterpretedTileForRendering(byte state)
        {
            TreeSegmentType segmentType = (TreeSegmentType)state;

            switch (segmentType)
            {
                case TreeSegmentType.Base:
                    return _baseTile != null ? _baseTile : DrawTile;

                case TreeSegmentType.Trunk:
                    return _trunkTile != null ? _trunkTile : DrawTile;

                case TreeSegmentType.BranchLeft:
                    return _branchLeftTile != null ? _branchLeftTile : DrawTile;

                case TreeSegmentType.BranchRight:
                    return _branchRightTile != null ? _branchRightTile : DrawTile;

                case TreeSegmentType.Top:
                    return _topTile != null ? _topTile : DrawTile;

                default:
                    return DrawTile;
            }
        }

        // Terraria-style tree destruction: severs the tree at (startX, startY) and destroys everything connected upwards, leaving lower segments intact.
        public override void OnTileDestroyed(TileGrid grid, int startX, int startY, TileData tileData, bool refreshCurrentBounds = false)
        {
            ushort myTreeId = GetId();

            Stack<Vector2Int> toProcess = new();
            HashSet<Vector2Int> visited = new();

            Vector2Int startPos = new(startX, startY);
            toProcess.Push(startPos);
            visited.Add(startPos);

            while (toProcess.Count > 0)
            {
                Vector2Int curr = toProcess.Pop();
                TileData currData = grid.GetTileData(curr.x, curr.y);

                // Wipe this tile from the grid
                grid.SetTileDataDirect(curr.x, curr.y, TileData.Air);
                grid.ClearDamage(curr.x, curr.y);
                
                SpawnLootAt(curr, currData);

                if (currData.State == (byte)TreeSegmentType.BranchLeft || currData.State == (byte)TreeSegmentType.BranchRight)
                {
                    continue; // Don't check candidates for a branch, just move to the next tile
                }

                // Scan for dependent segments:
                // check UP and SIDES (for branches). NEVER check (curr.x, curr.y - 1).
                Vector2Int[] candidates = new Vector2Int[]
                {
                    new(curr.x, curr.y + 1), // Directly above
                    new(curr.x - 1, curr.y), // Left branch tile
                    new(curr.x + 1, curr.y), // Right branch tile
                };

                foreach (var next in candidates)
                {
                    if (!grid.IsInBounds(next.x, next.y) || visited.Contains(next)) continue;
                    TileData neighbor = grid.GetTileData(next.x, next.y);

                    if (neighbor.TileId != myTreeId) continue;

                    // Only accept directly above (any trunk/top tile), OR side tiles if they are actually branches:
                    bool isValidNeighbor = false;

                    if (next.x == curr.x && next.y == curr.y + 1)
                    {
                        isValidNeighbor = true; // Directly above on the trunk
                    }
                    else if (next.x == curr.x - 1 && neighbor.State == (byte)TreeSegmentType.BranchLeft)
                    {
                        isValidNeighbor = true; // Left branch
                    }
                    else if (next.x == curr.x + 1 && neighbor.State == (byte)TreeSegmentType.BranchRight)
                    {
                        isValidNeighbor = true; // Right branch
                    }

                    if (isValidNeighbor)
                    {
                        visited.Add(next);
                        toProcess.Push(next);
                    }
                }
            }
        }
        
        private void SpawnLootAt(Vector2Int curr, TileData currentData) // WIP, will get to this when i create a dedicated loot system later
        {
            Vector2 tileCenterPos = new Vector2(curr.x + 0.5f, curr.y + 0.5f);

            if (_woodItem != null)
            {
                GameManager.Instance.SpawnItem(_woodItem, 1, tileCenterPos);
            }
        }

        // Attempts to grow a tree at (groundX, groundY). groundY is the soil/ground block the tree stands on.
        public bool TryGrowTree(TileGrid grid, int groundX, int groundY, System.Random random = null, bool refreshBounds = true)
        {
            ushort treeId = GetId();
            int height = UnityEngine.Random.Range(_minHeight, _maxHeight + 1); // For testing
            // int height = Mathf.RoundToInt(random.Next(_minHeight, _maxHeight + 1)); // For generation
            Debug.Log($"Height chosen: {height}");
            
            // Clearance Check: Ensure there is empty air above the ground
            for (int y = 1; y <= height; y++)
            {
                int checkY = groundY + y;
                if (!grid.IsInBounds(groundX, checkY)) return false;
                
                TileData tile = grid.GetTileData(groundX, checkY);
                if (tile.HasTile)
                {
                    Debug.Log($"cant grow tree bc Obstructed by another block");
                    return false; // Obstructed by another block
                }
            }

            // Base Segment (sits directly on top of the ground block)
            grid.SetTileDataDirect(groundX, groundY + 1, new TileData(treeId, state: (byte)TreeSegmentType.Base));

            // Trunk & Branch Segments
            for (int y = 2; y < height; y++)
            {
                TreeSegmentType segment = TreeSegmentType.Trunk;
                
                // Roll for random branches
                if (UnityEngine.Random.value < _branchChance) // Use the random parameter later but for now use this for testing
                {
                    TreeSegmentType branchSegment = UnityEngine.Random.value < 0.5f ? TreeSegmentType.BranchLeft : TreeSegmentType.BranchRight;
                    int offset = branchSegment == TreeSegmentType.BranchLeft ? -1 : 1;

                    grid.SetTileDataDirect(groundX + offset, groundY + y, new TileData(treeId, state: (byte)branchSegment));
                    Debug.Log($"Gen a {branchSegment} branch");
                }

                grid.SetTileDataDirect(groundX, groundY + y, new TileData(treeId, state: (byte)segment));
                Debug.Log($"Gen a trunk");
            }

            // Top Segment (holds the canopy)
            grid.SetTileDataDirect(groundX, groundY + height, new TileData(treeId, state: (byte)TreeSegmentType.Top));
            Debug.Log($"top seg added");

            // Refresh camera bounds if requested so it appears on screen immediately
            if (refreshBounds && PlayerCamera.Instance.PositionExistsInBounds(groundX, groundY))
            {
                PlayerCamera.Instance.InvokeCurrentBoundsRefresh();
            }
            
            return true;
        }


    }
}
