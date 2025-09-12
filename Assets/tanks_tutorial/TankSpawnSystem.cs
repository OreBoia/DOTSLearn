using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial struct TankSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // This RequireForUpdate means the system only updates if at 
        // least one entity with the Config component exists.
        // Effectively, this system will not update until the 
        // subscene with the Config has been loaded.
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Disabling a system will stop further updates.
        // By disabling this system in its first update, 
        // it will effectively update only once.
        state.Enabled = false;

        // A "singleton" is a component type which only has a single instance.
        // GetSingleton<T> throws an exception if zero entities or 
        // more than one entity has a component of type T.
        // In this case, there should only ever be one instance of Config.
        var config = SystemAPI.GetSingleton<Config>();

        // Random numbers from a hard-coded seed.
        var random = new Random(123);

        for (int i = 0; i < config.TankCount; i++)
        {
            var tankEntity = state.EntityManager.Instantiate(config.TankPrefab);
            
            if (i == 0)
            {
                state.EntityManager.AddComponent<Player>(tankEntity);
            }

            // URPMaterialPropertyBaseColor is a component from the Entities.Graphics package 
            // that lets us set the rendered base color of a rendered entity.
            var color = new URPMaterialPropertyBaseColor { Value = RandomColor(ref random) };
           
            // Every root entity instantiated from a prefab has a LinkedEntityGroup component, which
            // is a list of all the entities that make up the prefab hierarchy (including the root).

            // (LinkedEntityGroup is a special kind of component called a "DynamicBuffer", which is
            // a resizable array of struct values instead of just a single struct.)
            var linkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(tankEntity);
            foreach (var entity in linkedEntities)
            {
                // we want to set the URPMaterialPropertyBaseColor component only on the
                // entities that have it, so we first check
                if (state.EntityManager.HasComponent<URPMaterialPropertyBaseColor>(entity.Value))
                {
                    // Set the color of each entity that makes up the tank.
                    state.EntityManager.SetComponentData(entity.Value, color);    
                }
            }
        }
    }

    // Return a random color that is visually distinct.
    // (Naive randomness would produce a distribution of colors clustered 
    // around a narrow range of hues.
    // See https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/ )
    static float4 RandomColor(ref Random random)
    {
        // 0.618034005f is inverse of the golden ratio
        var hue = (random.NextFloat() + 0.618034005f) % 1;
        return (Vector4)Color.HSVToRGB(hue, 1.0f, 1.0f);
    }
}

