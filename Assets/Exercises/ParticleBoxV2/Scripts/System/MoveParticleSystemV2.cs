using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Exercises.ParticleBoxV2.Scripts.System
{
    public partial struct MoveParticleSystemV2 : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // float dt = SystemAPI.Time.DeltaTime;
            // var bounds = SystemAPI.GetSingleton<Bounds>();
            //
            // foreach (var (transform, vel) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<Velocity>>().WithAll<ParticleTag>())
            // {
            //     var t = transform.ValueRO;
            //     var v = vel.ValueRO.Value;
            //
            //     t.Position += v * dt;
            //
            //     var b = bounds;
            //     var pos = t.Position;
            //     
            //     //X
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
            //     transform.ValueRW = t;
            //     vel.ValueRW = new Velocity { Value = v };
            // }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}