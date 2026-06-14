using System.Linq;
using ConvoyManager.Core;
using ConvoyManager.Data;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer;
using VContainer.Unity;
using UniRx;

namespace ConvoyManager.World
{
    /// <summary>
    /// �������� �� ������������ �������������� ����� � ������ �����.
    /// ����������� �� GameObject � Grid � Tilemap.
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
        }

        private void ConfigureGrid()
        {
            _grid.cellLayout = _tileConfig.CellLayout;
            _grid.cellSwizzle = _tileConfig.CellSwizzle;
            _grid.cellSize = _tileConfig.CellSize;
        }

        private void Start()
        {
            if (_worldState == null || _eventBus == null || _tileConfig == null)
            {
                var scope = GetComponentInParent<LifetimeScope>();
                if (scope != null)
                    scope.Container.Inject(this);
            }

            if (_worldState == null || _tileConfig == null)
            {
                Debug.LogError("HexGridGenerator: ����������� �� ��������");
                return;
            }

            ConfigureGrid();

            if (_worldState.Hexes.Count > 0)
                DrawAllHexes();
            else
                _eventBus.Subscribe<GameStartedEvent>().Subscribe(_ => DrawAllHexes()).AddTo(this);

            _eventBus.Subscribe<HexDiscoveredEvent>().Subscribe(OnHexDiscovered).AddTo(this);
            _eventBus.Subscribe<GameLoadedEvent>().Subscribe(_ => DrawAllHexes()).AddTo(this);
        }

        public void Redraw()
        {
            DrawAllHexes();
            var renderer = _tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
                renderer.enabled = true;
            }
        }

        private void DrawAllHexes()
        {
            if (_tilemap == null || _tileConfig == null)
            {
                Debug.LogError("[HexGridGenerator] tilemap or config null");
                return;
            }

            bool hexOk = _tileConfig.HexTile != null;
            bool fogOk = _tileConfig.FogTile != null;
            Debug.Log($"[HexGridGenerator] HexTile={( hexOk ? _tileConfig.HexTile.name : "NULL")} FogTile={( fogOk ? _tileConfig.FogTile.name : "NULL")}");

            if (!hexOk || !fogOk)
            {
                var missingTiles = Resources.LoadAll<TileBase>("Tiles");
                if (!hexOk && missingTiles.Length > 0) _tileConfig.HexTile = missingTiles[0];
                if (!fogOk && missingTiles.Length > 1) _tileConfig.FogTile = missingTiles[1];
                hexOk = _tileConfig.HexTile != null;
                fogOk = _tileConfig.FogTile != null;
            }

            var hexes = _worldState.Hexes;
            var positions = new Vector3Int[hexes.Count];
            var tiles = new TileBase[hexes.Count];
            for (int i = 0; i < hexes.Count; i++)
            {
                positions[i] = HexCoordinatesToCell(hexes[i].Coordinates);
                tiles[i] = hexes[i].IsDiscovered ? _tileConfig.HexTile : _tileConfig.FogTile;
            }
            _tilemap.SetTiles(positions, tiles);
            _tilemap.RefreshAllTiles();

            int discoveredCount = hexes.Count(h => h.IsDiscovered);
            int usedCount = _tilemap.GetUsedTilesCount();
            Debug.Log($"[HexGridGenerator] DrawAllHexes: {discoveredCount}/{hexes.Count} discovered | tiles set: {usedCount}");
        }

        public void SetMissingTiles()
        {
            if (_tilemap == null || _tileConfig == null) return;
            var hexes = _worldState.Hexes;
            for (int i = 0; i < hexes.Count; i++)
            {
                var hex = hexes[i];
                Vector3Int cellPos = HexCoordinatesToCell(hex.Coordinates);
                var existing = _tilemap.GetTile(cellPos);
                if (existing == null)
                {
                    TileBase tile = hex.IsDiscovered ? _tileConfig.HexTile : _tileConfig.FogTile;
                    if (tile != null)
                        _tilemap.SetTile(cellPos, tile);
                }
            }
            _tilemap.RefreshAllTiles();
        }

        private void OnHexDiscovered(HexDiscoveredEvent evt)
        {
            var hex = _worldState.GetHex(evt.HexIndex);
            Vector3Int cellPos = HexCoordinatesToCell(hex.Coordinates);
            _tilemap.SetTile(cellPos, _tileConfig.HexTile);
        }

        /// <summary>
        /// ����������� ���������� ����� (X,Y) � ������ Tilemap (pointy-top, odd-row offset).
        /// </summary>
        private Vector3Int HexCoordinatesToCell(int2 coords)
        {
            return new Vector3Int(coords.x, coords.y, 0);
        }

        private void OnDestroy()
        {
            // ������� �������� ��� ����������� �������
        }
    }
}