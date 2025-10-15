using Exercises.ParticleBoxV2.Scripts.Aspect;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Exercises.ParticleBoxV2.Scripts
{
    public partial struct MoveJobV2 : IJobEntity
    {
        public float dt;
        public Bounds Bounds;
        public void Execute(ParticleAspect aspect)
        {
            aspect.Integrate(dt);
            aspect.Bounce(in Bounds);
        }
    }
}