using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Тег: караван прибыл в следующий город.
    /// </summary>
    public struct ConvoyArrivedTag : IComponentData { }

    /// <summary>
    /// Тег: у каравана кончилась пища.
    /// </summary>
    public struct OutOfFoodTag : IComponentData { }

    /// <summary>
    /// Тег: караван сломался (износ 100).
    /// </summary>
    public struct BrokenTag : IComponentData { }

    /// <summary>
    /// Тег: пора проверить, произойдёт ли случайное событие.
    /// </summary>
    public struct EventCheckTag : IComponentData { }
}