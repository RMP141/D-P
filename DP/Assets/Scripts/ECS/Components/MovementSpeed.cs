using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// —корость перемещени€ каравана (единиц в секунду).
    /// </summary>
    public struct MovementSpeed : IComponentData
    {
        public float Value;
    }
}