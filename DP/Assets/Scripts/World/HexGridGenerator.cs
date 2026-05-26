using ConvoyManager.Core;
using ConvoyManager.Data;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;
using UniRx;

namespace ConvoyManager.World
{
    /// <summary>
    /// Отвечает за визуализацию гексагональной сетки и тумана войны.
    /// Размещается на GameObject с Grid и Tilemap.
    /// </summary>
    public class HexGridGenerator : MonoBehaviour
    {
        [Inject] private IWorldState _worldState;
        [Inject] private EventBus _eventBus;
        [Inject] private HexTileConfig _tileConfig;

        private Tilemap _tilemap;
        private Grid _grid;

        private void Awake()
        {
            _grid = GetComponent<Grid>();
            _tilemap = GetComponentInChildren<Tilemap>();

            if (_tileConfig != null)
                ConfigureGrid();
        }

        private void ConfigureGrid()
        {
            _grid.cellLayout = _tileConfig.CellLayout;
            _grid.cellSwizzle = _tileConfig.CellSwizzle;
            _grid.cellSize = _tileConfig.CellSize;
        }

        private void Start()
        {
            if (_worldState == null || _tileConfig == null)
            {
                Debug.LogError("HexGridGenerator: зависимости не внедрены");
                return;
            }

            DrawAllHexes();
            _eventBus.Subscribe<HexDiscoveredEvent>().Subscribe(OnHexDiscovered).AddTo(this);
        }

        private void DrawAllHexes()
        {
            _tilemap.ClearAllTiles();

            var hexes = _worldState.Hexes;
            for (int i = 0; i < hexes.Count; i++)
            {
                var hex = hexes[i];
                Vector3Int cellPos = HexCoordinatesToCell(hex.Coordinates);
                TileBase tile = hex.IsDiscovered ? _tileConfig.HexTile : _tileConfig.FogTile;
                _tilemap.SetTile(cellPos, tile);
            }
        }

        private void OnHexDiscovered(HexDiscoveredEvent evt)
        {
            var hex = _worldState.GetHex(evt.HexIndex);
            Vector3Int cellPos = HexCoordinatesToCell(hex.Coordinates);
            _tilemap.SetTile(cellPos, _tileConfig.HexTile);
        }

        /// <summary>
        /// Преобразует координаты гекса (X,Y) в ячейку Tilemap (pointy-top, odd-row offset).
        /// </summary>
        private Vector3Int HexCoordinatesToCell(int2 coords)
        {
            int row = coords.y;
            int col = coords.x;
            int x = col - (row & 1) / 2; // odd-row offset
            int y = row;
            int z = -x - y;
            return new Vector3Int(x, y, z);
        }

        private void OnDestroy()
        {
            // Очистка подписок при уничтожении объекта
        }
    }
}