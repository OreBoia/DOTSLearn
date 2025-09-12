using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct TankMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        // For each entity having a LocalTransform and Tank component, 
        // we access the LocalTransform and entity ID.
        foreach (var (transform, entity) in
                 SystemAPI.Query<RefRW<LocalTransform>>()
                     .WithAll<Tank>()
                     .WithNone<Player>()  // exclude the player tank from the query
                     .WithEntityAccess())
        {
            var pos = transform.ValueRO.Position;

            // This does not modify the actual position of the tank, only the point at
            // which we sample the 3D noise function. This way, every tank is using a
            // different slice and will move along its own different random flow field.
            pos.y = (float)entity.Index;

            var angle = (0.5f + noise.cnoise(pos / 10f)) * 4.0f * math.PI;
            var dir = float3.zero;
            math.sincos(angle, out dir.x, out dir.z);

            // Update the LocalTransform.
            transform.ValueRW.Position += dir * dt * 5.0f;
            transform.ValueRW.Rotation = quaternion.RotateY(angle);
        }
        
        var spin = quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI);

        foreach (var tank in
                 SystemAPI.Query<RefRW<Tank>>())
        {
            var trans = SystemAPI.GetComponentRW<LocalTransform>(tank.ValueRO.Turret);
            
            // Add a rotation around the Y axis (relative to the parent).
            trans.ValueRW.Rotation = math.mul(spin, trans.ValueRO.Rotation);
        }
    }
}