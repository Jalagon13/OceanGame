using System;
using UnityEngine;

namespace OceanGame
{
    public static class GridPhysics
    {
        private const float SKIN_SIZE = 0.01f;

        public struct CollisionResult
        {
            public Vector2 NewPosition;
            public bool TouchingLeft;
            public bool TouchingRight;
            public bool TouchingBottom; // Can use this for grounded flag
            public bool TouchingTop;
        }

        public static CollisionResult MoveAndResolve(Vector2 currentPos, Vector2 velocity, Vector2 size, float deltaTime, bool ignoreCollisions = false)
        {
            CollisionResult result = new();

            var world = WorldManager.Instance;

            float halfWidth = size.x / 2f;
            float halfHeight = size.y / 2f;

            // Get the player's bounding box BEFORE moving this frame
            float startLeft = currentPos.x - halfWidth;
            float startRight = currentPos.x + halfWidth;
            float startBottom = currentPos.y - halfHeight;
            float startTop = currentPos.y + halfHeight;

            bool isEntombedX = false;
            bool isEntombedY = false;

            if (!ignoreCollisions) // If we ignore collisions, skip entombed check
            {
                // For checking entombed state
                bool isLeftOverlapping = false;
                bool isRightOverlapping = false;
                bool isBottomOverlapping = false;
                bool isTopOverlapping = false;

                // Scan the immediate tiles around the player's current position to check for entombment
                int sMinX = Mathf.FloorToInt(startLeft);
                int sMaxX = Mathf.CeilToInt(startRight);
                int sMinY = Mathf.FloorToInt(startBottom);
                int sMaxY = Mathf.CeilToInt(startTop);

                for (int x = sMinX; x <= sMaxX; x++)
                {
                    for (int y = sMinY; y <= sMaxY; y++)
                    {
                        var fgTd = world.FgLayer.GetTileData(x, y);

                        if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid)
                        {
                            // Check if we overlap this solid tile
                            if (IsOverlapping(startLeft, startRight, startBottom, startTop, x, x + 1, y, y + 1))
                            {
                                // Determine which edges of the player are currently inside this tile
                                if (startLeft < x + 1 && startLeft > x) isLeftOverlapping = true;
                                if (startRight > x && startRight < x + 1) isRightOverlapping = true;
                                if (startBottom < y + 1 && startBottom > y) isBottomOverlapping = true;
                                if (startTop > y && startTop < y + 1) isTopOverlapping = true;
                            }
                        }
                    }
                }

                // Determine entombment states. Entombed meaning overlapping on both sides of the bounding box on either axis
                isEntombedX = isLeftOverlapping && isRightOverlapping;
                isEntombedY = isBottomOverlapping && isTopOverlapping;
            }

            if (isEntombedX)
            {
                velocity.x = 0; // If entombed, dont move it
            }
            else if(velocity.x != 0) // Only scan if we are actually moving horizontally
            {
                // First focus on the horizontal movement
                currentPos.x += velocity.x * deltaTime;
                
                if(!ignoreCollisions) // If we ignore collisions, skip collision check
                {
                    // Define the bounding box area based on our new potential X position
                    float playerLeft = currentPos.x - halfWidth;
                    float playerRight = currentPos.x + halfWidth;
                    float playerBottom = currentPos.y - halfHeight;
                    float playerTop = currentPos.y + halfHeight;

                    // Get a tiny grid loop range surrounding the player's bounds
                    int minX = Mathf.FloorToInt(playerLeft);
                    int maxX = Mathf.CeilToInt(playerRight);
                    int minY = Mathf.FloorToInt(playerBottom);
                    int maxY = Mathf.CeilToInt(playerTop);

                    for (int tileX = minX; tileX <= maxX; tileX++)
                    {
                        for (int tileY = minY; tileY <= maxY; tileY++)
                        {
                            // If the tile is solid or out of bounds
                            var fgTd = world.FgLayer.GetTileData(tileX, tileY);

                            if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid)
                            {
                                // Check if the player's box overlaps this specific tile's AABB
                                if (IsOverlapping(playerLeft, playerRight, playerBottom, playerTop, tileX, tileX + 1, tileY, tileY + 1))
                                {
                                    // Resolve horizontal overlap: Are we moving right or left?
                                    if (velocity.x > 0 && playerRight > tileX && startRight <= tileX)
                                    {
                                        // Triggered right wall impact
                                        // Moving right: push back to the left edge of the block
                                        currentPos.x = tileX - halfWidth;
                                    }
                                    else if (velocity.x < 0 && playerLeft < tileX + 1 && startLeft >= tileX + 1)
                                    {
                                        // Triggered left wall impact
                                        // Moving left: push forward to the right edge of the block
                                        currentPos.x = (tileX + 1) + halfWidth;
                                    }

                                    // Re-cache edges for next iterations in the grid loop
                                    playerLeft = currentPos.x - halfWidth;
                                    playerRight = currentPos.x + halfWidth;
                                }
                            }
                        }
                    }
                }
            }

