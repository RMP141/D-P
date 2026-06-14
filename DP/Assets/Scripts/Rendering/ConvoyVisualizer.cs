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
            int count = entities.Length;
            if (count != _visuals.Length)
                ResizeVisuals(count);

            for (int i = 0; i < count; i++)
            {
                var pos = CalculatePosition(entities[i]);
                _visuals[i].transform.position = new Vector3(pos.x, pos.y, 0);
            }
        }

        private float3 CalculatePosition(Entity entity)
        {
            var state = EntityManager.GetComponentData<ConvoyStateComponent>(entity);
            var route = EntityManager.GetComponentData<RouteComponent>(entity);
            ref var routeBlob = ref route.Blob.Value;

            int cityA = routeBlob.CityIndices[0];
            int cityB = routeBlob.CityIndices[1];

            var hexA = _worldState.GetHex(_worldState.GetCity(cityA).HexIndex);
            var hexB = _worldState.GetHex(_worldState.GetCity(cityB).HexIndex);

            float3 aPos = new float3(hexA.Coordinates.x + 0.5f, hexA.Coordinates.y + 0.5f, 0);
            float3 bPos = new float3(hexB.Coordinates.x + 0.5f, hexB.Coordinates.y + 0.5f, 0);

            return math.lerp(aPos, bPos, state.Progress);
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
