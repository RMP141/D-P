using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace ConvoyManager.ECS
{
    [BurstCompile]
    public partial struct EventTimerSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<ConvoyTag, EventTimerComponent, ConvoyStateComponent>()
                .Build();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float checkInterval = 60f; // интервал проверки событий
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new TimerJob
            {
                DeltaTime = deltaTime,
                CheckInterval = checkInterval,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(_query, state.Dependency).Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        partial struct TimerJob : IJobEntity
        {
            public float DeltaTime;
            public float CheckInterval;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute([EntityIndexInChunk] int chunkIndex, Entity entity, ref EventTimerComponent timer, in ConvoyStateComponent state)
            {
                if (state.State != ConvoyState.Traveling) return;

                timer.TimeUntilCheck -= DeltaTime;
                if (timer.TimeUntilCheck <= 0f)
                {
                    timer.TimeUntilCheck += CheckInterval;
                    ECB.AddComponent<EventCheckTag>(chunkIndex, entity);
                }
            }
        }
    }
}