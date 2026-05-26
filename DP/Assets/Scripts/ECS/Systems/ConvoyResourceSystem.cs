using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace ConvoyManager.ECS
{
    [BurstCompile]
    public partial struct ConvoyResourceSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<ConvoyTag, ConvoyStateComponent, ResourceComponent>()
                .Build();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float foodConsumption = 0.5f;
            float wearRate = 0.1f;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new ResourceJob
            {
                DeltaTime = deltaTime,
                FoodConsumption = foodConsumption,
                WearRate = wearRate,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(_query, state.Dependency).Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        partial struct ResourceJob : IJobEntity
        {
            public float DeltaTime;
            public float FoodConsumption;
            public float WearRate;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute([EntityIndexInChunk] int chunkIndex, Entity entity, ref ResourceComponent resource, in ConvoyStateComponent state)
            {
                if (state.State != ConvoyState.Traveling) return;

                resource.Food -= FoodConsumption * DeltaTime;
                resource.Wear += WearRate * DeltaTime;

                if (resource.Food <= 0f)
                {
                    resource.Food = 0f;
                    ECB.AddComponent<OutOfFoodTag>(chunkIndex, entity);
                }
                if (resource.Wear >= 100f)
                {
                    resource.Wear = 100f;
                    ECB.AddComponent<BrokenTag>(chunkIndex, entity);
                }
            }
        }
    }
}