using Unity.Entities;
using Unity.Transforms;

namespace Exercises.ParticleBoxV2.Scripts
{
    public partial struct MoveJobV2 : IJobEntity
    {
        public float dt;
        public Bounds Bounds;
        public void Execute(ref LocalTransform t, ref Velocity vel, in ParticleTag tag)
        {

        }
    }
}