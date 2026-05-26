using System;
using Unity.Mathematics;

namespace ConvoyManager.Utils
{
    /// <summary>
    /// Набор статических математических функций для общих вычислений.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Линейная интерполяция с ограничением прогресса от 0 до 1.
        /// </summary>
        public static float LerpProgress(float from, float to, float t)
        {
            return math.lerp(from, to, math.saturate(t));
        }

        /// <summary>
        /// Ограничивает вектор по длине, не превышая maxLength.
        /// </summary>
        public static float3 ClampVector(float3 vector, float maxLength)
        {
            float len = math.length(vector);
            if (len <= maxLength) return vector;
            return math.normalize(vector) * maxLength;
        }

        /// <summary>
        /// Вычисляет расстояние между двумя точками в гексагональной сетке (кубические координаты).
        /// </summary>
        public static int HexDistance(int3 a, int3 b)
        {
            return (math.abs(a.x - b.x) + math.abs(a.y - b.y) + math.abs(a.z - b.z)) / 2;
        }
    }
}