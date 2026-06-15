using UnityEngine;
using UnityEngine.Tilemaps;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "HexTileConfig", menuName = "ConvoyManager/Hex Tile Config")]
    public class HexTileConfig : ScriptableObject
    {
        [Header("Terrain Tiles")]
        public TileBase PlainsTile;
        public TileBase ForestTile;
        public TileBase MountainsTile;
        public TileBase WaterTile;
        public TileBase FogTile;

        [Header("Settlement Tiles")]
        public TileBase CityTile;
        public TileBase VillageTile;

        [Header("Layout")]
        public GridLayout.CellLayout CellLayout = GridLayout.CellLayout.Hexagon;
        public GridLayout.CellSwizzle CellSwizzle = GridLayout.CellSwizzle.XYZ;
        public Vector3 CellSize = new Vector3(1f, 1.3f, 0);

        [Header("Generation")]
        public float NoiseScale = 0.08f;
        public float WaterThreshold = -0.15f;
        public float ForestThreshold = 0.15f;
        public float MountainThreshold = 0.4f;
    }
}
