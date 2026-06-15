using System.Collections.Generic;
using System.Linq;
using ConvoyManager.Core;
using ConvoyManager.Data;
using ConvoyManager.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace ConvoyManager.World
{
    public class WorldState : IWorldState
    {
        private readonly List<Hex> _hexes = new List<Hex>();
        private readonly List<City> _cities = new List<City>();
        private readonly EventBus _eventBus;
        private readonly GameConfig _gameConfig;
        private int _gridWidth = 30;
        private int _gridHeight = 30;

        public WorldState(EventBus eventBus, GameConfig gameConfig)
        {
            _eventBus = eventBus;
            _gameConfig = gameConfig;
        }

        public IReadOnlyList<Hex> Hexes => _hexes;
        public IReadOnlyList<City> Cities => _cities;

        public City GetCity(int index) => _cities[index];
        public Hex GetHex(int index) => _hexes[index];
        public int CenterHexIndex
        {
            get
            {
                int cx = (_gridWidth - 1) / 2;
                int cy = (_gridHeight - 1) / 2;
                return cx * _gridHeight + cy;
            }
        }

        public void Generate(int seed)
        {
            _hexes.Clear();
            _cities.Clear();

            var random = new Unity.Mathematics.Random((uint)seed);
            int centerX = (_gridWidth - 1) / 2;
            int centerY = (_gridHeight - 1) / 2;

            // Phase 1: generate hexes with terrain
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    float noiseVal = noise.cnoise(new float2(x * 0.08f, y * 0.08f));
                    var terrain = SampleTerrain(noiseVal);

                    // Force center hex to Plains so it can host the starting city
                    if (x == centerX && y == centerY)
                        terrain = HexType.Plains;

                    var hex = new Hex
                    {
                        Index = _hexes.Count,
                        Coordinates = new int2(x, y),
                        Terrain = terrain,
                        IsDiscovered = (x == centerX && y == centerY),
                        CityIndices = new List<int>()
                    };
                    _hexes.Add(hex);
                }
            }

            // Phase 2: place 9 cities (3 Human, 3 Dwarf, 3 Elf)
            var plainsIndices = Enumerable.Range(0, _hexes.Count)
                .Where(i => _hexes[i].Terrain == HexType.Plains)
                .ToList();

            // Place center hex first, then shuffle the rest
            int centerIndex = centerX * _gridHeight + centerY;
            plainsIndices.Remove(centerIndex);
            plainsIndices.Insert(0, centerIndex);

            for (int i = plainsIndices.Count - 1; i > 1; i--)
            {
                int j = random.NextInt(1, i + 1);
                int tmp = plainsIndices[i];
                plainsIndices[i] = plainsIndices[j];
                plainsIndices[j] = tmp;
            }

            var cityHexIndices = new List<int>();
            int[] minDistances = { 6, 4, 2 };

            for (int slot = 0; slot < 9; slot++)
            {
                int chosen = -1;
                foreach (int dist in minDistances)
                {
                    if (chosen >= 0) break;
                    chosen = plainsIndices.FirstOrDefault(pi =>
                        !cityHexIndices.Contains(pi) &&
                        cityHexIndices.All(existing => MathUtils.HexDistance(
                            _hexes[pi].Coordinates, _hexes[existing].Coordinates) >= dist));
                }
                if (chosen < 0)
                    chosen = plainsIndices.First(pi => !cityHexIndices.Contains(pi));

                cityHexIndices.Add(chosen);
            }

            var factionSlots = new[]
            {
                Faction.HumanKingdom1, Faction.HumanKingdom2, Faction.HumanKingdom3,
                Faction.Dwarves, Faction.Dwarves, Faction.Dwarves,
                Faction.Elves, Faction.Elves, Faction.Elves
            };

            var names = _gameConfig.CityNames;
            var usedNames = new HashSet<string>();

            for (int i = 0; i < 9; i++)
            {
                string cityName = PickUnusedName(names, usedNames, _hexes[cityHexIndices[i]].Coordinates, ref random);
                usedNames.Add(cityName);

                var city = new City
                {
                    Index = _cities.Count,
                    HexIndex = cityHexIndices[i],
                    Faction = factionSlots[i],
                    Name = cityName,
                    Type = SettlementType.City
                };
                _cities.Add(city);
                _hexes[cityHexIndices[i]].CityIndices.Add(city.Index);
            }

            // Phase 3: central city (closest to center) → starting city, discovered from start
            var centerCoord = new int2(centerX, centerY);
            int centralCityIdx = _cities
                .OrderBy(c => MathUtils.HexDistance(_hexes[c.HexIndex].Coordinates, centerCoord))
                .First().Index;

            var centralHex = _hexes[_cities[centralCityIdx].HexIndex];
            centralHex.IsDiscovered = true;

            // Phase 4: spawn 2 villages near the central city
            var villageCandidates = _hexes
                .Where(h => h.Terrain == HexType.Plains && h.CityIndices.Count == 0)
                .Where(h =>
                {
                    int d = MathUtils.HexDistance(h.Coordinates, centralHex.Coordinates);
                    return d >= 1 && d <= 2;
                })
                .ToList();

            for (int i = villageCandidates.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                var tmp = villageCandidates[i];
                villageCandidates[i] = villageCandidates[j];
                villageCandidates[j] = tmp;
            }

            int villagesToPlace = math.min(2, villageCandidates.Count);

            for (int i = 0; i < villagesToPlace; i++)
            {
                string villageName = PickUnusedName(names, usedNames, villageCandidates[i].Coordinates, ref random);
                usedNames.Add(villageName);

                var village = new City
                {
                    Index = _cities.Count,
                    HexIndex = villageCandidates[i].Index,
                    Faction = _cities[centralCityIdx].Faction,
                    Name = villageName,
                    Type = SettlementType.Village
                };
                _cities.Add(village);
                villageCandidates[i].CityIndices.Add(village.Index);
            }

            // Phase 5: assign random available item pools to each settlement
            var allItems = Resources.LoadAll<ItemDataSO>("Items").ToList();
            foreach (var city in _cities)
            {
                bool isCity = city.Type == SettlementType.City;
                int minItems = isCity ? 4 : 1;
                int maxItems = isCity ? 8 : 3;
                int count = random.NextInt(minItems, maxItems + 1);
                // Fisher-Yates shuffle of item indices using the main Random
                var shuffled = Enumerable.Range(0, allItems.Count).ToList();
                for (int i = shuffled.Count - 1; i > 0; i--)
                {
                    int j = random.NextInt(0, i + 1);
                    int tmp = shuffled[i];
                    shuffled[i] = shuffled[j];
                    shuffled[j] = tmp;
                }
                for (int i = 0; i < count && i < shuffled.Count; i++)
                    city.AvailableItemIds.Add(allItems[shuffled[i]].ID);

                // Each settlement has unique MaxWeight: cities 300-700, villages 100-300
                city.MaxWeight = isCity ? (float)random.NextInt(300, 701) : (float)random.NextInt(100, 301);
                int minQty = isCity ? 10 : 3;
                int maxQty = isCity ? 30 : 10;
                foreach (int itemId in city.AvailableItemIds)
                {
                    int qty = random.NextInt(minQty, maxQty + 1);
                    city.Stock[itemId] = qty;
                }
            }
        }

        private static string PickUnusedName(string[] pool, HashSet<string> used, int2 coord, ref Unity.Mathematics.Random random)
        {
            if (pool == null || pool.Length == 0)
                return $"Settlement_{coord.x}_{coord.y}";

            int attempts = 0;
            while (attempts < pool.Length * 2)
            {
                int idx = random.NextInt(0, pool.Length);
                if (!used.Contains(pool[idx]))
                    return pool[idx];
                attempts++;
            }
            return $"{pool[random.NextInt(0, pool.Length)]}_{coord.x}_{coord.y}";
        }

        private static HexType SampleTerrain(float noise)
        {
            if (noise < -0.15f) return HexType.Water;
            if (noise < 0.15f) return HexType.Plains;
            if (noise < 0.4f) return HexType.Forest;
            return HexType.Mountains;
        }

        public void LoadFrom(WorldStateData data)
        {
            _hexes.Clear();
            _cities.Clear();

            for (int i = 0; i < data.Hexes.Length; i++)
            {
                var hexData = data.Hexes[i];
                var hex = new Hex
                {
                    Index = i,
                    Coordinates = hexData.Coordinates,
                    Terrain = hexData.Terrain,
                    IsDiscovered = hexData.IsDiscovered,
                    WorldPosition = hexData.WorldPosition,
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
                    Name = cityData.Name,
                    Type = cityData.Type,
                    MaxWeight = cityData.MaxWeight
                };
                if (cityData.AvailableItemIds != null)
                    city.AvailableItemIds = new System.Collections.Generic.List<int>(cityData.AvailableItemIds);
                if (cityData.StockKeys != null && cityData.StockValues != null)
                    for (int si = 0; si < cityData.StockKeys.Length && si < cityData.StockValues.Length; si++)
                        city.Stock[cityData.StockKeys[si]] = cityData.StockValues[si];
                if (cityData.CacheKeys != null && cityData.CacheValues != null)
                    for (int ci = 0; ci < cityData.CacheKeys.Length && ci < cityData.CacheValues.Length; ci++)
                        city.PlayerCache[cityData.CacheKeys[ci]] = cityData.CacheValues[ci];
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
