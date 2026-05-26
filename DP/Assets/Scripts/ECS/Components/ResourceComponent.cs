using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Запасы пищи и износ повозок каравана.
    /// </summary>
    public struct ResourceComponent : IComponentData
    {
        public float Food;   // Текущий запас пищи
        public float Wear;   // Износ (0..100)
    }
}