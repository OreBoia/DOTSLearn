using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Exercises.ParticleBoxEx.Scripts
{
    public class ParticleAuthoring : MonoBehaviour
    {
        [Header("Spawn")]
        public GameObject Prefab;
        public int Count = 10_000;

        [Header("Motion")]
        public Vector2 SpeedRange = new Vector2(1f, 5f);

        [Header("Bounds (world-space)")]
        public Vector3 Min = new Vector3(-25f, -25f, -25f);
        public Vector3 Max = new Vector3( 25f,  25f,  25f);
        
        private class ParticleAuthoringBaker : Baker<ParticleAuthoring>
        {
            public override void Bake(ParticleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new Bounds
                {
                    Min = (float3)authoring.Min,
                    Max = (float3)authoring.Max
                });

                if (authoring.Prefab != null)
                {
                    var prefabEntity = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);
                    AddComponent(entity, new SpawnConfig
                    {
                        Prefab = prefabEntity,
                        Count = authoring.Count,
                        SpeedRange = (float2)authoring.SpeedRange,
                    });
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
}