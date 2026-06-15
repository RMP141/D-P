using System;
using System.Collections.Generic;
using System.Linq;
using ConvoyManager.Combat;
using ConvoyManager.ECS;
using ConvoyManager.Utils;
using ConvoyManager.World;
using Unity.Collections;
using Unity.Entities;

namespace ConvoyManager.Travel
{
    public class RoutePlanner : IRoutePlanner
    {
        private readonly IWorldState _worldState;
        private readonly EntityManager _entityManager;
        private readonly ICaptainCollection _captainCollection;

        public RoutePlanner(IWorldState worldState, EntityManager entityManager, ICaptainCollection captainCollection)
        {
            _worldState = worldState ?? throw new ArgumentNullException(nameof(worldState));
            _entityManager = entityManager;
            _captainCollection = captainCollection;
        }

        public Entity CreateConvoy(int[] cityIndices, CargoItem[] cargoItems)
        {
            if (cityIndices == null || cityIndices.Length < 2 || cityIndices.Length > 5)
                throw new ArgumentException("Route must contain 2 to 5 cities.");

            foreach (int cityIdx in cityIndices)
                _ = _worldState.GetCity(cityIdx);

            // Compute and store the full hex path between consecutive cities
            var allSegments = new List<List<int>>();
            int totalPathLength = 0;
            for (int i = 0; i < cityIndices.Length - 1; i++)
            {
                int fromHex = _worldState.GetCity(cityIndices[i]).HexIndex;
                int toHex = _worldState.GetCity(cityIndices[i + 1]).HexIndex;
                var path = HexPathfinder.FindPath(_worldState, fromHex, toHex);
                if (path == null)
                    throw new InvalidOperationException(
                        $"No valid path between {_worldState.GetCity(cityIndices[i]).Name} and {_worldState.GetCity(cityIndices[i + 1]).Name} (water blocks the way)");

                // Remove the first hex (it's the same as the last hex of previous segment)
                // Keep the start city hex for the first segment, remove duplicates for subsequent
                if (i > 0 && path.Count > 0)
                    path.RemoveAt(0);

                allSegments.Add(path);
                totalPathLength += path.Count;
            }

            var entity = _entityManager.CreateEntity(
                ComponentType.ReadWrite<ConvoyTag>(),
                ComponentType.ReadWrite<MovementSpeed>(),
                ComponentType.ReadWrite<ConvoyStateComponent>(),
                ComponentType.ReadWrite<ResourceComponent>(),
                ComponentType.ReadWrite<RouteComponent>(),
                ComponentType.ReadWrite<CargoComponent>(),
                ComponentType.ReadWrite<EventTimerComponent>()
            );

            float baseSpeed = 1f;
            if (_captainCollection?.ActiveCaptain != null)
                baseSpeed += 5f;
            _entityManager.SetComponentData(entity, new MovementSpeed { Value = baseSpeed });
            _entityManager.SetComponentData(entity, new ResourceComponent { Food = 100f, Wear = 0f });
            _entityManager.SetComponentData(entity, new ConvoyStateComponent { State = ConvoyState.Traveling, Progress = 0f, CurrentHexIndex = 0 });
            _entityManager.SetComponentData(entity, new EventTimerComponent { TimeUntilCheck = 60f });

            // Store route as Blob with full hex path
            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                ref var routeBlob = ref blobBuilder.ConstructRoot<RouteBlob>();

                var citiesArray = blobBuilder.Allocate(ref routeBlob.CityIndices, cityIndices.Length);
                for (int i = 0; i < cityIndices.Length; i++)
                    citiesArray[i] = cityIndices[i];

                var hexPathArr = blobBuilder.Allocate(ref routeBlob.HexPath, totalPathLength);
                var speedArr = blobBuilder.Allocate(ref routeBlob.HexTerrainSpeeds, totalPathLength);
                var waypointsArr = blobBuilder.Allocate(ref routeBlob.CityWaypoints, cityIndices.Length);

                int idx = 0;
                int segIdx = 0;
                int waypointAccum = 0;
                foreach (var seg in allSegments)
                {
                    foreach (int hi in seg)
                    {
                        hexPathArr[idx] = hi;
                        speedArr[idx] = TerrainCost.GetSpeedMultiplier(_worldState.GetHex(hi).Terrain);
                        idx++;
                    }
                    waypointAccum += seg.Count;
                    waypointsArr[segIdx] = waypointAccum - 1;
                    segIdx++;
                }

                routeBlob.CurrentSegment = 0;
                _entityManager.SetComponentData(entity, new RouteComponent
                {
                    Blob = blobBuilder.CreateBlobAssetReference<RouteBlob>(Allocator.Persistent)
                });
            }

            // ���������� Blob-������ �����
            using (var blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                ref var cargoBlob = ref blobBuilder.ConstructRoot<CargoBlob>();
                var itemsArray = blobBuilder.Allocate(ref cargoBlob.Items, cargoItems.Length);
                for (int i = 0; i < cargoItems.Length; i++)
                {
                    itemsArray[i] = new CargoBlobItem
                    {
                        ItemId = cargoItems[i].ItemId,
                        Quantity = cargoItems[i].Quantity
                    };
                }
                _entityManager.SetComponentData(entity, new CargoComponent
                {
                    Blob = blobBuilder.CreateBlobAssetReference<CargoBlob>(Allocator.Persistent)
                });
            }

            return entity;
        }
    }
}