using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Компонент-ссылка на Blob с маршрутом.
    /// </summary>
    public struct RouteComponent : IComponentData
    {
        public BlobAssetReference<RouteBlob> Blob;
    }

    /// <summary>
    /// Неизменяемый (Blob) маршрут: индексы городов и текущий сегмент.
    /// </summary>
    public struct RouteBlob
    {
        public BlobArray<int> CityIndices;
        public int CurrentSegment;
    }
}