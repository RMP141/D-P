using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Таймер обратного отсчёта до следующей проверки случайного события.
    /// </summary>
    public struct EventTimerComponent : IComponentData
    {
        public float TimeUntilCheck;
    }
}