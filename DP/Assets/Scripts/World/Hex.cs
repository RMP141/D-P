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
}
