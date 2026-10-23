using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SketchLand
{
    [Serializable]
    public class OreGenStep : WorldGenStep
    {
        [SerializeField] private List<OreConfig> _oreConfigs = new();
        
        [Serializable]
        private class OreConfig
        {
            public TileConfigSO OreTile;
            public int StartingDepth = 20;
            public int ChunkSize = 30;
            public int VeinsPerChunk = 6;
            public int VeinMinDistance = 6;
            public int MaxAttemptsPerVein = 15;
            public int MinVeinSize = 2;
            public int MaxVeinSize = 8;
        }

        private readonly Vector2Int[] _cardinalDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public override IEnumerator Execute(WorldGenContext ctx)
        {
            foreach (OreConfig oreConfig in _oreConfigs)
            {
                int chunkSize = Mathf.Max(1, oreConfig.ChunkSize);
                int highestAllowedY = Mathf.Min(ctx.SeaLevel - oreConfig.StartingDepth, ctx.Height - 1);

                if (highestAllowedY < 0)
                {
                    continue;
                }

                for (int chunkX = 0; chunkX < ctx.Width; chunkX += chunkSize)
                {
                    for (int chunkY = 0; chunkY <= highestAllowedY; chunkY += chunkSize)
                    {
                        HashSet<Vector2Int> foundVeinPositions = new();

                        for (int i = 0; i < oreConfig.VeinsPerChunk; i++)
                        {
                            int currentAttempt = 0;
                            bool veinPlaced = false;

                            while (currentAttempt < oreConfig.MaxAttemptsPerVein && !veinPlaced)
                            {
                                int maxX = Mathf.Min(chunkX + chunkSize, ctx.Width);
                                int maxYExclusive = Mathf.Min(chunkY + chunkSize, highestAllowedY + 1);

                                Vector2Int randPos = new(ctx.Random.Next(chunkX, maxX), ctx.Random.Next(chunkY, maxYExclusive));

                                if (IsValidVeinPosition(ctx, randPos, foundVeinPositions, oreConfig))
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

                        foreach (Vector2Int seedPos in foundVeinPositions)
                        {
                            GrowOreVein(ctx, seedPos, oreConfig);
                        }
                    }

                    yield return null;
                }
            }
        }

        private bool IsValidVeinPosition(WorldGenContext ctx, Vector2Int pos, HashSet<Vector2Int> existingPositions, OreConfig oreConfig)
        {
            if (!CanPlaceOreAt(ctx, pos, oreConfig))
            {
                return false;
            }

            foreach (Vector2Int existingPos in existingPositions)
            {
                float sqrDistance = (existingPos - pos).sqrMagnitude;
                if (sqrDistance < oreConfig.VeinMinDistance * oreConfig.VeinMinDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanPlaceOreAt(WorldGenContext ctx, Vector2Int pos, OreConfig oreConfig)
        {
            if (!IsWithinWorldBounds(ctx, pos))
            {
                return false;
            }

            int depth = ctx.SeaLevel - pos.y;
            if (depth < oreConfig.StartingDepth)
            {
                return false;
            }

            return !ctx.FgGrid[pos.x, pos.y].IsAir;
        }

        private void GrowOreVein(WorldGenContext ctx, Vector2Int seedPos, OreConfig oreConfig)
        {
            int targetVeinSize = ctx.Random.Next(oreConfig.MinVeinSize, oreConfig.MaxVeinSize + 1);

            HashSet<Vector2Int> placedOrePositions = new();
            HashSet<Vector2Int> visitedPositions = new();
            Queue<Vector2Int> frontier = new();

            ctx.FgGrid[seedPos.x, seedPos.y] = new TileData(oreConfig.OreTile.GetId());
            placedOrePositions.Add(seedPos);
            visitedPositions.Add(seedPos);

            foreach (Vector2Int dir in _cardinalDirections)
            {
                Vector2Int neighbor = seedPos + dir;

                if (IsWithinWorldBounds(ctx, neighbor) && visitedPositions.Add(neighbor) && CanPlaceOreAt(ctx, neighbor, oreConfig))
                {
                    frontier.Enqueue(neighbor);
                }
            }

            while (frontier.Count > 0 && placedOrePositions.Count < targetVeinSize)
            {
                Vector2Int currentTile = frontier.Dequeue();

                float distance = Vector2Int.Distance(seedPos, currentTile);
                float maxVeinRadius = 6f;
                float minProb = 0.25f;
                float maxProb = 0.8f;

                float t = 1f - Mathf.Clamp01(distance / maxVeinRadius);
                float smoothedT = Mathf.SmoothStep(0f, 1f, t);
                float spawnChance = Mathf.Lerp(minProb, maxProb, smoothedT);
                float roll = (float)ctx.Random.NextDouble();

                if (roll <= spawnChance)
                {
                    ctx.FgGrid[currentTile.x, currentTile.y] = new TileData(oreConfig.OreTile.GetId());
                    placedOrePositions.Add(currentTile);

                    foreach (Vector2Int dir in _cardinalDirections)
                    {
                        Vector2Int neighbor = currentTile + dir;

                        if (IsWithinWorldBounds(ctx, neighbor) && visitedPositions.Add(neighbor) && CanPlaceOreAt(ctx, neighbor, oreConfig))
                        {
                            frontier.Enqueue(neighbor);
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