using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace ConvoyManager.ECS
{
    [BurstCompile]
    public partial struct ConvoyMovementSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = SystemAPI.QueryBuilder()
                .WithAll<ConvoyTag, ConvoyStateComponent, MovementSpeed>()
                .Build();
            state.RequireForUpdate(_query);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            new MoveJob
            {
                DeltaTime = deltaTime,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(_query, state.Dependency).Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        partial struct MoveJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute([EntityIndexInChunk] int chunkIndex, Entity entity, ref ConvoyStateComponent stateComp, in MovementSpeed speed)
            {
                if (stateComp.State != ConvoyState.Traveling) return;

                stateComp.Progress += speed.Value * DeltaTime;
                if (stateComp.Progress >= 1f)
                {
                    stateComp.Progress = 0f;
                    stateComp.State = ConvoyState.WaitingForInput;
                    ECB.AddComponent<ConvoyArrivedTag>(chunkIndex, entity);
                }
            }
        }
    }
}