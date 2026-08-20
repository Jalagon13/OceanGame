using System;
using UnityEngine;

namespace OceanGame
{
    public static class GridPhysics
    {
        public static Vector2 MoveAndResolve(Vector2 currentPos, Vector2 velocity, Vector2 size, float deltaTime)
        {
            var world = WorldManager.Instance;

            float halfWidth = size.x / 2f;
            float halfHeight = size.y / 2f;

            // Get the player's bounding box BEFORE moving this frame
            float startLeft = currentPos.x - halfWidth;
            float startRight = currentPos.x + halfWidth;
            float startBottom = currentPos.y - halfHeight;
            float startTop = currentPos.y + halfHeight;

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
                    if (world.ForegroundLayer[x, y] >= 0)
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
            bool isEntombedX = isLeftOverlapping && isRightOverlapping;
            bool isEntombedY = isBottomOverlapping && isTopOverlapping;

            if (isEntombedX)
            {
                velocity.x = 0; // If entombed, dont move it
            }
            else if(velocity.x != 0) // Only scan if we are actually moving horizontally
            {
                // First focus on the horizontal movement
                currentPos.x += velocity.x * deltaTime;

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
                        // If the tile is solid (0 or higher, since -1 is Air and -2 is Out of bounds)
                        if (world.ForegroundLayer[tileX, tileY] >= 0)
                        {
                            // Check if the player's box overlaps this specific tile's AABB
                            if (IsOverlapping(playerLeft, playerRight, playerBottom, playerTop, tileX, tileX + 1, tileY, tileY + 1))
                            {
                                // Resolve horizontal overlap: Are we moving right or left?
                                if (velocity.x > 0 && playerRight > tileX && startRight <= tileX)
                                {
                                    // Moving right: push back to the left edge of the block
                                    currentPos.x = tileX - halfWidth;
                                }
                                else if (velocity.x < 0 && playerLeft < tileX + 1 && startLeft >= tileX + 1)
                                {
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

            if (isEntombedY)
            {
                velocity.y = 0;
            }
            else if(velocity.y != 0)
            {
                // Second focus on the vertical movement
                currentPos.y += velocity.y * deltaTime;

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
                        if (world.ForegroundLayer[tileX, tileY] >= 0)
                        {
                            if (IsOverlapping(playerLeft, playerRight, playerBottom, playerTop, tileX, tileX + 1, tileY, tileY + 1))
                            {
                                // Resolve vertical overlap: Are we going up or down
                                if (velocity.y > 0 && playerTop > tileY && startTop <= tileY)
                                {
                                    // Moving up: hit ceiling, push down below the block
                                    currentPos.y = tileY - halfHeight;
                                }
                                else if (velocity.y < 0 && playerBottom < tileY + 1 && startBottom >= tileY + 1)
                                {
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

            return currentPos;
        }

        // Pure mathematical evaluation: checks if two rectangular boundary definitions intersect
        private static bool IsOverlapping(float b1Left, float b1Right, float b1Bottom, float b1Top, float b2Left, float b2Right, float b2Bottom, float b2Top)
        {
            return b1Right > b2Left && b1Left < b2Right && b1Top > b2Bottom && b1Bottom < b2Top;
        }
    }
}
