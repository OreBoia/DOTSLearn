using Unity.Burst;
using Unity.Entities;

namespace Exercises.ParticleBoxEx.Scripts
{
    public partial struct ParticleMoveJobSystem : ISystem
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
            // var b = SystemAPI.GetSingleton<Bounds>();
            // var job = new MoveJob
            // {
            //     dt = dt,
            //     bounds = b,
            // };
            // job.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}