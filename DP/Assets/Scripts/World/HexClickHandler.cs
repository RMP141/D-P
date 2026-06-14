using System.Collections;
using ConvoyManager.Core;
using ConvoyManager.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

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
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            var mousePos = Mouse.current.position.ReadValue();
            var worldPos = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
            var cellPos = _tilemap.WorldToCell(worldPos);

            foreach (var hex in _worldState.Hexes)
            {
                if (hex.Coordinates.x == cellPos.x && hex.Coordinates.y == cellPos.y && !hex.IsDiscovered)
                {
                    StartCoroutine(ScoutRoutine(hex.Index));
                    break;
                }
            }
        }

        private IEnumerator ScoutRoutine(int hexIndex)
        {
            _scoutInProgress = true;
            var hex = _worldState.GetHex(hexIndex);
            var target = new Vector3(hex.Coordinates.x + 0.5f, hex.Coordinates.y + 0.5f, -0.5f);
            var start = new Vector3(1.5f, 1.5f, -0.5f);

            var duration = Vector3.Distance(start, target) / 2f;

            var scout = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            scout.transform.position = start;
            scout.transform.localScale = Vector3.one * 0.3f;
            var mr = scout.GetComponent<MeshRenderer>();
            if (mr != null) mr.material.color = Color.blue;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                scout.transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }

            Destroy(scout);
            _worldState.DiscoverHex(hexIndex);
            _scoutInProgress = false;
        }
    }
}
