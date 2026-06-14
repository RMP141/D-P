using System.Collections.Generic;
using Unity.Mathematics;

namespace ConvoyManager.World
{
    /// <summary>
    /// ������ ����� (������� ����).
    /// </summary>
    public class Hex
    {
        public int Index;
        public int2 Coordinates;
        public bool IsDiscovered;
        public List<int> CityIndices = new List<int>();
    }
}