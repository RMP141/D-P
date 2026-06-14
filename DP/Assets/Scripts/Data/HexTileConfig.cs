using UnityEngine;
using UnityEngine.Tilemaps;

namespace ConvoyManager.Data
{
    [CreateAssetMenu(fileName = "HexTileConfig", menuName = "ConvoyManager/Hex Tile Config")]
    public class HexTileConfig : ScriptableObject
    {
        [Header("Tiles")]
        public TileBase HexTile;
        public TileBase FogTile;

        [Header("Layout")]
        public GridLayout.CellLayout CellLayout = GridLayout.CellLayout.Rectangle;
        public GridLayout.CellSwizzle CellSwizzle = GridLayout.CellSwizzle.XYZ;
        public Vector3 CellSize = new Vector3(1, 1, 0);
    }
}