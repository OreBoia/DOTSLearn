using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Exercises.ParticleBoxV2.Scripts.Aspect
{
    public readonly partial struct ParticleAspect : IAspect
    {
        public readonly RefRW<LocalTransform> Transform;
        public readonly RefRW<Velocity> Vel;

        public void Integrate(float dt)
        {
            Transform.ValueRW.Position += Vel.ValueRW.Value * dt;
        }

        public void Bounce(in Bounds b)
        {
            var pos = Transform.ValueRO.Position;
            var v = Vel.ValueRW.Value;
            //X
            if (pos.x > b.Max.x) { pos.x = b.Max.x; v.x = -math.abs(v.x); }
            if (pos.x < b.Min.x) { pos.x = b.Min.x; v.x =  math.abs(v.x); }
            
            // Y
            if (pos.y > b.Max.y) { pos.y = b.Max.y; v.y = -math.abs(v.y); }
            if (pos.y < b.Min.y) { pos.y = b.Min.y; v.y =  math.abs(v.y); }
            
            // Z
            if (pos.z > b.Max.z) { pos.z = b.Max.z; v.z = -math.abs(v.z); }
            if (pos.z < b.Min.z) { pos.z = b.Min.z; v.z =  math.abs(v.z); }
            
            Transform.ValueRW.Position = pos;
            Vel.ValueRW = new Velocity { Value = v };
        }
    }
}