using Unity.Entities;
using Unity.Mathematics;

namespace Exercises.ParticleBoxV2.Scripts
{
    public struct Bounds : IComponentData
    {
        public float3 Min;
        public float3 Max;
    }
}