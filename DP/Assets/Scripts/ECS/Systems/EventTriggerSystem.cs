using Unity.Entities;
using Unity.Collections;
using ConvoyManager.Core;
using ConvoyManager.Data;
using Unity.Mathematics;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Обрабатывает тег EventCheckTag, с заданной вероятностью выбирает случайное событие и публикует EventTriggeredMessage.
    /// </summary>
    [UpdateAfter(typeof(EventTimerSystem))]
    public partial class EventTriggerSystem : SystemBase
    {
        public EventBus EventBus { get; set; }
        public EventDataSO[] EventPool { get; set; }
        public float Probability { get; set; } = 0.3f;

        protected override void OnUpdate()
        {
            if (EventBus == null || EventPool == null || EventPool.Length == 0) return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            uint seed = (uint)System.DateTime.Now.Ticks;
            var rand = new Random(seed);

            Entities
                .WithAll<EventCheckTag, ConvoyTag>()
                .ForEach((Entity entity) =>
                {
                    if (rand.NextFloat() < Probability)
                    {
                        var eventData = EventPool[rand.NextInt(0, EventPool.Length)];
                        EventBus.Publish(new EventTriggeredMessage(entity, eventData));
                    }
                    ecb.RemoveComponent<EventCheckTag>(entity);
                }).WithoutBurst().Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}