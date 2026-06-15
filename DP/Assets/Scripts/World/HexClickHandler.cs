using System.Collections;
using System.Linq;
using ConvoyManager.Core;
using ConvoyManager.Utils;
using ConvoyManager.World;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Random = Unity.Mathematics.Random;

namespace ConvoyManager.World
{
    public class HexClickHandler : MonoBehaviour
    {
        private IWorldState _worldState;
        private EventBus _eventBus;
        private Tilemap _tilemap;
        private Camera _camera;
        private bool _scoutInProgress;
        private bool _active = true;
        private float _scoutCooldown;

        private const float ScoutCooldownSeconds = 2f;

        public void Initialize(IWorldState worldState, EventBus eventBus)
        {
            _worldState = worldState;
            _eventBus = eventBus;
        }

        public void SetActive(bool active)
        {
            _active = active;
        }

        private void Start()
        {
            _tilemap = GetComponentInChildren<Tilemap>();
            _camera = Camera.main;
        }

        private void Update()
        {
            if (_worldState == null) return;
            if (!_active) return;
            if (_scoutInProgress) return;

            if (_scoutCooldown > 0)
                _scoutCooldown -= Time.deltaTime;

            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            var mousePos = Mouse.current.position.ReadValue();
            var worldPos = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
            var cellPos = _tilemap.WorldToCell(worldPos);

            if (_scoutCooldown > 0)
            {
                Debug.Log($"Scout on cooldown ({_scoutCooldown:F1}s left)");
                return;
            }

            // Try exact cell hit first
            var targetHex = FindHexAtCell(cellPos.x, cellPos.y);
            if (targetHex == null)
            {
                // Fallback: find nearest undiscovered hex within generous radius (~2 cells)
                targetHex = FindNearestUndiscoveredHex(worldPos, _tilemap.cellSize.x * 1.5f);
            }

            if (targetHex == null || targetHex.IsDiscovered)
                return;

            if (!IsAdjacentToDiscovered(targetHex.Index))
            {
                Debug.Log("Target hex must be adjacent to discovered territory");
                return;
            }

            StartCoroutine(ScoutRoutine(targetHex.Index));
        }

        private Hex FindHexAtCell(int x, int y)
        {
            foreach (var hex in _worldState.Hexes)
            {
                if (hex.Coordinates.x == x && hex.Coordinates.y == y)
                    return hex;
            }
            return null;
        }

        private Hex FindNearestUndiscoveredHex(Vector3 worldPos, float maxDist)
        {
            Hex nearest = null;
            float nearestDist = float.MaxValue;
            foreach (var hex in _worldState.Hexes)
            {
                if (hex.IsDiscovered) continue;
                var hc = hex.Coordinates;
                var hexCenter = _tilemap.GetCellCenterWorld(new Vector3Int(hc.x, hc.y, 0));
                float d = Vector3.Distance(worldPos, hexCenter);
                if (d < maxDist && d < nearestDist)
                {
                    nearestDist = d;
                    nearest = hex;
                }
            }
            return nearest;
        }

        private bool IsAdjacentToDiscovered(int hexIndex)
        {
            var hex = _worldState.GetHex(hexIndex);
            int x = hex.Coordinates.x;
            int y = hex.Coordinates.y;
            bool oddRow = (y & 1) == 1;

            int[][] offsetsEven = new int[][]
            {
                new int[] { -1, 0 }, new int[] { 1, 0 },
                new int[] { 0, -1 }, new int[] { -1, -1 },
                new int[] { 0, 1 }, new int[] { -1, 1 }
            };

            int[][] offsetsOdd = new int[][]
            {
                new int[] { -1, 0 }, new int[] { 1, 0 },
                new int[] { 1, -1 }, new int[] { 0, -1 },
                new int[] { 1, 1 }, new int[] { 0, 1 }
            };

            var offsets = oddRow ? offsetsOdd : offsetsEven;
            int width = 30, height = 30;

            foreach (var o in offsets)
            {
                int nx = x + o[0];
                int ny = y + o[1];
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;
                int ni = nx * height + ny;
                if (_worldState.GetHex(ni).IsDiscovered)
                    return true;
            }
            return false;
        }

        private IEnumerator ScoutRoutine(int hexIndex)
        {
            _scoutInProgress = true;
            var hex = _worldState.GetHex(hexIndex);
            var target = hex.WorldPosition + new float3(0, 0, -0.5f);

            // Always start from the center hex
            var start = _worldState.GetHex(_worldState.CenterHexIndex).WorldPosition + new float3(0, 0, -0.5f);

            // Distance-based travel: 1 hex = 0.5 seconds, min 0.5s
            int hexDist = MathUtils.HexDistance(
                hex.Coordinates,
                _worldState.Hexes[_worldState.CenterHexIndex].Coordinates);
            var duration = Mathf.Max(0.5f, hexDist * 0.5f);

            // Scout visual: diamond-shaped sprite, blue
            var scout = new GameObject("Scout");
            var sr = scout.AddComponent<SpriteRenderer>();
            sr.sprite = CreateScoutSprite();
            sr.sortingOrder = 10;
            scout.transform.position = start;
            scout.transform.localScale = Vector3.one * 0.5f;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                scout.transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            Destroy(scout);
            _worldState.DiscoverHex(hexIndex);
            _scoutCooldown = ScoutCooldownSeconds;
            _scoutInProgress = false;
        }

        private static Sprite CreateScoutSprite()
        {
            var tex = new Texture2D(32, 32);
            Color clear = Color.clear;
            Color blue = new Color(0.2f, 0.4f, 1f);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Diamond shape: |x-15.5| + |y-15.5| <= 13
                    float dx = Mathf.Abs(x - 15.5f);
                    float dy = Mathf.Abs(y - 15.5f);
                    tex.SetPixel(x, y, dx + dy <= 13f ? blue : clear);
                }
            }
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
    }
}
