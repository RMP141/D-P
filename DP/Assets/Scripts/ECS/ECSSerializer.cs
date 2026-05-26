using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Отвечает за сериализацию и десериализацию ECS-сущностей караванов.
    /// Работает с EntityManager, корректно освобождает и восстанавливает BlobAssetReference.
    /// </summary>
    public class ECSSerializer
    {
        private readonly EntityManager _entityManager;

        public ECSSerializer(EntityManager entityManager)
        {
            _entityManager = entityManager;
        }

        /// <summary>
        /// Сохраняет все сущности с компонентом ConvoyTag в сериализуемый объект ECSData.
        /// </summary>
        public ECSData Save()
        {
            var data = new ECSData();
            var query = _entityManager.CreateEntityQuery(typeof(ConvoyTag));
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var entry = new EntityData
                {
                    Position = _entityManager.GetComponentData<PositionComponent>(entity).Value,
                    Speed = _entityManager.GetComponentData<MovementSpeed>(entity).Value,
                    State = _entityManager.GetComponentData<ConvoyStateComponent>(entity).State,
                    Progress = _entityManager.GetComponentData<ConvoyStateComponent>(entity).Progress,
                    Food = _entityManager.GetComponentData<ResourceComponent>(entity).Food,
                    Wear = _entityManager.GetComponentData<ResourceComponent>(entity).Wear
                };

                // Сериализация груза (Blob)
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

                // Сериализация маршрута (Blob)
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
        /// Загружает сущности из сохранённых данных, удаляя все текущие караваны.
        /// Blob-ассеты создаются заново через BlobBuilder.
        /// </summary>
        public void Load(ECSData data)
        {
            // Удаляем все существующие караваны
            var query = _entityManager.CreateEntityQuery(typeof(ConvoyTag));
            _entityManager.DestroyEntity(query);

            foreach (var entry in data.Entities)
            {
                var entity = _entityManager.CreateEntity(
                    typeof(ConvoyTag),
                    typeof(PositionComponent),
                    typeof(MovementSpeed),
                    typeof(ConvoyStateComponent),
                    typeof(ResourceComponent),
                    typeof(CargoComponent),
                    typeof(RouteComponent),
                    typeof(EventTimerComponent)
                );

                _entityManager.SetComponentData(entity, new PositionComponent { Value = entry.Position });
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
                    TimeUntilCheck = 60f // Сбрасываем таймер при загрузке
                });

                // Восстановление груза (Blob)
                if (entry.Cargo != null && entry.Cargo.Length > 0)
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

                // Восстановление маршрута (Blob)
                if (entry.RouteCityIndices != null && entry.RouteCityIndices.Length > 0)
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
    /// Контейнер для сериализации всех ECS-сущностей.
    /// </summary>
    [System.Serializable]
    public class ECSData
    {
        public List<EntityData> Entities = new List<EntityData>();
    }

    /// <summary>
    /// Данные одной сущности каравана в сериализуемом виде.
    /// </summary>
    [System.Serializable]
    public class EntityData
    {
        public float3 Position;
        public float Speed;
        public ConvoyState State;
        public float Progress;
        public float Food;
        public float Wear;
        public CargoItemSerialized[] Cargo;
        public int[] RouteCityIndices;
        public int CurrentSegment;
    }

    /// <summary>
    /// Сериализуемая запись о товаре в грузе.
    /// </summary>
    [System.Serializable]
    public struct CargoItemSerialized
    {
        public int ItemId;
        public int Quantity;
    }
}