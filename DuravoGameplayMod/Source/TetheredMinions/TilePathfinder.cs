// MIT Licensed - Copyright (c) 2025 David W. Jeske
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DuravoQOLMod.TetheredMinions
{
    /// <summary>
    /// Simple A* pathfinder on Terraria's tile grid.
    /// Used to determine if a walking/flying path exists between two points.
    /// </summary>
    public static class TilePathfinder
    {
        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                        TUNABLE CONSTANTS                           ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>Get debug pathfinding setting from mod config</summary>
        private static bool DebugPathfinding => ModContent.GetInstance<DuravoQOLModConfig>()?.Debug?.DebugMinionPathfinding ?? false;

        /// <summary>Default maximum search radius in tiles (bounded by minion tether distance)</summary>
        public const int DefaultMaxSearchRadiusTiles = 100;

        /// <summary>Maximum nodes to explore before giving up (prevents runaway on complex maps)</summary>
        private const int MaxNodesExplored = 1500;

        /// <summary>Pixels per tile for clearance calculations</summary>
        private const float PixelsPerTile = 16f;

        // 4-directional movement offsets (no diagonals - corners can't be cut)
        private static readonly Point[] CardinalOffsets = {
            new Point(0, -1),   // Up
            new Point(0, 1),    // Down
            new Point(-1, 0),   // Left
            new Point(1, 0)     // Right
        };

        // Thread-local storage for current pathfinding clearance (avoids parameter threading everywhere)
        [ThreadStatic]
        private static int _currentClearanceWidthTiles;
        [ThreadStatic]
        private static int _currentClearanceHeightTiles;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                          A* NODE                                   ║
        // ╚════════════════════════════════════════════════════════════════════╝

        private struct PathNode : IComparable<PathNode>
        {
            public float FCost;  // GCost + heuristic to goal
            public float GCost;  // Cost from start            
            public Point TilePosition;
            public int CompareTo(PathNode other)
            {
                int cmp = FCost.CompareTo(other.FCost);
                if (cmp != 0) return cmp;

                // Tiebreaker by position to ensure unique ordering in SortedSet
                cmp = TilePosition.X.CompareTo(other.TilePosition.X);
                if (cmp != 0) return cmp;

                return TilePosition.Y.CompareTo(other.TilePosition.Y);
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      PUBLIC API                                    ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Calculate the clearance in tiles needed for a given size in pixels.
        /// Rounds up to ensure the entity can always fit.
        /// </summary>
        public static int CalculateTileClearance(float sizePixels)
        {
            return (int)MathF.Ceiling(sizePixels / PixelsPerTile);
        }

        /// <summary>
        /// Check if a walking/flying path exists between two world positions.
        /// Does NOT return the actual path, just whether one exists.
        /// Uses default 2x2 tile clearance.
        /// </summary>
        public static bool PathExists(Vector2 fromWorldPosition, Vector2 toWorldPosition)
        {
            return PathExists(fromWorldPosition, toWorldPosition, 2, 2);
        }

        /// <summary>
        /// Check if a walking/flying path exists for an entity with specific hitbox size.
        /// </summary>
        /// <param name="fromWorldPosition">Start position in world coordinates (pixels)</param>
        /// <param name="toWorldPosition">Goal position in world coordinates (pixels)</param>
        /// <param name="entityWidthPixels">Entity hitbox width in pixels</param>
        /// <param name="entityHeightPixels">Entity hitbox height in pixels</param>
        /// <returns>True if a path exists, false if blocked or unreachable</returns>
        public static bool PathExists(Vector2 fromWorldPosition, Vector2 toWorldPosition, float entityWidthPixels, float entityHeightPixels)
        {
            Point startTile = fromWorldPosition.ToTileCoordinates();
            Point goalTile = toWorldPosition.ToTileCoordinates();

            int clearanceWidth = CalculateTileClearance(entityWidthPixels);
            int clearanceHeight = CalculateTileClearance(entityHeightPixels);

            return FindPathBetweenTiles(startTile, goalTile, clearanceWidth, clearanceHeight) != null;
        }

        /// <summary>
        /// Find the actual A* path between two world positions.
        /// Returns list of world pixel positions (centered for clearance) from start to goal, or null if no path exists.
        /// Uses default 2x2 tile clearance.
        /// </summary>
        public static List<Vector2>? FindPath(Vector2 fromWorldPosition, Vector2 toWorldPosition)
        {
            return FindPath(fromWorldPosition, toWorldPosition, 32f, 32f);  // Default 2x2 tile = 32x32 pixels
        }

        /// <summary>
        /// Find the actual A* path for an entity with specific hitbox size.
        /// Returns waypoints in WORLD PIXEL coordinates, centered for the entity's clearance.
        /// </summary>
        /// <param name="fromWorldPosition">Start position in world coordinates (pixels)</param>
        /// <param name="toWorldPosition">Goal position in world coordinates (pixels)</param>
        /// <param name="entityWidthPixels">Entity hitbox width in pixels</param>
        /// <param name="entityHeightPixels">Entity hitbox height in pixels</param>
        /// <param name="maxPathLengthTiles">Optional maximum search radius in tiles (default: 35)</param>
        /// <returns>List of Vector2 world positions from start to goal, or null if no path</returns>
        public static List<Vector2>? FindPath(Vector2 fromWorldPosition, Vector2 toWorldPosition, float entityWidthPixels, float entityHeightPixels, int maxPathLengthTiles = DefaultMaxSearchRadiusTiles)
        {
            Point startTile = fromWorldPosition.ToTileCoordinates();
            Point goalTile = toWorldPosition.ToTileCoordinates();

            int clearanceWidth = CalculateTileClearance(entityWidthPixels);
            int clearanceHeight = CalculateTileClearance(entityHeightPixels);

            List<Point>? tilePath = FindPathBetweenTiles(startTile, goalTile, clearanceWidth, clearanceHeight, maxPathLengthTiles);

            if (tilePath == null) {
                return null;
            }

            // Convert tile path to world coordinates, centering on the clearance area
            var worldPath = new List<Vector2>(tilePath.Count);
            float halfClearanceX = (clearanceWidth * PixelsPerTile) / 2f;
            float halfClearanceY = (clearanceHeight * PixelsPerTile) / 2f;

            foreach (Point tilePos in tilePath) {
                // Target the CENTER of the clearance area, not the tile corner
                Vector2 worldPos = new Vector2(
                    tilePos.X * PixelsPerTile + halfClearanceX,
                    tilePos.Y * PixelsPerTile + halfClearanceY
                );
                worldPath.Add(worldPos);
            }

            // Smooth the path by removing redundant waypoints where diagonal shortcuts are possible
            if (worldPath.Count > 2) {
                worldPath = SmoothWorldPath(worldPath, clearanceWidth, clearanceHeight);
            }

            return worldPath;
        }

        /// <summary>
        /// Smooth a world-coordinate path by removing intermediate waypoints when direct diagonal
        /// shortcuts are possible. Uses line-of-sight checks with clearance.
        /// </summary>
        private static List<Vector2> SmoothWorldPath(List<Vector2> originalPath, int clearanceWidth, int clearanceHeight)
        {
            if (originalPath.Count <= 2) {
                return originalPath;  // Nothing to smooth
            }

            // Store clearance for IsTilePassable calls
            _currentClearanceWidthTiles = clearanceWidth;
            _currentClearanceHeightTiles = clearanceHeight;

            var smoothedPath = new List<Vector2> { originalPath[0] };
            int currentIndex = 0;

            while (currentIndex < originalPath.Count - 1) {
                // Try to find the farthest waypoint we can reach directly
                int farthestReachable = currentIndex + 1;

                for (int testIndex = currentIndex + 2; testIndex < originalPath.Count; testIndex++) {
                    if (HasMinionVectorClearance(originalPath[currentIndex], originalPath[testIndex])) {
                        farthestReachable = testIndex;
                    } else {
                        // Once we can't reach a waypoint, stop looking further
                        // (obstacles generally don't "un-block" as we go further)
                        break;
                    }
                }

                // Add the farthest reachable waypoint
                smoothedPath.Add(originalPath[farthestReachable]);
                currentIndex = farthestReachable;
            }

            if (DebugPathfinding && smoothedPath.Count < originalPath.Count) {
                int removed = originalPath.Count - smoothedPath.Count;
                Main.NewText($"[A*] Path smoothed: {originalPath.Count} → {smoothedPath.Count} waypoints (-{removed})", Microsoft.Xna.Framework.Color.Lime);
            }

            return smoothedPath;
        }

        /// <summary>
        /// Check if a minion can traverse directly between two world positions,
        /// accounting for entity clearance by checking all 4 corners of the hitbox.
        /// </summary>
        private static bool HasMinionVectorClearance(Vector2 fromWorld, Vector2 toWorld)
        {
            // The waypoints are CENTERED on the entity's clearance area
            // Calculate corner offsets based on clearance (in pixels)
            float halfWidthPixels = (_currentClearanceWidthTiles * PixelsPerTile) / 2f;
            float halfHeightPixels = (_currentClearanceHeightTiles * PixelsPerTile) / 2f;

            // Define the 4 corner offsets from center
            Vector2[] cornerOffsets = {
                new Vector2(-halfWidthPixels, -halfHeightPixels),  // Top-left
                new Vector2(halfWidthPixels - 1, -halfHeightPixels),   // Top-right (subtract 1 to stay inside hitbox)
                new Vector2(-halfWidthPixels, halfHeightPixels - 1),   // Bottom-left
                new Vector2(halfWidthPixels - 1, halfHeightPixels - 1) // Bottom-right
            };

            // Check line-of-sight for each corner
            for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++) {
                Vector2 fromCorner = fromWorld + cornerOffsets[cornerIndex];
                Vector2 toCorner = toWorld + cornerOffsets[cornerIndex];

                if (!IsSingleLineClear(fromCorner, toCorner)) {
                    return false;  // This corner's path is blocked
                }
            }

            return true;  // All 4 corners can reach their destinations
        }

        /// <summary>
        /// Check if a single line (Bresenham) between two world positions is clear.
        /// Uses IsSingleTilePassable (not full clearance check).
        /// </summary>
        private static bool IsSingleLineClear(Vector2 fromWorld, Vector2 toWorld)
        {
            Point fromTile = fromWorld.ToTileCoordinates();
            Point toTile = toWorld.ToTileCoordinates();

            // Use Bresenham's line algorithm
            int dx = Math.Abs(toTile.X - fromTile.X);
            int dy = Math.Abs(toTile.Y - fromTile.Y);
            int stepX = fromTile.X < toTile.X ? 1 : -1;
            int stepY = fromTile.Y < toTile.Y ? 1 : -1;

            int currentX = fromTile.X;
            int currentY = fromTile.Y;
            int error = dx - dy;

            while (true) {
                // Check if current tile is passable (skip start tile - entity is already there)
                if (!(currentX == fromTile.X && currentY == fromTile.Y)) {
                    if (!IsSingleTilePassable(currentX, currentY)) {
                        return false;
                    }
                }

                // Reached destination?
                if (currentX == toTile.X && currentY == toTile.Y) {
                    return true;
                }

                int error2 = 2 * error;
                if (error2 > -dy) {
                    error -= dy;
                    currentX += stepX;
                }
                if (error2 < dx) {
                    error += dx;
                    currentY += stepY;
                }
            }
        }

        /// <summary>
        /// Check if a path exists between two tile positions.
        /// </summary>
        public static bool PathExistsBetweenTiles(Point startTile, Point goalTile, int clearanceWidthTiles = 2, int clearanceHeightTiles = 2)
        {
            return FindPathBetweenTiles(startTile, goalTile, clearanceWidthTiles, clearanceHeightTiles) != null;
        }

        /// <summary>
        /// Find path between two tile positions using A*.
        /// </summary>
        /// <param name="startTile">Starting tile position</param>
        /// <param name="goalTile">Goal tile position</param>
        /// <param name="clearanceWidthTiles">Required clearance width in tiles</param>
        /// <param name="clearanceHeightTiles">Required clearance height in tiles</param>
        /// <param name="maxSearchRadiusTiles">Maximum search radius in tiles</param>
        /// <returns>List of tile Points from start to goal (inclusive), or null if no path</returns>
        public static List<Point>? FindPathBetweenTiles(Point startTile, Point goalTile, int clearanceWidthTiles = 2, int clearanceHeightTiles = 2, int maxSearchRadiusTiles = DefaultMaxSearchRadiusTiles)
        {
            // Store clearance in thread-local variables for use by IsTilePassable
            _currentClearanceWidthTiles = Math.Max(1, clearanceWidthTiles);
            _currentClearanceHeightTiles = Math.Max(1, clearanceHeightTiles);

            // NOTE: We do NOT check if start OR goal tiles are passable!
            // - Start: The entity is ALREADY standing there, so by definition it must be valid.
            // - Goal: The player/target is standing there, so by definition it must be reachable.
            // Checking either would fail when entity is right next to a wall due to clearance overlap.
            // The A* search will naturally find a valid path if one exists.

            // Quick check: same tile
            if (startTile == goalTile) {
                return new List<Point> { startTile };
            }

            // Quick check: if distance > max radius, definitely no path within bounds
            float tileDistance = Vector2.Distance(startTile.ToVector2(), goalTile.ToVector2());
            if (tileDistance > maxSearchRadiusTiles) {
                if (DebugPathfinding) {
                    Terraria.Main.NewText($"[A*] FAILED: Distance {tileDistance:F1} tiles > max {maxSearchRadiusTiles} tiles", Microsoft.Xna.Framework.Color.Red);
                }
                return null;
            }

            // A* search - SortedSet uses PathNode.CompareTo for ordering
            var openSet = new SortedSet<PathNode>();

            var gCosts = new Dictionary<Point, float>();
            var cameFrom = new Dictionary<Point, Point>(); // Track path for reconstruction

            // Initialize with start node
            float startHeuristic = Heuristic(startTile, goalTile);
            openSet.Add(new PathNode {
                TilePosition = startTile,
                GCost = 0f,
                FCost = startHeuristic
            });
            gCosts[startTile] = 0f;

            if (DebugPathfinding) {
                Terraria.Main.NewText($"[A*] START: ({startTile.X},{startTile.Y}) → GOAL: ({goalTile.X},{goalTile.Y}), clearance {_currentClearanceWidthTiles}x{_currentClearanceHeightTiles}", Microsoft.Xna.Framework.Color.Cyan);
            }

            int nodesExplored = 0;

            while (openSet.Count > 0 && nodesExplored < MaxNodesExplored) {
                // Get node with lowest FCost
                PathNode currentNode = openSet.Min;
                openSet.Remove(currentNode);

                Point currentTile = currentNode.TilePosition;

                // Check if we reached the goal
                if (currentTile == goalTile) {
                    // Reconstruct path from goal back to start
                    return ReconstructPath(cameFrom, startTile, goalTile);
                }

                // Skip if we already found a better path to this tile
                // (This node might be a stale duplicate with higher cost)
                if (gCosts.TryGetValue(currentTile, out float storedGCost) && currentNode.GCost > storedGCost) {
                    continue;
                }
                nodesExplored++;

                // Explore cardinal neighbors (4-directional - no diagonals since corners can't be cut)
                for (int i = 0; i < CardinalOffsets.Length; i++) {
                    Point offset = CardinalOffsets[i];
                    Point neighborTile = new Point(currentTile.X + offset.X, currentTile.Y + offset.Y);

                    // Skip if too far from goal (bounding box optimization)
                    if (Math.Abs(neighborTile.X - goalTile.X) > maxSearchRadiusTiles ||
                        Math.Abs(neighborTile.Y - goalTile.Y) > maxSearchRadiusTiles) {
                        continue;
                    }

                    // Skip if not passable (with debug for first few nodes)
                    if (!IsTilePassable(neighborTile.X, neighborTile.Y)) {
                        // Debug: Show exactly which sub-tile blocked (only for first 3 explored nodes)
                        if (DebugPathfinding && nodesExplored <= 3) {
                            string dir = offset.Y < 0 ? "Up" : offset.Y > 0 ? "Down" : offset.X < 0 ? "Left" : "Right";
                            string blockingTile = FindBlockingSubTile(neighborTile.X, neighborTile.Y);
                            Terraria.Main.NewText($"[A*] {dir}({neighborTile.X},{neighborTile.Y}) blocked by {blockingTile}", Microsoft.Xna.Framework.Color.Orange);
                        }
                        continue;
                    }

                    float tentativeGCost = currentNode.GCost + 1f;  // All cardinal moves cost 1

                    // Skip if we already have a better path to this tile
                    if (gCosts.TryGetValue(neighborTile, out float existingGCost) && tentativeGCost >= existingGCost) {
                        continue;
                    }

                    // Update/add this neighbor
                    gCosts[neighborTile] = tentativeGCost;
                    cameFrom[neighborTile] = currentTile; // Track where we came from
                    float fCost = tentativeGCost + Heuristic(neighborTile, goalTile);

                    openSet.Add(new PathNode {
                        TilePosition = neighborTile,
                        GCost = tentativeGCost,
                        FCost = fCost
                    });
                }
            }

            // If we get here, no path was found
            if (DebugPathfinding) {
                if (openSet.Count == 0) {
                    // All possible paths were blocked by clearance checks
                    Terraria.Main.NewText($"[A*] FAILED: All paths blocked (explored {nodesExplored} tiles, openSet empty)", Microsoft.Xna.Framework.Color.Red);
                }
                else {
                    // Hit the node exploration limit
                    Terraria.Main.NewText($"[A*] FAILED: Hit node limit ({nodesExplored}/{MaxNodesExplored} explored)", Microsoft.Xna.Framework.Color.Red);
                }
            }
            return null;
        }

        /// <summary>
        /// Reconstruct the path from cameFrom dictionary.
        /// Returns list from start to goal (inclusive).
        /// </summary>
        private static List<Point> ReconstructPath(Dictionary<Point, Point> cameFrom, Point startTile, Point goalTile)
        {
            var path = new List<Point>();
            Point current = goalTile;

            while (current != startTile) {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(startTile);

            // Reverse so it goes start -> goal
            path.Reverse();
            return path;
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║                      HELPER METHODS                                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Check if a tile has clearance for minion movement (NxM tile area).
        /// This accounts for minion hitboxes being larger than 1 tile.
        /// Uses the current thread-local clearance values.
        /// </summary>
        private static bool IsTilePassable(int tileX, int tileY)
        {
            // Check an area of clearanceWidth x clearanceHeight tiles
            for (int xOffset = 0; xOffset < _currentClearanceWidthTiles; xOffset++) {
                for (int yOffset = 0; yOffset < _currentClearanceHeightTiles; yOffset++) {
                    if (!IsSingleTilePassable(tileX + xOffset, tileY + yOffset)) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Check if a single tile is passable (can be walked/flown through).
        /// </summary>
        private static bool IsSingleTilePassable(int tileX, int tileY)
        {
            // Bounds check
            if (tileX < 0 || tileX >= Main.maxTilesX || tileY < 0 || tileY >= Main.maxTilesY) {
                return false;
            }

            Tile tile = Main.tile[tileX, tileY];

            // No tile = passable (air)
            if (!tile.HasTile) {
                return true;
            }

            // Platforms and other non-solid tiles are passable
            if (!Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) {
                return true;
            }

            // Actuated tiles (toggled off) are passable
            if (!tile.IsActuated) {
                return false;  // Solid and not actuated = blocked
            }

            return true;  // Actuated solid = passable
        }

        /// <summary>
        /// Debug helper: Find which sub-tile in the clearance area is blocking.
        /// </summary>
        private static string FindBlockingSubTile(int tileX, int tileY)
        {
            for (int xOffset = 0; xOffset < _currentClearanceWidthTiles; xOffset++) {
                for (int yOffset = 0; yOffset < _currentClearanceHeightTiles; yOffset++) {
                    int checkX = tileX + xOffset;
                    int checkY = tileY + yOffset;
                    if (!IsSingleTilePassable(checkX, checkY)) {
                        return $"({checkX},{checkY}) at offset +{xOffset},+{yOffset}";
                    }
                }
            }
            return "unknown";
        }

        /// <summary>
        /// Euclidean heuristic for A*.
        /// </summary>
        private static float Heuristic(Point from, Point to)
        {
            float dx = from.X - to.X;
            float dy = from.Y - to.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
    }
}