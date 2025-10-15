using System;
using System.Security.Cryptography;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Exercises.ParticleBoxV2.Scripts.System
{
    public partial struct SpawnParticleSystemV2 : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnConfigV2>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<SpawnConfigV2>();
            var bounds = SystemAPI.GetSingleton<Bounds>();
            var singletonEntity = SystemAPI.GetSingletonEntity<SpawnConfigV2>();

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var rng = new Random((uint)Environment.TickCount);
            
            for (int i = 0; i < settings.Count; i++)
            {
                Entity e = ecb.Instantiate(settings.Prefab);

                float3 pos = new float3(
                    rng.NextFloat(bounds.Min.x, bounds.Max.x),
                    rng.NextFloat(bounds.Min.y, bounds.Max.y),
                    rng.NextFloat(bounds.Min.z, bounds.Max.z));

                float speed = rng.NextFloat(settings.SpeedRange.x, settings.SpeedRange.y);
                float3 dir = math.normalize(rng.NextFloat3Direction());
                
                float3 vel = dir * speed;
                
                var lt = LocalTransform.FromPositionRotationScale(pos, quaternion.identity, 1f);
                ecb.SetComponent(e, lt);
                
                ecb.AddComponent(e, new ParticleTag());
                ecb.AddComponent(e, new Velocity { Value = vel });
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            state.Enabled = false;
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}