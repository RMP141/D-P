using System.Collections.Generic;
using ConvoyManager.Core;
using Unity.Mathematics;

namespace ConvoyManager.World
{
    public class WorldState : IWorldState
    {
        private readonly List<Hex> _hexes = new List<Hex>();
        private readonly List<City> _cities = new List<City>();
        private readonly EventBus _eventBus;

        public WorldState(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public IReadOnlyList<Hex> Hexes => _hexes;
        public IReadOnlyList<City> Cities => _cities;

        public City GetCity(int index) => _cities[index];
        public Hex GetHex(int index) => _hexes[index];

        public void Generate(int seed)
        {
            _hexes.Clear();
            _cities.Clear();

            var random = new Unity.Mathematics.Random((uint)seed);
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    var hex = new Hex
                    {
                        Coordinates = new int2(x, y),
                        IsDiscovered = (x == 1 && y == 1), // ������ ������ ����������� ����
                        CityIndices = new List<int>()
                    };

                    int cityCount = random.NextInt(3, 28);
                    for (int i = 0; i < cityCount; i++)
                    {
                        var city = new City
                        {
                            Index = _cities.Count,
                            HexIndex = _hexes.Count,
                            Faction = (Faction)random.NextInt(0, 11),
                            Name = $"City_{_cities.Count}"
                        };
                        _cities.Add(city);
                        hex.CityIndices.Add(city.Index);
                    }
                    _hexes.Add(hex);
                }
            }
        }

        public void LoadFrom(WorldStateData data)
        {
            _hexes.Clear();
            _cities.Clear();

            foreach (var hexData in data.Hexes)
            {
                var hex = new Hex
                {
                    Coordinates = hexData.Coordinates,
                    IsDiscovered = hexData.IsDiscovered,
                    CityIndices = new List<int>(hexData.CityIndices)
                };
                _hexes.Add(hex);
            }

            foreach (var cityData in data.Cities)
            {
                var city = new City
                {
                    Index = cityData.Index,
                    HexIndex = cityData.HexIndex,
                    Faction = cityData.Faction,
                    Name = cityData.Name
                };
                _cities.Add(city);
            }
        }

        public void DiscoverHex(int hexIndex)
        {
            var hex = _hexes[hexIndex];
            if (!hex.IsDiscovered)
            {
                hex.IsDiscovered = true;
                _eventBus.Publish(new HexDiscoveredEvent(hexIndex));
            }
        }
    }

}