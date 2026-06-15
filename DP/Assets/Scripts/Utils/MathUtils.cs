using System;
using Unity.Mathematics;

namespace ConvoyManager.Utils
{
    /// <summary>
    /// ����� ����������� �������������� ������� ��� ����� ����������.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// �������� ������������ � ������������ ��������� �� 0 �� 1.
        /// </summary>
        public static float LerpProgress(float from, float to, float t)
        {
            return math.lerp(from, to, math.saturate(t));
        }

        /// <summary>
        /// ������������ ������ �� �����, �� �������� maxLength.
        /// </summary>
        public static float3 ClampVector(float3 vector, float maxLength)
        {
            float len = math.length(vector);
            if (len <= maxLength) return vector;
            return math.normalize(vector) * maxLength;
        }

        /// <summary>
        /// ��������� ���������� ����� ����� ������� � �������������� ����� (���������� ����������).
        /// </summary>
        public static int HexDistance(int3 a, int3 b)
        {
            return (math.abs(a.x - b.x) + math.abs(a.y - b.y) + math.abs(a.z - b.z)) / 2;
        }

        /// <summary>
        /// ��������� ���������� ����� ����� ������ offset-���������� (odd-r, �������� �����) � �������������� �����.
        /// </summary>
        public static int HexDistance(int2 a, int2 b)
        {
            int q1 = a.x - (a.y - (a.y & 1)) / 2;
            int r1 = a.y;
            int q2 = b.x - (b.y - (b.y & 1)) / 2;
            int r2 = b.y;
            return (math.abs(q1 - q2) + math.abs(r1 - r2) + math.abs(q1 + r1 - q2 - r2)) / 2;
        }
    }
}