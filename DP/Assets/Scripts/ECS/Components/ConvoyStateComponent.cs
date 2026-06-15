using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// ��������� ��������.
    /// </summary>
    public enum ConvoyState : byte
    {
        Idle,             // ������� ��������
        Traveling,        // � ����
        WaitingForInput   // ������ � �����, ��� �������� ������
    }

    /// <summary>
    /// ������� ��������� �������� � �������� ��������.
    /// Progress: 0..1 between HexPath[CurrentHexIndex] and HexPath[CurrentHexIndex+1].
    /// CurrentHexIndex: index into RouteBlob.HexPath.
    /// </summary>
    public struct ConvoyStateComponent : IComponentData
    {
        public ConvoyState State;
        public float Progress;
        public int CurrentHexIndex;
    }
}