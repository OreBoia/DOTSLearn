using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public partial struct CubeRotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        // This foreach loops through all entities that have LocalTransform and RotationSpeed components. 
        // You need to modify the LocalTransform, so it is wrapped in RefRW (read-write). 
        // You only need to read the RotationSpeed, so it is wrapped in RefRO (read only). 
        foreach (var (trasform, rotationSpeed) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            // Rotate the transform around the Y axis. 
            var radians = rotationSpeed.ValueRO.RadiansPerSeconds * deltaTime;
            trasform.ValueRW = trasform.ValueRW.RotateY(radians);
        }
    }
}
