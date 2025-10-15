using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Exercises.ParticleBoxV2.Scripts.Authoring
{
    public class ParticleAuthoring : MonoBehaviour
    {
        public GameObject ParticlePrefab;
        public int Count = 500;
        public Vector3 BoxMin = new(-10,-10,-10);
        public Vector3 BoxMax = new(10,10,10);
        public float SpeedMin = 1f;
        public float SpeedMax = 5f;
        
        private class ParticleAuthoringBaker : Baker<ParticleAuthoring>
        {
            public override void Bake(ParticleAuthoring authoring)
            {
                var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(e, new Bounds { Min = authoring.BoxMin, Max = authoring.BoxMax });
                
                AddComponent(e, new SpawnConfig
                {
                    Prefab = GetEntity(authoring.ParticlePrefab, TransformUsageFlags.Renderable),
                    Count = authoring.Count,
                    SpeedRange = new float2(authoring.SpeedMin, authoring.SpeedMax)
                });;
            }
        }
    }
}

public struct SpawnConfig : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float2 SpeedRange;
}