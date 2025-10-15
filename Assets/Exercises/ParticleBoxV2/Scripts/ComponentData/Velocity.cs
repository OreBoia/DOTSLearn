using Unity.Entities;
using Unity.Mathematics;

namespace Exercises.ParticleBoxV2.Scripts
{
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
}