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
            _grid.cellLayout = GridLayout.CellLayout.Hexagon;
            _grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            _grid.cellSize = new Vector3(1f, 1.3f, 0);
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
                Debug.LogError("HexGridGenerator: dependencies not resolved");
                return;
            }

            ConfigureGrid();

            // Attach CameraPan to main camera for edge-scroll
            if (Camera.main != null && Camera.main.GetComponent<CameraPan>() == null)
                Camera.main.gameObject.AddComponent<CameraPan>();

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

        private TileBase GetSettlementTile(Hex hex)
        {
            if (hex.CityIndices == null || hex.CityIndices.Count == 0)
                return null;

            var city = _worldState.GetCity(hex.CityIndices[0]);
            return city.Type == SettlementType.City ? _tileConfig.CityTile : _tileConfig.VillageTile;
        }

        private TileBase GetTerrainTile(Hex hex)
        {
            // If the hex has a settlement, draw the settlement tile instead
            var settlementTile = GetSettlementTile(hex);
            if (settlementTile != null)
                return settlementTile;

            switch (hex.Terrain)
            {
                case HexType.Plains: return _tileConfig.PlainsTile;
                case HexType.Forest: return _tileConfig.ForestTile;
                case HexType.Mountains: return _tileConfig.MountainsTile;
                case HexType.Water: return _tileConfig.WaterTile;
                default: return _tileConfig.PlainsTile;
            }
        }

        private void DrawAllHexes()
        {
            if (_tilemap == null || _tileConfig == null)
            {
                Debug.LogError("[HexGridGenerator] tilemap or config null");
                return;
            }

            if (_tileConfig.FogTile == null)
            {
                var loaded = Resources.LoadAll<TileBase>("Tiles");
                if (loaded.Length > 0) _tileConfig.FogTile = loaded[0];
            }

            var hexes = _worldState.Hexes;
            var positions = new Vector3Int[hexes.Count];
            var tiles = new TileBase[hexes.Count];
            for (int i = 0; i < hexes.Count; i++)
            {
                var hex = hexes[i];
                var cellPos = new Vector3Int(hex.Coordinates.x, hex.Coordinates.y, 0);
                positions[i] = cellPos;

                if (hex.IsDiscovered)
                    tiles[i] = GetTerrainTile(hex);
                else
                    tiles[i] = _tileConfig.FogTile;

                hex.WorldPosition = _tilemap.GetCellCenterWorld(cellPos);
            }
            _tilemap.SetTiles(positions, tiles);
            _tilemap.RefreshAllTiles();

            CenterCameraOnGrid();

            int discoveredCount = hexes.Count(h => h.IsDiscovered);
            Debug.Log($"[HexGridGenerator] {discoveredCount}/{hexes.Count} discovered, {hexes.Count} tiles set");
        }

        private void CenterCameraOnGrid()
        {
            var camera = Camera.main;
            if (camera == null) return;

            var hexes = _worldState.Hexes;
            if (hexes.Count == 0) return;

            float maxX = float.MinValue, maxY = float.MinValue;
            float minX = float.MaxValue, minY = float.MaxValue;
            foreach (var h in hexes)
            {
                var wp = h.WorldPosition;
                if (wp.x < minX) minX = wp.x;
                if (wp.x > maxX) maxX = wp.x;
                if (wp.y < minY) minY = wp.y;
                if (wp.y > maxY) maxY = wp.y;
            }

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float gridWidth = maxX - minX + _tileConfig.CellSize.x;
            float gridHeight = maxY - minY + _tileConfig.CellSize.y;

            camera.transform.position = new Vector3(centerX, centerY, -10);
            camera.orthographicSize = math.max(gridWidth * 0.5f, gridHeight * 0.5f) * 1.15f;
        }

        private void OnHexDiscovered(HexDiscoveredEvent evt)
        {
            var hex = _worldState.GetHex(evt.HexIndex);
            var cellPos = new Vector3Int(hex.Coordinates.x, hex.Coordinates.y, 0);
            _tilemap.SetTile(cellPos, GetTerrainTile(hex));
        }

        public IWorldState GetWorldState() => _worldState;

        private void OnDestroy()
        {
        }
    }
}
