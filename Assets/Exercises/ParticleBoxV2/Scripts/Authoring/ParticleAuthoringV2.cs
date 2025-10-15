using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace Exercises.ParticleBoxV2.Scripts.Authoring
{
    public class ParticleAuthoringV2 : MonoBehaviour
    {
        public GameObject ParticlePrefab;
        public int Count = 500;
        public Vector3 BoxMin = new(-10,-10,-10);
        public Vector3 BoxMax = new(10,10,10);
        public float SpeedMin = 1f;
        public float SpeedMax = 5f;
        
        private class ParticleAuthoringBaker : Baker<ParticleAuthoringV2>
        {
            public override void Bake(ParticleAuthoringV2 authoringV2)
            {
                var e = GetEntity(authoringV2, TransformUsageFlags.Dynamic);
                AddComponent(e, new Bounds { Min = authoringV2.BoxMin, Max = authoringV2.BoxMax });
                
                AddComponent(e, new SpawnConfigV2
                {
                    Prefab = GetEntity(authoringV2.ParticlePrefab, TransformUsageFlags.Renderable),
                    Count = authoringV2.Count,
                    SpeedRange = new float2(authoringV2.SpeedMin, authoringV2.SpeedMax)
                });;
            }
        }
    }
}

public struct SpawnConfigV2 : IComponentData
{
    public Entity Prefab;
    public int Count;
    public float2 SpeedRange;
}