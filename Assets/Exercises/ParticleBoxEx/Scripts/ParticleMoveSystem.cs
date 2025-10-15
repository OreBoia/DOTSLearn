using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Exercises.ParticleBoxEx.Scripts
{
    public partial struct ParticleMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // float dt = SystemAPI.Time.DeltaTime;
            //
            // foreach (var (transform, vel, bounds) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>, RefRO<Bounds>>())
            // {
            //     var pos = transform.ValueRO.Position;
            //     var v = vel.ValueRO.Value;
            //     var b = bounds.ValueRO;
            //     
            //     pos += v * dt;
            //
            //     if (pos.x > b.Max.x) { pos.x = b.Max.x; v.x = -math.abs(v.x); }
            //     if (pos.x < b.Min.x) { pos.x = b.Min.x; v.x =  math.abs(v.x); }
            //
            //     // Y
            //     if (pos.y > b.Max.y) { pos.y = b.Max.y; v.y = -math.abs(v.y); }
            //     if (pos.y < b.Min.y) { pos.y = b.Min.y; v.y =  math.abs(v.y); }
            //
            //     // Z
            //     if (pos.z > b.Max.z) { pos.z = b.Max.z; v.z = -math.abs(v.z); }
            //     if (pos.z < b.Min.z) { pos.z = b.Min.z; v.z =  math.abs(v.z); }
            //
            //     transform.ValueRW.Position = pos;
            //     vel.ValueRW.Value = v;
            // }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}