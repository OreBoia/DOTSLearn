using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct CannonBallSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        var cannonBallJob = new CannonBallJob
        {
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        cannonBallJob.Schedule();
    }

    // IJobEntity relies on source generation to implicitly define a 
    // query from the signature of the Execute method.
    // In this case, the implicit query will look for all entities that
    // have the CannonBall and LocalTransform components. 
    [BurstCompile]
    public partial struct CannonBallJob : IJobEntity
    {
        public EntityCommandBuffer ECB { get; set; }
        public float DeltaTime { get; set; }

        // Execute will be called once for every entity that 
        // has a CannonBall and LocalTransform component.

        void Execute(Entity entity, ref CannonBall cannonBall, ref LocalTransform localTransform)
        {
            var gravity = new float3(0.0f, -9.82f, 0.0f);

            localTransform.Position += cannonBall.Velocity * DeltaTime;

            // if hit the ground
            if (localTransform.Position.y <= 0.0f)
            {
                ECB.DestroyEntity(entity);
            }

            cannonBall.Velocity += gravity * DeltaTime;
        }
    }
}
