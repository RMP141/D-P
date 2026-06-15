using Unity.Entities;
using Unity.Collections;
using ConvoyManager.Core;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// ������������ ���� �������, ����������� ������� ���������, � ��������� �� ����� EventBus.
    /// �� �������� Burst, ��� ��� �������� �� ��������� EventBus.
    /// </summary>
    [UpdateAfter(typeof(ConvoyMovementSystem))]
    [UpdateAfter(typeof(ConvoyResourceSystem))]
    public partial class ConvoyEventPublisherSystem : SystemBase
    {
        public EventBus EventBus { get; set; }

        protected override void OnUpdate()
        {
            if (EventBus == null) return;

            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            Entities
                .WithAll<ConvoyArrivedTag, ConvoyTag>()
                .ForEach((Entity entity) =>
                {
                    EventBus.Publish(new ConvoyArrivedEvent(entity));
                    ecb.RemoveComponent<ConvoyArrivedTag>(entity);
                }).WithoutBurst().Run();
            Entities
                .WithAll<ConvoyArrivedAtCityTag, ConvoyTag>()
                .ForEach((Entity entity) =>
                {
                    EventBus.Publish(new ConvoyArrivedAtCityEvent(entity));
                    ecb.RemoveComponent<ConvoyArrivedAtCityTag>(entity);
                }).WithoutBurst().Run();
            Entities
                .WithAll<OutOfFoodTag, ConvoyTag>()
                .ForEach((Entity entity) =>
                {
                    EventBus.Publish(new OutOfFoodEvent(entity));
                    ecb.RemoveComponent<OutOfFoodTag>(entity);
                }).WithoutBurst().Run();
            Entities
                .WithAll<BrokenTag, ConvoyTag>()
                .ForEach((Entity entity) =>
                {
                    EventBus.Publish(new BrokenEvent(entity));
                    ecb.RemoveComponent<BrokenTag>(entity);
                }).WithoutBurst().Run();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}