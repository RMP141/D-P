using System.Collections.Generic;
using Unity.Mathematics;

namespace ConvoyManager.World
{
    /// <summary>
    /// Контейнер для сериализации WorldState.
    /// </summary>
    [System.Serializable]
    public class WorldStateData
    {
        public HexData[] Hexes;
        public CityData[] Cities;

        public static WorldStateData FromWorldState(IWorldState worldState)
        {
            var data = new WorldStateData();
            var hexes = worldState.Hexes;
            var cities = worldState.Cities;

            data.Hexes = new HexData[hexes.Count];
            for (int i = 0; i < hexes.Count; i++)
            {
                data.Hexes[i] = new HexData
                {
                    Coordinates = hexes[i].Coordinates,
                    IsDiscovered = hexes[i].IsDiscovered,
                    CityIndices = hexes[i].CityIndices.ToArray()
                };
            }

            data.Cities = new CityData[cities.Count];
            for (int i = 0; i < cities.Count; i++)
            {
                data.Cities[i] = new CityData
                {
                    Index = cities[i].Index,
                    HexIndex = cities[i].HexIndex,
                    Faction = cities[i].Faction,
                    Name = cities[i].Name
                };
            }

            return data;
        }
    }

    [System.Serializable]
    public struct HexData
    {
        public int2 Coordinates;
        public bool IsDiscovered;
        public int[] CityIndices;
    }

    [System.Serializable]
    public struct CityData
    {
        public int Index;
        public int HexIndex;
        public Faction Faction;
        public string Name;
    }
}