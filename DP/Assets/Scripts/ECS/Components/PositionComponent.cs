using Unity.Entities;
using Unity.Mathematics;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// “екуща€ позици€ каравана в мировом пространстве.
    /// </summary>
    public struct PositionComponent : IComponentData
    {
        public float3 Value;
    }
}