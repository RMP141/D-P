using System.Collections.Generic;
using Unity.Mathematics;

namespace ConvoyManager.World
{
    public enum HexType
    {
        Plains,
        Forest,
        Mountains,
        Water
    }

    public class Hex
    {
        public int Index;
        public int2 Coordinates;
        public bool IsDiscovered;
        public HexType Terrain;
        public float3 WorldPosition;
        public List<int> CityIndices = new List<int>();
    }

    public static class TerrainCost
    {
        /// <summary>
        /// Movement cost multiplier per hex type.
        /// Higher = slower to traverse. Plains = 1.0 baseline.
        /// Water returns -1 (impassable).
        /// </summary>
        public static float GetCost(HexType terrain)
        {
            switch (terrain)
            {
                case HexType.Plains: return 1.0f;
                case HexType.Forest: return 1.5f;
                case HexType.Mountains: return 3.0f;
                case HexType.Water: return -1f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Speed multiplier while moving across this terrain (inverse of cost).
        /// Plains = 1.0 (normal speed), Forest = 0.67, Mountains = 0.33.
        /// </summary>
        public static float GetSpeedMultiplier(HexType terrain)
        {
            float cost = GetCost(terrain);
            return cost > 0 ? 1f / cost : 0f;
        }
    }
}
