using Unity.Entities;

namespace ConvoyManager.ECS
{
    /// <summary>
    /// ���������-������ �� Blob � ���������.
    /// </summary>
    public struct RouteComponent : IComponentData
    {
        public BlobAssetReference<RouteBlob> Blob;
    }

    /// <summary>
    /// ������������ (Blob) �������: �������� ��������� ��� (HexPath) � ��������� ������.
    /// CityIndices: [city0, city1, city2, ...] — �������� ����� ������.
    /// HexPath: ������ ������ hex-�� ����� �����, �������� [city0_hex, hex, ..., city1_hex, hex, ..., city2_hex].
    /// CurrentSegment: ����� ������� �������� (����� ����� ������).
    /// </summary>
    public struct RouteBlob
    {
        public BlobArray<int> CityIndices;          // [city0, city1, ...]
        public BlobArray<int> HexPath;              // Full hex path: [city0_hex, hex, ..., city1_hex, ...]
        public BlobArray<float> HexTerrainSpeeds;   // Speed multipliers parallel to HexPath
        public BlobArray<int> CityWaypoints;        // Indices in HexPath where city hexes are: [0, len-1, ...]
        public int CurrentSegment;
    }

    /// <summary>
    /// Helper to indicate the convoy has arrived at a city hex and should trigger arrival logic.
    /// Added by ConvoyMovementSystem when CurrentHexIndex reaches a city waypoint.
    /// </summary>
    public struct ConvoyArrivedAtCityTag : IComponentData { }
}