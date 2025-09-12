# Unity DOTS Learning Project - AI Coding Agent Instructions

## Project Overview

This is a Unity DOTS (Data-Oriented Technology Stack) learning project containing educational examples of ECS (Entity-Component-System), JobSystem, and Burst compiler patterns. The project includes multiple tutorial modules demonstrating core DOTS concepts through practical implementations.

## Architecture & Structure

### Core DOTS Pattern
- **Components** (`IComponentData`): Pure data structs without logic (e.g., `RotationSpeed`, `Tank`, `CannonBall`)
- **Systems** (`ISystem`): Logic processors that operate on entities with specific components
- **Authoring/Baking**: Bridge between traditional GameObject workflow and ECS entities

### Project Organization
```
Assets/
├── Scripts/
│   ├── CubeSpawner(Entities)/     # Basic ECS rotation/spawning tutorial
│   └── SeekersAndTargets/         # JobSystem parallel processing examples
├── tanks_tutorial/
│   └── Scripts/                   # Complete tank game with movement, shooting, spawning
└── Dispense/                      # Educational documentation (Italian)
```

## Key DOTS Patterns Used

### 1. Component Definition Pattern
```csharp
public struct RotationSpeed : IComponentData
{
    public float RadiansPerSeconds;
}
```
- Always use `struct` with `IComponentData`
- Keep components as pure data containers
- Use Unity.Mathematics types (`float3`, `quaternion`) over Unity types

### 2. Authoring/Baking Pattern
```csharp
public class RotationSpeedAuthoring : MonoBehaviour
{
    public float DegreesPerSecond = 360.0f;
}

class RotationSpeedBaker : Baker<RotationSpeedAuthoring>
{
    public override void Bake(RotationSpeedAuthoring authoring)
    {
        var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent(entity, new RotationSpeed 
        { 
            RadiansPerSeconds = math.radians(authoring.DegreesPerSecond) 
        });
    }
}
```
- Authoring scripts expose Unity Inspector interface
- Baker classes convert GameObject data to ECS components during baking
- Always specify `TransformUsageFlags` appropriately

### 3. System Query Pattern
```csharp
[BurstCompile]
public partial struct CubeRotationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, rotationSpeed) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            // Process entities with both components
        }
    }
}
```
- Use `RefRW<T>` for components you need to modify
- Use `RefRO<T>` for read-only component access
- Add `[BurstCompile]` for performance-critical systems
- Use `WithAll<T>()`, `WithNone<T>()` for additional filtering

### 4. JobSystem Pattern
```csharp
[BurstCompile]
public struct FindNearestJob : IJob
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float3> SeekerPositions;
    public NativeArray<float3> NearestTargetPosition;

    public void Execute()
    {
        // Single-threaded job logic
    }
}
```
- Mark read-only data with `[ReadOnly]`
- Use `NativeArray`, `NativeList` for job-safe collections
- Prefer `IJobParallelFor` for parallelizable work

## Development Conventions

### Naming Patterns
- **Components**: Noun-based names (`Tank`, `CannonBall`, `Player`)
- **Systems**: Verb-based names ending in "System" (`TankMovementSystem`, `ShootingSystem`)
- **Authoring**: Component name + "Authoring" (`TankAuthoring`, `ConfigAuthoring`)
- **Jobs**: Descriptive name + "Job" (`FindNearestJob`, `FindNearestJobParallel`)

### Entity Relationships
- Use `Entity` fields in components for entity references (see `Tank` component)
- Leverage `SystemAPI.GetComponentRW<T>(entity)` for indirect component access
- Use entity hierarchies sparingly; prefer composition over inheritance

### Performance Considerations
- Always use `Unity.Mathematics` types (`float3`, `math.sin()`) over Unity types
- Apply `[BurstCompile]` to performance-critical systems and jobs
- Use `SystemAPI.Time.DeltaTime` instead of `Time.deltaTime`
- Minimize `SystemAPI.GetComponent` calls in hot paths

## Common Integration Points

### Input Handling
- Player input typically processed in systems with `Player` component filtering
- Input data often stored in singleton components for system access

### Transform Manipulation
- Use `LocalTransform` component for entity positioning/rotation
- `TransformUsageFlags.Dynamic` for entities that move
- Access parent-child relationships through Unity's transform system when needed

### Entity Spawning
- Spawning systems use prefab entity references (see `Config` component)
- `EntityManager.Instantiate()` for runtime entity creation
- Baking converts GameObjects to entity prefabs automatically

## Debugging & Development Workflow

### Entity Inspection
- Use Unity's Entity Debugger window to inspect runtime entities
- Systems window shows system execution order and timing
- Enable "Show Runtime Entities" in Hierarchy for entity visibility

### Common Issues
- Ensure `TransformUsageFlags` matches entity usage patterns
- Check component dependencies in system queries
- Verify job dependencies and memory ownership in parallel jobs

## Documentation Resources

- `Guida_ECS_JobSystem.md`: Comprehensive Italian guide covering ECS theory and practical examples
- Each script directory represents a complete tutorial module
- Comments in systems explain query patterns and entity manipulation techniques

When working with this codebase, prioritize understanding the data flow between components and systems rather than traditional object-oriented patterns. Focus on composing entities through components and processing them efficiently through systems.