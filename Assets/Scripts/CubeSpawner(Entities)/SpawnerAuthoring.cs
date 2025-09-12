using Unity.Entities;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject CubePrefab;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            var spawner = new SpawnerComponentData
            {
                Prefab = GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic)
            };

            AddComponent(entity, spawner);
        }
    }
}

struct SpawnerComponentData : IComponentData
{
    public Entity Prefab;
}
