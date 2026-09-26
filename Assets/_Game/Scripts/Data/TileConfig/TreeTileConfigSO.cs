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
        [field: SerializeField] public int MinHeight { get; private set; } = 5;
        [field: SerializeField] public int MaxHeight { get; private set; } = 10;
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
        public bool TryGrowTree(WorldGenContext ctx, int groundX, int groundY)
        {
            ushort treeId = GetId();
            int height = ctx.Random.Next(MinHeight, MaxHeight + 1);

            // Clearance Check
            for (int y = 1; y <= height; y++)
            {
                int checkY = groundY + y;
                if (checkY >= ctx.Height || ctx.FgGrid[groundX, checkY].HasTile) return false;
            }

            // Gen Base
            ctx.FgGrid[groundX, groundY + 1] = new TileData(treeId, state: (byte)TreeSegmentType.Base);

            // Gen Trunk and Branches
            for (int y = 2; y < height; y++)
            {
                TreeSegmentType segment = TreeSegmentType.Trunk;

                if (ctx.Random.NextDouble() < _branchChance)
                {
                    TreeSegmentType branchType = ctx.Random.NextDouble() < 0.5
                        ? TreeSegmentType.BranchLeft
                        : TreeSegmentType.BranchRight;
                    int offset = branchType == TreeSegmentType.BranchLeft ? -1 : 1;
                    int branchX = groundX + offset;

                    // Make sure branch space is clear and within bounds
                    if (branchX >= 0 && branchX < ctx.Width && !ctx.FgGrid[branchX, groundY + y].HasTile)
                    {
                        ctx.FgGrid[branchX, groundY + y] = new TileData(treeId, state: (byte)branchType);
                    }
                }

                ctx.FgGrid[groundX, groundY + y] = new TileData(treeId, state: (byte)segment);
            }

            // Top
            ctx.FgGrid[groundX, groundY + height] = new TileData(treeId, state: (byte)TreeSegmentType.Top);

            return true;
        }


    }
}
