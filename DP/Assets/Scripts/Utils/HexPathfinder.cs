using System.Collections.Generic;
using ConvoyManager.World;

namespace ConvoyManager.Utils
{
    /// <summary>
    /// A* pathfinder on the hex grid that avoids water and respects terrain costs.
    /// </summary>
    public static class HexPathfinder
    {
        private const int GridWidth = 30;
        private const int GridHeight = 30;

        /// <summary>
        /// Find a path of hex indices from startHexIndex to endHexIndex avoiding water.
        /// Uses terrain movement costs (Plains=1, Forest=1.5, Mountains=3).
        /// Returns null if no path exists.
        /// </summary>
        public static List<int> FindPath(IWorldState world, int startHexIndex, int endHexIndex)
        {
            var openSet = new List<(int index, float fCost)>();
            var cameFrom = new Dictionary<int, int>();
            var gCosts = new Dictionary<int, float>();
            var closedSet = new HashSet<int>();

            gCosts[startHexIndex] = 0;
            float startH = HexDistance(world, startHexIndex, endHexIndex);
            openSet.Add((startHexIndex, startH));

            while (openSet.Count > 0)
            {
                openSet.Sort((a, b) => a.fCost.CompareTo(b.fCost));
                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.index == endHexIndex)
                    return ReconstructPath(cameFrom, current.index);

                closedSet.Add(current.index);

                foreach (var neighbor in GetNeighbors(world, current.index))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    float terrainCost = TerrainCost.GetCost(world.GetHex(neighbor).Terrain);
                    if (terrainCost < 0)
                        continue;

                    float tentativeG = gCosts[current.index] + terrainCost;

                    if (!gCosts.ContainsKey(neighbor) || tentativeG < gCosts[neighbor])
                    {
                        cameFrom[neighbor] = current.index;
                        gCosts[neighbor] = tentativeG;
                        float h = HexDistance(world, neighbor, endHexIndex);
                        float f = tentativeG + h;

                        int existingIdx = openSet.FindIndex(x => x.index == neighbor);
                        if (existingIdx >= 0)
                            openSet[existingIdx] = (neighbor, f);
                        else
                            openSet.Add((neighbor, f));
                    }
                }
            }

            return null;
        }

        private static int HexCoordinateToIndex(int x, int y)
        {
            return x * GridHeight + y;
        }

        private static float HexDistance(IWorldState world, int a, int b)
        {
            return MathUtils.HexDistance(
                world.GetHex(a).Coordinates,
                world.GetHex(b).Coordinates);
        }

        private static List<int> GetNeighbors(IWorldState world, int hexIndex)
        {
            var hex = world.GetHex(hexIndex);
            int x = hex.Coordinates.x;
            int y = hex.Coordinates.y;
            bool oddRow = (y & 1) == 1;

            int[][] offsetsEven = new int[][]
            {
                new int[] { -1, 0 }, new int[] { 1, 0 },
                new int[] { 0, -1 }, new int[] { -1, -1 },
                new int[] { 0, 1 }, new int[] { -1, 1 }
            };

            int[][] offsetsOdd = new int[][]
            {
                new int[] { -1, 0 }, new int[] { 1, 0 },
                new int[] { 1, -1 }, new int[] { 0, -1 },
                new int[] { 1, 1 }, new int[] { 0, 1 }
            };

            var offsets = oddRow ? offsetsOdd : offsetsEven;
            var neighbors = new List<int>();

            foreach (var o in offsets)
            {
                int nx = x + o[0];
                int ny = y + o[1];
                if (nx >= 0 && nx < GridWidth && ny >= 0 && ny < GridHeight)
                    neighbors.Add(HexCoordinateToIndex(nx, ny));
            }

            return neighbors;
        }

        private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            var path = new List<int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }
    }
}
