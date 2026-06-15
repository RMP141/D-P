using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ConvoyManager.World
{
    public class CameraPan : MonoBehaviour
    {
        public float edgeScrollSize = 20f;
        public float panSpeed = 15f;
        public float edgePadding = 2f;

        private Camera _cam;
        private float _minX, _maxX, _minY, _maxY;
        private bool _boundsSet;
        private bool _active = true;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            ComputeBounds();
        }

        private void ComputeBounds()
        {
            var generator = FindFirstObjectByType<HexGridGenerator>();
            if (generator == null) return;

            var worldState = generator.GetWorldState();
            if (worldState == null || worldState.Hexes.Count == 0) return;

            float mapMinX = worldState.Hexes.Min(h => h.WorldPosition.x);
            float mapMaxX = worldState.Hexes.Max(h => h.WorldPosition.x);
            float mapMinY = worldState.Hexes.Min(h => h.WorldPosition.y);
            float mapMaxY = worldState.Hexes.Max(h => h.WorldPosition.y);

            // Clamp camera so viewport doesn't show beyond map edges
            if (_cam != null && _cam.orthographic)
            {
                float vertExtent = _cam.orthographicSize;
                float horzExtent = vertExtent * _cam.aspect;

                float mapCenterX = (mapMinX + mapMaxX) * 0.5f;
                float mapCenterY = (mapMinY + mapMaxY) * 0.5f;

                _minX = mapMinX + horzExtent - edgePadding;
                _maxX = mapMaxX - horzExtent + edgePadding;
                _minY = mapMinY + vertExtent - edgePadding;
                _maxY = mapMaxY - vertExtent + edgePadding;

                // Prevent inverted bounds when map is smaller than viewport
                if (_minX > _maxX) { _minX = _maxX = mapCenterX; }
                if (_minY > _maxY) { _minY = _maxY = mapCenterY; }
            }
            else
            {
                _minX = mapMinX - edgePadding;
                _maxX = mapMaxX + edgePadding;
                _minY = mapMinY - edgePadding;
                _maxY = mapMaxY + edgePadding;
            }

            _boundsSet = true;
        }

        public void SetActive(bool active)
        {
            _active = active;
        }

        private void Update()
        {
            if (!_active) return;
            if (!_boundsSet) ComputeBounds();
            if (!_boundsSet) return;

            var pos = transform.position;
            var mouse = Mouse.current.position.ReadValue();
            float speed = panSpeed * Time.deltaTime;

            if (mouse.x < edgeScrollSize) pos.x -= speed;
            if (mouse.x > Screen.width - edgeScrollSize) pos.x += speed;
            if (mouse.y < edgeScrollSize) pos.y -= speed;
            if (mouse.y > Screen.height - edgeScrollSize) pos.y += speed;

            pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
            pos.y = Mathf.Clamp(pos.y, _minY, _maxY);

            transform.position = pos;
        }
    }
}
