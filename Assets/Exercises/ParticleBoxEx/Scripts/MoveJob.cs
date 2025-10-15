using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Exercises.ParticleBoxEx.Scripts
{
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float dt;
        public Bounds bounds;
        
        public void Execute(ref LocalTransform t, ref Velocity vel)
        {
            var pos = t.Position;
            var v = vel.Value;
            var b = bounds;
            
            pos += v * dt;

            if (pos.x > b.Max.x) { pos.x = b.Max.x; v.x = -math.abs(v.x); }
            if (pos.x < b.Min.x) { pos.x = b.Min.x; v.x =  math.abs(v.x); }

            // Y
            if (pos.y > b.Max.y) { pos.y = b.Max.y; v.y = -math.abs(v.y); }
            if (pos.y < b.Min.y) { pos.y = b.Min.y; v.y =  math.abs(v.y); }

            // Z
            if (pos.z > b.Max.z) { pos.z = b.Max.z; v.z = -math.abs(v.z); }
            if (pos.z < b.Min.z) { pos.z = b.Min.z; v.z =  math.abs(v.z); }

            t.Position = pos;
            vel.Value = v;
            
        }
    }
}