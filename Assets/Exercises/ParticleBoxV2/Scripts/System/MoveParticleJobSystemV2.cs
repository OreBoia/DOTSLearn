using Unity.Burst;
using Unity.Entities;

namespace Exercises.ParticleBoxV2.Scripts.System
{
    public partial struct MoveParticleJobSystemV2 : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Bounds>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new MoveJobV2
            {
                dt = SystemAPI.Time.DeltaTime,
                Bounds = SystemAPI.GetSingleton<Bounds>()
            };

            job.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}