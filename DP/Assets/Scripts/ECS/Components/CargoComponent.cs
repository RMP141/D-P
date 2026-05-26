using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// Компонент-ссылка на Blob с данными о грузе.
    /// </summary>
    public struct CargoComponent : IComponentData
    {
        public BlobAssetReference<CargoBlob> Blob;
    }

    /// <summary>
    /// Неизменяемый (Blob) массив товаров в караване.
    /// </summary>
    public struct CargoBlob
    {
        public BlobArray<CargoBlobItem> Items;
    }

    /// <summary>
    /// Запись о товаре в Blob.
    /// </summary>
    public struct CargoBlobItem
    {
        public int ItemId;
        public int Quantity;
    }
}