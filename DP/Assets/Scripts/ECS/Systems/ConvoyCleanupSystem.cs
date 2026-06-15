using Unity.Collections;
using Unity.Entities;

namespace ConvoyManager.ECS
{
    [UpdateAfter(typeof(ConvoyEventPublisherSystem))]
    public partial class ConvoyCleanupSystem : SystemBase
    {
        public NativeQueue<Entity> PendingDestroy;

        protected override void OnCreate()
        {
            PendingDestroy = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            if (PendingDestroy.Count == 0) return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            while (PendingDestroy.TryDequeue(out Entity e))
            {
                if (EntityManager.Exists(e))
                    ecb.DestroyEntity(e);
            }
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        protected override void OnDestroy()
        {
            if (PendingDestroy.IsCreated)
                PendingDestroy.Dispose();
        }
    }
}
