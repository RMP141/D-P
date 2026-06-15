using ConvoyManager.ECS;
using ConvoyManager.World;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ConvoyManager.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ConvoyVisualizationSystem : SystemBase
    {
        private IWorldState _worldState;
        private GameObject _visualRoot;
        private GameObject[] _visuals = new GameObject[0];
        private EntityQuery _convoyQuery;

        public void SetWorldState(IWorldState worldState)
        {
            _worldState = worldState;
        }

        protected override void OnCreate()
        {
            _convoyQuery = EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ConvoyTag>(),
                ComponentType.ReadOnly<ConvoyStateComponent>(),
                ComponentType.ReadOnly<RouteComponent>()
            );

            _visualRoot = new GameObject("ConvoyVisuals");
            _visualRoot.transform.SetParent(GameObject.Find("Grid")?.transform);
        }

        protected override void OnUpdate()
        {
            if (_worldState == null) return;

            using var entities = _convoyQuery.ToEntityArray(Allocator.Temp);
            // Count only traveling convoys
            int count = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var state = EntityManager.GetComponentData<ConvoyStateComponent>(entities[i]);
                if (state.State == ConvoyState.Traveling) count++;
            }

            if (count != _visuals.Length)
                ResizeVisuals(count);

            int visIdx = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                var state = EntityManager.GetComponentData<ConvoyStateComponent>(entities[i]);
                if (state.State != ConvoyState.Traveling) continue;

                var pos = CalculatePosition(entities[i], state);
                _visuals[visIdx].transform.position = new Vector3(pos.x, pos.y, 0);
                visIdx++;
            }
        }

        private float3 CalculatePosition(Entity entity, ConvoyStateComponent state)
        {
            var route = EntityManager.GetComponentData<RouteComponent>(entity);
            if (!route.Blob.IsCreated) return float3.zero;

            ref var routeBlob = ref route.Blob.Value;
            if (routeBlob.CityIndices.Length < 2) return float3.zero;

            int seg = routeBlob.CurrentSegment;
            if (seg < 0 || seg + 1 >= routeBlob.CityIndices.Length) return float3.zero;
            int cityA = routeBlob.CityIndices[seg];
            int cityB = routeBlob.CityIndices[seg + 1];

            var hexA = _worldState.GetHex(_worldState.GetCity(cityA).HexIndex);
            var hexB = _worldState.GetHex(_worldState.GetCity(cityB).HexIndex);

            return math.lerp(hexA.WorldPosition, hexB.WorldPosition, state.Progress);
        }

        private void ResizeVisuals(int count)
        {
            foreach (var go in _visuals)
                if (go != null) Object.Destroy(go);

            _visuals = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(_visualRoot.transform, false);
                go.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.material.color = Color.yellow;
                _visuals[i] = go;
            }
        }

        protected override void OnDestroy()
        {
            ResizeVisuals(0);
            if (_visualRoot != null) Object.Destroy(_visualRoot);
        }
    }
}
