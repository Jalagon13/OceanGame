using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OceanGame
{
    [Serializable]
    public class OreGenStep : WorldGenStep
    {
        [SerializeField] private TileConfigSO _ironOreTile;
        [SerializeField] private int _chunkSize = 20;

        // Exposed configuration for easier balancing
        [SerializeField] private int _veinsPerChunk = 6;
        [SerializeField] private int _veinMinDistance = 5;
        [SerializeField] private int _maxAttemptsPerVein = 10;
        [SerializeField] private int _belowSurfaceHeightOffset = 4;

        [Header("Vein Size Configurations")]
        [SerializeField] private int _minVeinSize = 8;
        [SerializeField] private int _maxVeinSize = 16;

        private readonly Vector2Int[] _cardinalDirections = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            // Safeguard bounds checking to prevent index out of bounds at world edges
            for (int chunkX = 0; chunkX < ctx.Width; chunkX += _chunkSize)
            {
                for (int chunkY = 0; chunkY < ctx.SeaLevel; chunkY += _chunkSize)
                {
                    HashSet<Vector2Int> foundVeinPositions = new();

                    // Outer loop tracks exactly how many veins we want to place
                    for (int i = 0; i < _veinsPerChunk; i++)
                    {
                        int currentAttempt = 0;
                        bool veinPlaced = false;

                        // Inner loop tracks attempts for the CURRENT vein
                        while (currentAttempt < _maxAttemptsPerVein && !veinPlaced)
                        {
                            // Calculate local chunk bounds, clamped to world size
                            int maxX = Mathf.Min(chunkX + _chunkSize, ctx.Width);
                            int maxY = Mathf.Min(chunkY + _chunkSize, ctx.SeaLevel);

                            int randPosX = ctx.Random.Next(chunkX, maxX);
                            int randPosY = ctx.Random.Next(chunkY, maxY);
                            Vector2Int randPos = new(randPosX, randPosY);

                            if (IsValidVeinPosition(ctx, randPos, foundVeinPositions))
                            {
                                foundVeinPositions.Add(randPos);
                                veinPlaced = true;
                            }
                            else
                            {
                                currentAttempt++;
                            }
                        }
                    }

                    // Grow the veins out from the found seed positions
                    foreach (Vector2Int seedPos in foundVeinPositions)
                    {
                        GrowOreVein(ctx, seedPos);
                    }
                }
                // Yielding per column of chunks prevents frame stutter during generation
                yield return null;
            }
        }

        private bool IsValidVeinPosition(WorldGenContext ctx, Vector2Int pos, HashSet<Vector2Int> existingPositions)
        {
            if(pos.y >= ctx.SurfaceHeightValues[pos.x] - _belowSurfaceHeightOffset) return false; // Makes it spawn at least some distance away from the surface
            if(ctx.FgGrid[pos.x, pos.y].IsAir) return false;
        
            foreach (var existingPos in existingPositions)
            {
                // Using SqrMagnitude is significantly faster than Vector2Int.Distance 
                // because it avoids calculating a square root.
                float sqrDistance = (existingPos - pos).sqrMagnitude;
                if (sqrDistance < _veinMinDistance * _veinMinDistance)
                {
                    return false;
                }
            }
            return true;
        }

        // (Assuming you integrate this into the main class from the previous step)
        private void GrowOreVein(WorldGenContext ctx, Vector2Int seedPos)
        {
            // 1. Determine a randomized target size for this specific vein
            int targetVeinSize = ctx.Random.Next(_minVeinSize, _maxVeinSize + 1);

            // Track tiles we have already converted to ore
            HashSet<Vector2Int> placedOrePositions = new();
            // Track outer tiles we have already evaluated so we don't evaluate them twice
            HashSet<Vector2Int> visitedPositions = new();
            // Queue for processing neighboring tiles layer-by-layer (BFS)
            Queue<Vector2Int> frontier = new();

            // 2. Initialize with the seed position
            ctx.FgGrid[seedPos.x, seedPos.y] = new TileData(_ironOreTile.GetId());
            placedOrePositions.Add(seedPos);
            visitedPositions.Add(seedPos);

            // Add the initial neighbors to start the growth
            foreach (var dir in _cardinalDirections)
            {
                Vector2Int neighbor = seedPos + dir;
                bool isAir = ctx.FgGrid[seedPos.x, seedPos.y].IsAir;
                
                if (IsWithinWorldBounds(ctx, neighbor) && !isAir)
                {
                    frontier.Enqueue(neighbor);
                    visitedPositions.Add(neighbor);
                }
            }

            // 3. Loop through the neighbors while we have options and haven't hit our target size
            while (frontier.Count > 0 && placedOrePositions.Count < targetVeinSize)
            {
                Vector2Int currentTile = frontier.Dequeue();

                // Calculate distance from the starting seed
                float distance = Vector2Int.Distance(seedPos, currentTile);
                
                float maxVeinRadius = 6f;
                float minProb = 0.25f;
                float maxProb = 0.8f;

                float t = 1f - Mathf.Clamp01(distance / maxVeinRadius);
                float smoothedT = Mathf.SmoothStep(0f, 1f, t);

                float spawnChance = Mathf.Lerp(minProb, maxProb, smoothedT);

                float roll = (float)ctx.Random.NextDouble(); // Generates a value between 0.0 and 1.0

                if (roll <= spawnChance)
                {
                    // Success! Convert this tile to ore
                    ctx.FgGrid[currentTile.x, currentTile.y] = new TileData(_ironOreTile.GetId());
                    placedOrePositions.Add(currentTile);

                    // Since this tile is now part of the cluster, add ITS neighbors to the frontier
                    foreach (var dir in _cardinalDirections)
                    {
                        Vector2Int neighbor = currentTile + dir;

                        // Only add if it's in the world bounds and we haven't checked it yet
                        if (IsWithinWorldBounds(ctx, neighbor) && !visitedPositions.Contains(neighbor))
                        {
                            frontier.Enqueue(neighbor);
                            visitedPositions.Add(neighbor);
                        }
                    }
                }
            }
        }

        private bool IsWithinWorldBounds(WorldGenContext ctx, Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < ctx.Width && pos.y >= 0 && pos.y < ctx.Height;
        }
    }
}
