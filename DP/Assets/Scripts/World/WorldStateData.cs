using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace ConvoyManager.World
{
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
                    Terrain = hexes[i].Terrain,
                    IsDiscovered = hexes[i].IsDiscovered,
                    WorldPosition = hexes[i].WorldPosition,
                    CityIndices = hexes[i].CityIndices.ToArray()
                };
            }

            data.Cities = new CityData[cities.Count];
            for (int i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                data.Cities[i] = new CityData
                {
                    Index = city.Index,
                    HexIndex = city.HexIndex,
                    Faction = city.Faction,
                    Name = city.Name,
                    Type = city.Type,
                    MaxWeight = city.MaxWeight,
                    AvailableItemIds = city.AvailableItemIds.ToArray(),
                    StockKeys = city.Stock.Keys.ToArray(),
                    StockValues = city.Stock.Values.ToArray(),
                    CacheKeys = city.PlayerCache.Keys.ToArray(),
                    CacheValues = city.PlayerCache.Values.ToArray()
                };
            }

            return data;
        }
    }

    [System.Serializable]
    public struct HexData
    {
        public int2 Coordinates;
        public HexType Terrain;
        public bool IsDiscovered;
        public float3 WorldPosition;
        public int[] CityIndices;
    }

    [System.Serializable]
    public struct CityData
    {
        public int Index;
        public int HexIndex;
        public Faction Faction;
        public string Name;
        public SettlementType Type;
        public float MaxWeight;
        public int[] AvailableItemIds;
        public int[] StockKeys;
        public int[] StockValues;
        public int[] CacheKeys;
        public int[] CacheValues;
    }
}
