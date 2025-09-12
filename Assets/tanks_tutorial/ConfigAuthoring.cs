using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject TankPrefab;
    public GameObject CannonBallPrefab;
    public int TankCount;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            // The config entity itself doesn’t need transform components,
            // so we use TransformUsageFlags.None
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                // Bake the prefab into entities. GetEntity will return the 
                // root entity of the prefab hierarchy.
                TankPrefab = GetEntity(authoring.TankPrefab, TransformUsageFlags.Dynamic),
                CannonBallPrefab = GetEntity(authoring.CannonBallPrefab, TransformUsageFlags.Dynamic),
                TankCount = authoring.TankCount,
            });
        }
    }
}
public struct Config : IComponentData
{
    public Entity TankPrefab;
    public Entity CannonBallPrefab;
    public int TankCount;
}