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
                .WithAll<ConvoyTag, ConvoyStateComponent, MovementSpeed, RouteComponent>()
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

            void Execute([EntityIndexInChunk] int chunkIndex, Entity entity,
                ref ConvoyStateComponent stateComp, in MovementSpeed speed, in RouteComponent route)
            {
                if (stateComp.State != ConvoyState.Traveling) return;
                if (!route.Blob.IsCreated) return;

                ref var blob = ref route.Blob.Value;
                if (blob.HexPath.Length < 2) return;

                // If at the last hex in path, route complete
                if (stateComp.CurrentHexIndex >= blob.HexPath.Length - 1)
                {
                    stateComp.State = ConvoyState.WaitingForInput;
                    stateComp.Progress = 0f;
                    ECB.AddComponent<ConvoyArrivedTag>(chunkIndex, entity);
                    return;
                }

                // Terrain-aware movement: speed * terrainMultiplier * deltaTime
                float terrainSpeed = blob.HexTerrainSpeeds.Length > stateComp.CurrentHexIndex
                    ? blob.HexTerrainSpeeds[stateComp.CurrentHexIndex]
                    : 1f;

                stateComp.Progress += speed.Value * terrainSpeed * DeltaTime;

                if (stateComp.Progress >= 1f)
                {
                    stateComp.Progress = 0f;
                    stateComp.CurrentHexIndex++;

                    // Check if the new hex is a city waypoint
                    for (int i = 0; i < blob.CityWaypoints.Length; i++)
                    {
                        if (blob.CityWaypoints[i] == stateComp.CurrentHexIndex)
                        {
                            stateComp.State = ConvoyState.WaitingForInput;
                            ECB.AddComponent<ConvoyArrivedAtCityTag>(chunkIndex, entity);
                            if (i == blob.CityWaypoints.Length - 1)
                                ECB.AddComponent<ConvoyArrivedTag>(chunkIndex, entity);
                            return;
                        }
                    }
                }
            }
        }
    }
}