using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Состояния каравана.
    /// </summary>
    public enum ConvoyState : byte
    {
        Idle,             // Ожидает отправки
        Traveling,        // В пути
        WaitingForInput   // Прибыл в город, ждёт действий игрока
    }

    /// <summary>
    /// Текущее состояние каравана и прогресс движения по текущему сегменту маршрута (0..1).
    /// </summary>
    public struct ConvoyStateComponent : IComponentData
    {
        public ConvoyState State;
        public float Progress;
    }
}