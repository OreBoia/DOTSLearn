using Unity.Entities;
using Unity.Mathematics;

namespace Exercises.ParticleBoxEx.Scripts
{
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
}