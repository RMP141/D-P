using UnityEngine;

namespace ConvoyManager.Utils
{
    /// <summary>
    /// Реализация IRandomGenerator на основе UnityEngine.Random.
    /// </summary>
    public class UnityRandomGenerator : IRandomGenerator
    {
        public float Range(float min, float max) => Random.Range(min, max);
        public int Range(int min, int max) => Random.Range(min, max);
    }
}