namespace ConvoyManager.Utils
{
    /// <summary>
    /// Интерфейс генератора случайных чисел, чтобы можно было подменять реализацию (например, для тестов).
    /// </summary>
    public interface IRandomGenerator
    {
        float Range(float min, float max);
        int Range(int min, int max);
    }
}