using Unity.Entities;

namespace ConvoyManager.Travel
{
    public interface IRoutePlanner
    {
        /// <summary>
        /// Создаёт ECS-сущность каравана с заданным маршрутом и грузом.
        /// </summary>
        /// <param name="cityIndices">Индексы городов в порядке посещения (от 2 до 5).</param>
        /// <param name="cargoItems">Товары для перевозки.</param>
        /// <returns>Созданная сущность.</returns>
        Entity CreateConvoy(int[] cityIndices, CargoItem[] cargoItems);
    }
}