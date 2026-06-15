using System.Collections.Generic;

namespace ConvoyManager.World
{
    public interface IWorldState
    {
        IReadOnlyList<Hex> Hexes { get; }
        IReadOnlyList<City> Cities { get; }
        City GetCity(int cityIndex);
        Hex GetHex(int hexIndex);
        int CenterHexIndex { get; }
        void Generate(int seed);
        void LoadFrom(WorldStateData data);
        void DiscoverHex(int hexIndex);
    }
}