            if (isEntombedY)
            {
                velocity.y = 0;
            }
            else if(velocity.y != 0)
            {
                // Second focus on the vertical movement
                currentPos.y += velocity.y * deltaTime;

                if (!ignoreCollisions) // If we ignore collisions, skip collision check
                {
                    // Recalculate vertical edges with the x positons from earlier
                    float playerLeft = currentPos.x - halfWidth;
                    float playerRight = currentPos.x + halfWidth;
                    float playerBottom = currentPos.y - halfHeight;
                    float playerTop = currentPos.y + halfHeight;

                    // Again Get a tiny grid loop range surrounding the player's bounds
                    int minX = Mathf.FloorToInt(playerLeft);
                    int maxX = Mathf.CeilToInt(playerRight);
                    int minY = Mathf.FloorToInt(playerBottom);
                    int maxY = Mathf.CeilToInt(playerTop);

                    for (int tileX = minX; tileX <= maxX; tileX++)
                    {
                        for (int tileY = minY; tileY <= maxY; tileY++)
                        {
                            var fgTd = world.FgLayer.GetTileData(tileX, tileY);

                            if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid)
                            {
                                if (IsOverlapping(playerLeft, playerRight, playerBottom, playerTop, tileX, tileX + 1, tileY, tileY + 1))
                                {
                                    // Resolve vertical overlap: Are we going up or down
                                    if (velocity.y > 0 && playerTop > tileY && startTop <= tileY)
                                    {
                                        // Hit ceiling
                                        // Moving up: hit ceiling, push down below the block
                                        currentPos.y = tileY - halfHeight;
                                    }
                                    else if (velocity.y < 0 && playerBottom < tileY + 1 && startBottom >= tileY + 1)
                                    {
                                        // Landed on ground (Grounded)
                                        // Moving down: landed on ground, push up on top of the block
                                        currentPos.y = (tileY + 1) + halfHeight;
                                    }

                                    // Re-cache edges
                                    playerBottom = currentPos.y - halfHeight;
                                    playerTop = currentPos.y + halfHeight;
                                }
                            }
                        }
                    }
                }
            }
            
            if(!ignoreCollisions) // If we are ignoring collisions, do not calculate touching checks
            {
                // Skin contact monitoring
                // Recompute the final player boundaries after all movement resolutions are done
                float finalLeft = currentPos.x - halfWidth;
                float finalRight = currentPos.x + halfWidth;
                float finalBottom = currentPos.y - halfHeight;
                float finalTop = currentPos.y + halfHeight;

                // We slightly pull the perpendicular corners inward to prevent side-walls 
                // from falsely flagging as ground contact or ceiling contact.
                float inset = 0.02f;

                // Check Bottom Contact (Grounded Check)
                int bMinX = Mathf.FloorToInt(finalLeft + inset);
                int bMaxX = Mathf.FloorToInt(finalRight - inset);
                int bTileY = Mathf.FloorToInt(finalBottom - SKIN_SIZE);
                for (int x = bMinX; x <= bMaxX; x++)
                {
                    var fgTd = world.FgLayer.GetTileData(x, bTileY);

                    if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid) result.TouchingBottom = true;
                }

                // Check Top Contact (Ceiling Check)
                int tMinX = Mathf.FloorToInt(finalLeft + inset);
                int tMaxX = Mathf.FloorToInt(finalRight - inset);
                int tTileY = Mathf.FloorToInt(finalTop + SKIN_SIZE);
                for (int x = tMinX; x <= tMaxX; x++)
                {
                    var fgTd = world.FgLayer.GetTileData(x, tTileY);

                    if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid) result.TouchingTop = true;
                }

                // Check Left Contact (Left Wall Check)
                int lTileX = Mathf.FloorToInt(finalLeft - SKIN_SIZE);
                int lMinY = Mathf.FloorToInt(finalBottom + inset);
                int lMaxY = Mathf.FloorToInt(finalTop - inset);
                for (int y = lMinY; y <= lMaxY; y++)
                {
                    var fgTd = world.FgLayer.GetTileData(lTileX, y);

                    if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid) result.TouchingLeft = true;
                }

                // Check Right Contact (Right Wall Check)
                int rTileX = Mathf.FloorToInt(finalRight + SKIN_SIZE);
                int rMinY = Mathf.FloorToInt(finalBottom + inset);
                int rMaxY = Mathf.FloorToInt(finalTop - inset);
                for (int y = rMinY; y <= rMaxY; y++)
                {
                    var fgTd = world.FgLayer.GetTileData(rTileX, y);

                    if ((fgTd.HasTile || fgTd.IsOutOfBounds) && fgTd.IsSolid) result.TouchingRight = true;
                }

                // If the entity is entombed, force their contact indicators to true natively
                if (isEntombedX) { result.TouchingLeft = true; result.TouchingRight = true; }
                if (isEntombedY) { result.TouchingBottom = true; result.TouchingTop = true; }
            }

            result.NewPosition = currentPos;
            return result;
        }

        // Pure mathematical evaluation: checks if two rectangular boundary definitions intersect
        private static bool IsOverlapping(float b1Left, float b1Right, float b1Bottom, float b1Top, float b2Left, float b2Right, float b2Bottom, float b2Top)
        {
            return b1Right > b2Left && b1Left < b2Right && b1Top > b2Bottom && b1Bottom < b2Top;
        }
    }
}
