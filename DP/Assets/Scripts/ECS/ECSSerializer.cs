using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// �������� �� ������������ � �������������� ECS-��������� ���������.
    /// �������� � EntityManager, ��������� ����������� � ��������������� BlobAssetReference.
    /// </summary>
    public class ECSSerializer
    {
        private readonly EntityManager _entityManager;

        public ECSSerializer(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        /// <summary>
        /// ��������� ��� �������� � ����������� ConvoyTag � ������������� ������ ECSData.
        /// </summary>
        public ECSData Save()
        {
            var data = new ECSData();
            var query = _entityManager.CreateEntityQuery(typeof(ConvoyTag));
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var entry = new EntityData();

                if (_entityManager.HasComponent<MovementSpeed>(entity))
                    entry.Speed = _entityManager.GetComponentData<MovementSpeed>(entity).Value;
                if (_entityManager.HasComponent<ConvoyStateComponent>(entity))
                {
                    var state = _entityManager.GetComponentData<ConvoyStateComponent>(entity);
                    entry.State = state.State;
                    entry.Progress = state.Progress;
                }
                if (_entityManager.HasComponent<ResourceComponent>(entity))
                {
                    var res = _entityManager.GetComponentData<ResourceComponent>(entity);
                    entry.Food = res.Food;
                    entry.Wear = res.Wear;
                }
                if (_entityManager.HasComponent<EventTimerComponent>(entity))
                    entry.TimeUntilCheck = _entityManager.GetComponentData<EventTimerComponent>(entity).TimeUntilCheck;

                if (_entityManager.HasComponent<CargoComponent>(entity))
                {
                    var cargoComp = _entityManager.GetComponentData<CargoComponent>(entity);
                    if (cargoComp.Blob.IsCreated)
                    {
                        ref var cargo = ref cargoComp.Blob.Value;
                        var items = new CargoItemSerialized[cargo.Items.Length];
                        for (int i = 0; i < cargo.Items.Length; i++)
                        {
                            items[i] = new CargoItemSerialized
                            {
                                ItemId = cargo.Items[i].ItemId,
                                Quantity = cargo.Items[i].Quantity
                            };
                        }
                        entry.Cargo = items;
                    }
                }

                if (_entityManager.HasComponent<RouteComponent>(entity))
                {
                    var routeComp = _entityManager.GetComponentData<RouteComponent>(entity);
                    if (routeComp.Blob.IsCreated)
                    {
                        ref var route = ref routeComp.Blob.Value;
                        entry.RouteCityIndices = route.CityIndices.ToArray();
                        entry.CurrentSegment = route.CurrentSegment;
                    }
                }

                data.Entities.Add(entry);
            }

            entities.Dispose();
            return data;
        }

        /// <summary>
        /// ��������� �������� �� ����������� ������, ������ ��� ������� ��������.
        /// Blob-������ ��������� ������ ����� BlobBuilder.
        /// </summary>
        public void Load(ECSData data)
        {
            // ������� ��� ������������ ��������
            var query = _entityManager.CreateEntityQuery(typeof(ConvoyTag));
            _entityManager.DestroyEntity(query);

            foreach (var entry in data.Entities)
            {
                var compTypes = new List<ComponentType>
                {
                    typeof(ConvoyTag),
                    typeof(MovementSpeed),
                    typeof(ConvoyStateComponent),
                    typeof(ResourceComponent),
                    typeof(EventTimerComponent)
                };

                bool hasCargo = entry.Cargo != null && entry.Cargo.Length > 0;
                bool hasRoute = entry.RouteCityIndices != null && entry.RouteCityIndices.Length > 0;
                if (hasCargo) compTypes.Add(typeof(CargoComponent));
                if (hasRoute) compTypes.Add(typeof(RouteComponent));

                var entity = _entityManager.CreateEntity(compTypes.ToArray());

                _entityManager.SetComponentData(entity, new MovementSpeed { Value = entry.Speed });
                _entityManager.SetComponentData(entity, new ConvoyStateComponent
                {
                    State = entry.State,
                    Progress = entry.Progress
                });
                _entityManager.SetComponentData(entity, new ResourceComponent
                {
                    Food = entry.Food,
                    Wear = entry.Wear
                });
                _entityManager.SetComponentData(entity, new EventTimerComponent
                {
                    TimeUntilCheck = entry.TimeUntilCheck
                });

                if (hasCargo)
                {
                    using var blobBuilder = new BlobBuilder(Allocator.Temp);
                    ref var cargoBlob = ref blobBuilder.ConstructRoot<CargoBlob>();
                    var items = blobBuilder.Allocate(ref cargoBlob.Items, entry.Cargo.Length);
                    for (int i = 0; i < entry.Cargo.Length; i++)
                    {
                        items[i] = new CargoBlobItem
                        {
                            ItemId = entry.Cargo[i].ItemId,
                            Quantity = entry.Cargo[i].Quantity
                        };
                    }
                    _entityManager.SetComponentData(entity, new CargoComponent
                    {
                        Blob = blobBuilder.CreateBlobAssetReference<CargoBlob>(Allocator.Persistent)
                    });
                }

                if (hasRoute)
                {
                    using var blobBuilder = new BlobBuilder(Allocator.Temp);
                    ref var routeBlob = ref blobBuilder.ConstructRoot<RouteBlob>();
                    var cities = blobBuilder.Allocate(ref routeBlob.CityIndices, entry.RouteCityIndices.Length);
                    for (int i = 0; i < entry.RouteCityIndices.Length; i++)
                        cities[i] = entry.RouteCityIndices[i];
                    routeBlob.CurrentSegment = entry.CurrentSegment;

                    _entityManager.SetComponentData(entity, new RouteComponent
                    {
                        Blob = blobBuilder.CreateBlobAssetReference<RouteBlob>(Allocator.Persistent)
                    });
                }
            }
        }
    }

    /// <summary>
    /// ��������� ��� ������������ ���� ECS-���������.
    /// </summary>
    [System.Serializable]
    public class ECSData
    {
        public List<EntityData> Entities = new List<EntityData>();
    }

    /// <summary>
    /// ������ ����� �������� �������� � ������������� ����.
    /// </summary>
    [System.Serializable]
    public class EntityData
    {
        public float Speed;
        public ConvoyState State;
        public float Progress;
        public float Food;
        public float Wear;
        public float TimeUntilCheck;
        public CargoItemSerialized[] Cargo;
        public int[] RouteCityIndices;
        public int CurrentSegment;
    }

    /// <summary>
    /// ������������� ������ � ������ � �����.
    /// </summary>
    [System.Serializable]
    public struct CargoItemSerialized
    {
        public int ItemId;
        public int Quantity;
    }
}