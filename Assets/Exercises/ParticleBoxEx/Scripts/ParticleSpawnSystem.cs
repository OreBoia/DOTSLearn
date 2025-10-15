using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Rendering;

namespace Exercises.ParticleBoxEx.Scripts
{
    public partial class ParticleSpawnSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // RequireForUpdate<SpawnConfig>();
            // RequireForUpdate<Bounds>();
        }

        protected override void OnStartRunning()
        {
            // var settings = SystemAPI.GetSingleton<SpawnConfig>();
            // var bounds = SystemAPI.GetSingleton<Bounds>();
            // var setupEntitny = SystemAPI.GetSingletonEntity<SpawnConfig>();
            //
            // var ecb = new EntityCommandBuffer(Allocator.Temp);
            //
            // var rng = new Unity.Mathematics.Random((uint)Environment.TickCount);
            //
            // for (int i = 0; i < settings.Count; i++)
            // {
            //     Entity e = ecb.Instantiate((settings.Prefab));
            //
            //     float3 pos = new float3(
            //         rng.NextFloat(bounds.Min.x, bounds.Max.x),
            //         rng.NextFloat(bounds.Min.y, bounds.Max.y),
            //         rng.NextFloat(bounds.Min.z, bounds.Max.z));
            //
            //     float3 dir = math.normalize(rng.NextFloat3Direction());
            //     float speed = rng.NextFloat(settings.SpeedRange.x, settings.SpeedRange.y);
            //
            //     var lt = LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f);
            //     ecb.SetComponent(e, lt);
            //     ecb.AddComponent(e, new Velocity { Value = dir * speed });
            // }
            //
            // ecb.Playback(EntityManager);
            // ecb.Dispose();
            //
            // EntityManager.RemoveComponent<SpawnConfig>(setupEntitny);
        }

        protected override void OnUpdate(){}
    }
}