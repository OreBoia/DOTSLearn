using Unity.Entities;
using Unity.Mathematics;

namespace Exercises.ParticleBoxEx.Scripts
{
    public struct Bounds : IComponentData
    {
        public float3 Min;
        public float3 Max;
    }
}