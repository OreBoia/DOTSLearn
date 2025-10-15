# Riepilogo dei Concetti Chiave di DOTS

Questo documento riassume i concetti fondamentali per lavorare con il Data-Oriented Technology Stack (DOTS) di Unity.

## Indice

1. [Component Data: I Mattoni Fondamentali](#1-component-data-i-mattoni-fondamentali)
2. [Authoring: Dal GameObject all'Entità](#2-authoring-dal-gameobject-allentità)
3. [Aspects: Interfacce Intelligenti per i Dati](#3-aspects-interfacce-intelligenti-per-i-dati)
4. [EntityManager e EntityCommandBuffer: Gestione delle Entità](#4-entitymanager-e-entitycommandbuffer-gestione-delle-entità)
5. [Debugging delle Entità](#5-debugging-delle-entità)
6. [Schema di Implementazione: Dalla Concezione al Codice](#6-schema-di-implementazione-dalla-concezione-al-codice)
7. [ISystem vs SystemBase: Confronto Diretto](#7-isystem-vs-systembase-confronto-diretto)

## 1. Component Data: I Mattoni Fondamentali

I **Component Data** sono le strutture dati pure che contengono le informazioni di gioco in DOTS. A differenza dei MonoBehaviour tradizionali, sono semplici contenitori di dati senza logica.

### Tipi di Componenti

- **IComponentData:** Per dati semplici per entità (posizione, salute, velocità).

```csharp
public struct Health : IComponentData
{
    public float Value;
    public float MaxValue;
}
```

- **IBufferElementData:** Per collezioni dinamiche di dati (inventario, waypoints).

```csharp
public struct InventoryItem : IBufferElementData
{
    public Entity ItemEntity;
    public int Quantity;
}
```

- **ISharedComponentData:** Per dati condivisi tra molte entità (materiali, configurazioni).

```csharp
public struct RenderMesh : ISharedComponentData
{
    public Mesh mesh;
    public Material material;
}
```

### Principi Chiave

- **Solo Dati:** Nessuna logica, solo variabili pubbliche
- **Struct Blittable:** Deve essere copiabile bit per bit (no reference types)
- **Immutabilità Preferita:** Evita modifiche parziali, sostituisci l'intero componente

## 2. Authoring: Dal GameObject all'Entità

L'**Authoring** è il processo che converte i GameObject tradizionali in entità DOTS durante il build o in runtime. Permette di progettare in modalità Object-Oriented e convertire automaticamente in Data-Oriented.

### Baker: Il Convertitore Automatico

I **Baker** sono classi che definiscono come convertire un MonoBehaviour in componenti DOTS:

```csharp
public class HealthAuthoring : MonoBehaviour
{
    public float MaxHealth = 100f;
}

public class HealthBaker : Baker<HealthAuthoring>
{
    public override void Bake(HealthAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Health
        {
            Value = authoring.MaxHealth,
            MaxValue = authoring.MaxHealth
        });
    }
}
```

### Vantaggi dell'Authoring

- **Workflow Familiare:** Usa l'Inspector e i prefab come sempre
- **Conversione Automatica:** Al build, tutto diventa ottimizzato per DOTS
- **Debug Facilitato:** Mantieni la visualizzazione GameObject nell'Editor

## 3. Aspects: Interfacce Intelligenti per i Dati

Gli **Aspects** sono wrapper che forniscono un'interfaccia conveniente per accedere a gruppi correlati di componenti su un'entità. Semplificano l'accesso ai dati e rendono il codice più leggibile.

### Struttura di un Aspect

```csharp
public readonly partial struct MovementAspect : IAspect
{
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRO<MovementData> MovementData;
    
    public float3 Position
    {
        get => Transform.ValueRO.Position;
        set => Transform.ValueRW.Position = value;
    }
    
    public void Move(float deltaTime)
    {
        Position += MovementData.ValueRO.Direction * MovementData.ValueRO.Speed * deltaTime;
    }
}
```

### Vantaggi degli Aspects

- **Encapsulation:** Raggruppa logicamente componenti correlati
- **Convenient API:** Fornisce metodi e proprietà intuitive
- **Type Safety:** Il sistema garantisce che tutti i componenti richiesti siano presenti
- **Performance:** Zero overhead runtime, tutto risolto a compile-time

### Utilizzo negli Sistemi

```csharp
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var movement in SystemAPI.Query<MovementAspect>())
        {
            movement.Move(deltaTime);
        }
    }
}
```

## 4. EntityManager e EntityCommandBuffer: Gestione delle Entità

### EntityManager: Il Cuore della Gestione Entità

L'**EntityManager** è la classe centrale che gestisce tutte le operazioni sulle entità in DOTS. È responsabile della creazione, modifica e distruzione delle entità e dei loro componenti.

#### Operazioni Principali

```csharp
public partial struct EntityManagementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        // Creazione entità
        var newEntity = entityManager.CreateEntity();
        
        // Aggiunta componenti
        entityManager.AddComponent<Health>(newEntity);
        entityManager.SetComponentData(newEntity, new Health { Value = 100f, MaxValue = 100f });
        
        // Lettura componenti
        var health = entityManager.GetComponentData<Health>(newEntity);
        
        // Rimozione componenti
        entityManager.RemoveComponent<Health>(newEntity);
        
        // Distruzione entità
        entityManager.DestroyEntity(newEntity);
    }
}
```

#### Creazione con Archetipi

Un **EntityArchetype** (Archetipo di Entità) è uno dei concetti fondamentali per ottenere alte prestazioni in DOTS. Pensa a un archetipo come a un **progetto** o uno stampo per biscotti.

Invece di definire un'entità e poi aggiungere i componenti uno per uno, un archetipo pre-definisce l'esatta combinazione di tipi di componenti che un gruppo di entità deve avere.

> **Perché è così efficiente?**
>
> - **Organizzazione della Memoria:** L'`EntityManager` raggruppa tutte le entità che condividono lo stesso archetipo in blocchi di memoria contigui chiamati **Chunk**.
> - **Accesso Veloce ai Dati:** Quando un sistema deve iterare su componenti (es. aggiornare tutte le posizioni), può scorrere questi blocchi di memoria in modo lineare. Questo massimizza l'uso della cache della CPU, riducendo drasticamente i tempi di accesso ai dati.
> - **Creazione Istantanea:** Creare un'entità da un archetipo è un'operazione rapidissima. L'`EntityManager` sa già esattamente dove allocare la memoria per la nuova entità, perché la "forma" dell'entità è già definita.

Nel codice di esempio, il processo è suddiviso in due fasi chiave per massimizzare le prestazioni:

1. **`OnCreate` (Definizione dello stampo):** L'archetipo `playerArchetype` viene creato una sola volta quando il sistema nasce. Questo definisce che ogni "giocatore" sarà composto da `LocalTransform`, `Health`, `MovementData` e `PlayerTag`. Questa operazione è relativamente costosa, per cui va eseguita raramente.

2. **`OnUpdate` (Utilizzo dello stampo):** Ad ogni frame (o quando necessario), `CreateEntity(playerArchetype)` usa lo stampo pre-costruito per "stampare" una nuova entità in modo estremamente efficiente. A questo punto, l'entità esiste già con tutti i suoi componenti, anche se i loro dati non sono ancora stati inizializzati.

Infine, `SetComponentData` viene usato per inizializzare i valori dei componenti appena creati.

```csharp
public partial struct ArchetypeCreationSystem : ISystem
{
    private EntityArchetype playerArchetype;
    
    public void OnCreate(ref SystemState state)
    {
        // Definisci l'archetipo una volta
        playerArchetype = state.EntityManager.CreateArchetype(
            typeof(LocalTransform),
            typeof(Health),
            typeof(MovementData),
            typeof(PlayerTag)
        );
    }
    
    public void OnUpdate(ref SystemState state)
    {
        // Creazione efficiente usando l'archetipo
        var player = state.EntityManager.CreateEntity(playerArchetype);
        
        // Inizializza i componenti
        state.EntityManager.SetComponentData(player, new Health { Value = 100f, MaxValue = 100f });
        state.EntityManager.SetComponentData(player, new MovementData { Speed = 5f });
    }
}
```

In sintesi, usare gli archetipi è la pratica consigliata per creare entità in DOTS perché permette al sistema di organizzare i dati nel modo più efficiente possibile per l'hardware moderno.

### EntityCommandBuffer: Operazioni Differite e Thread-Safe

L'**EntityCommandBuffer (ECB)** permette di registrare comandi per modificare entità che verranno eseguiti in un momento successivo. È essenziale per:

- **Thread Safety:** Modifiche sicure da job paralleli
- **Performance:** Batch di operazioni per ridurre overhead
- **Determinismo:** Controllo preciso di quando le modifiche vengono applicate

#### Tipi di EntityCommandBuffer

```csharp
public partial struct ECBUsageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // 1. ECB Immediato (uso diretto)
        var ecbImmediate = new EntityCommandBuffer(Allocator.TempJob);
        
        // 2. ECB da Sistema di Buffer (raccomandato)
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUpdateAllocator);
        
        // 3. ECB Parallelo per job
        var ecbParallel = ecbSystem.CreateCommandBuffer(state.WorldUpdateAllocator).AsParallelWriter();
        
        // Ricorda: Dispose manuale solo per ECB immediato
        ecbImmediate.Dispose();
    }
}
```

#### Operazioni con EntityCommandBuffer

```csharp
public partial struct ECBOperationsSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUpdateAllocator);
        
        foreach (var (health, entity) in 
                 SystemAPI.Query<RefRO<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.Value <= 0f)
            {
                // Distruzione differita
                ecb.DestroyEntity(entity);
                
                // Spawn effetto morte prima della distruzione
                var deathEffect = ecb.CreateEntity();
                ecb.AddComponent(deathEffect, new LocalTransform 
                { 
                    Position = SystemAPI.GetComponent<LocalTransform>(entity).Position,
                    Scale = 1f
                });
                ecb.AddComponent<DeathEffectTag>(deathEffect);
            }
            else if (health.ValueRO.Value < health.ValueRO.MaxValue * 0.3f)
            {
                // Aggiungi componente "low health" se non presente
                if (!SystemAPI.HasComponent<LowHealthTag>(entity))
                {
                    ecb.AddComponent<LowHealthTag>(entity);
                }
            }
        }
    }
}
```

#### EntityCommandBuffer nei Job

```csharp
[BurstCompile]
public partial struct SpawnerJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    public float DeltaTime;
    public EntityArchetype BulletArchetype;
    
    public void Execute([ChunkIndexInQuery] int chunkIndex, 
                       ref SpawnerData spawner, 
                       in LocalTransform transform)
    {
        spawner.Timer -= DeltaTime;
        
        if (spawner.Timer <= 0f)
        {
            // Usa chunkIndex per thread safety
            var bullet = ECB.CreateEntity(chunkIndex, BulletArchetype);
            
            ECB.SetComponent(chunkIndex, bullet, new LocalTransform
            {
                Position = transform.Position,
                Rotation = transform.Rotation,
                Scale = 1f
            });
            
            ECB.SetComponent(chunkIndex, bullet, new MovementData
            {
                Direction = transform.Forward(),
                Speed = 10f
            });
            
            spawner.Timer = spawner.SpawnRate;
        }
    }
}

public partial struct SpawnerSystem : ISystem
{
    private EntityArchetype bulletArchetype;
    
    public void OnCreate(ref SystemState state)
    {
        bulletArchetype = state.EntityManager.CreateArchetype(
            typeof(LocalTransform),
            typeof(MovementData),
            typeof(BulletTag)
        );
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        
        var spawnerJob = new SpawnerJob
        {
            ECB = ecbSystem.CreateCommandBuffer(state.WorldUpdateAllocator).AsParallelWriter(),
            DeltaTime = SystemAPI.Time.DeltaTime,
            BulletArchetype = bulletArchetype
        };
        
        spawnerJob.ScheduleParallel();
    }
}
```

### Sistemi EntityCommandBuffer Built-in

Unity fornisce diversi sistemi ECB per timing specifici:

```csharp
// All'inizio del frame, prima dell'update dei sistemi
BeginInitializationEntityCommandBufferSystem

// Tra i gruppi di sistemi di simulazione
BeginSimulationEntityCommandBufferSystem
EndSimulationEntityCommandBufferSystem

// Durante il rendering
BeginPresentationEntityCommandBufferSystem
EndPresentationEntityCommandBufferSystem
```

#### Esempio di Timing Corretto

```csharp
public partial struct DamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Usa EndSimulation per modifiche che altri sistemi potrebbero vedere nel prossimo frame
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUpdateAllocator);
        
        foreach (var (health, entity) in 
                 SystemAPI.Query<RefRW<Health>>().WithEntityAccess())
        {
            if (health.ValueRO.Value <= 0f)
            {
                // Questo comando verrà eseguito alla fine del frame
                ecb.DestroyEntity(entity);
                
                // Spawn loot che sarà disponibile dal prossimo frame
                var loot = ecb.CreateEntity();
                ecb.AddComponent<LootTag>(loot);
            }
        }
    }
}
```

### Best Practices: EntityManager vs EntityCommandBuffer

#### Usa EntityManager Quando

- **Immediate Mode:** Hai bisogno di modifiche immediate
- **Main Thread:** Sei nel thread principale e non in un job
- **Setup/Cleanup:** Durante OnCreate o OnDestroy dei sistemi

```csharp
public void OnCreate(ref SystemState state)
{
    // Setup immediato - usa EntityManager
    var config = state.EntityManager.CreateEntity();
    state.EntityManager.AddComponent<GameConfigSingleton>(config);
}
```

#### Usa EntityCommandBuffer Quando

- **Job Execution:** Stai lavorando in job paralleli
- **Batch Operations:** Hai molte operazioni da fare
- **Deferred Execution:** Il timing delle modifiche è importante

```csharp
[BurstCompile]
public partial struct CollisionResponseJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;
    
    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in CollisionEvent collision)
    {
        // Job parallelo - DEVE usare ECB
        ECB.DestroyEntity(chunkIndex, entity);
    }
}
```

### Pattern Comuni e Ottimizzazioni

#### 1. Pooling di Entità

```csharp
public partial struct EntityPoolSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUpdateAllocator);
        
        // Invece di distruggere, disattiva
        foreach (var (_, entity) in 
                 SystemAPI.Query<RefRO<DestroyTag>>().WithEntityAccess())
        {
            ecb.RemoveComponent<DestroyTag>(entity);
            ecb.AddComponent<DisabledTag>(entity);
            ecb.SetComponent(entity, new LocalTransform()); // Reset transform
        }
        
        // Riattiva entità dal pool quando necessario
        foreach (var (poolRequest, entity) in 
                 SystemAPI.Query<RefRO<PoolRequestTag>>().WithEntityAccess())
        {
            if (SystemAPI.HasComponent<DisabledTag>(entity))
            {
                ecb.RemoveComponent<DisabledTag>(entity);
                ecb.RemoveComponent<PoolRequestTag>(entity);
                // Reinizializza componenti...
            }
        }
    }
}
```

#### 2. Creazione Batch Efficiente

```csharp
public partial struct BatchSpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingleton<WaveSpawner>();
        if (spawner.ShouldSpawn)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(state.WorldUpdateAllocator);
            
            // Crea molte entità in batch
            var entities = new NativeArray<Entity>(spawner.Count, Allocator.Temp);
            ecb.CreateEntity(spawner.EnemyArchetype, entities);
            
            // Inizializza tutte le entità
            for (int i = 0; i < entities.Length; i++)
            {
                ecb.SetComponent(entities[i], new LocalTransform 
                { 
                    Position = spawner.GetSpawnPosition(i)
                });
            }
            
            entities.Dispose();
        }
    }
}
```

## 5. Debugging delle Entità

Il pacchetto `Entities` include una serie di strumenti di debugging essenziali per diagnosticare problemi.

- **Familiarizza con gli strumenti:** È fondamentale conoscere le varie finestre di debug e le informazioni che forniscono per monitorare le prestazioni del progetto.
- **Risorse consigliate:**
  - **GDC 2022 Talk:** La presentazione "[DOTS authoring and debugging workflows in the Unity Editor](https://www.youtube.com/watch?v=r-4_9-b2sOA)" è un'ottima introduzione a questi strumenti.
  - **Documentazione Ufficiale:** Per dettagli approfonditi, consulta la sezione [Working in the Editor](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=manual/editor/working-in-the-editor.html) della documentazione di Entities.

## 6. Schema di Implementazione: Dalla Concezione al Codice

### Step-by-Step per Implementare un Oggetto in DOTS

```
┌─────────────────────────────────────────────────────────────┐
│                    WORKFLOW COMPLETO                        │
└─────────────────────────────────────────────────────────────┘

1. ANALISI DEL DESIGN
   ├── Identifica i dati necessari (componenti)
   ├── Definisci i comportamenti (sistemi)
   └── Pianifica le interazioni tra entità

2. CREAZIONE COMPONENTI
   ├── IComponentData per dati base
   ├── IBufferElementData per collezioni
   └── ISharedComponentData per dati condivisi

3. AUTHORING SETUP
   ├── MonoBehaviour per l'editor
   ├── Baker per la conversione
   └── Prefab per il riutilizzo

4. ASPECTS (Opzionale ma Consigliato)
   ├── Raggruppa componenti correlati
   ├── Crea API conveniente
   └── Incapsula logica comune

5. SYSTEMS IMPLEMENTATION
   ├── ISystem per la logica
   ├── Queries per filtrare entità
   └── Jobs per il parallelismo

6. INTEGRATION & TESTING
   ├── Test delle performance
   ├── Debug con DOTS tools
   └── Ottimizzazione finale
```

### Esempio Pratico: Un Proiettile

**1. Component Data:**

```csharp
public struct Projectile : IComponentData
{
    public float Speed;
    public float Damage;
    public float LifeTime;
    public Entity Owner;
}

public struct Lifetime : IComponentData
{
    public float Value;
}
```

**2. Authoring:**

```csharp
public class ProjectileAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public float Damage = 25f;
    public float LifeTime = 5f;
}

public class ProjectileBaker : Baker<ProjectileAuthoring>
{
    public override void Bake(ProjectileAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Projectile
        {
            Speed = authoring.Speed,
            Damage = authoring.Damage,
            LifeTime = authoring.LifeTime
        });
        AddComponent(entity, new Lifetime { Value = authoring.LifeTime });
    }
}
```

**3. Aspect:**

```csharp
public readonly partial struct ProjectileAspect : IAspect
{
    public readonly RefRW<LocalTransform> Transform;
    public readonly RefRO<Projectile> ProjectileData;
    public readonly RefRW<Lifetime> Lifetime;
    
    public void UpdateMovement(float deltaTime)
    {
        var forward = Transform.ValueRO.Forward();
        Transform.ValueRW.Position += forward * ProjectileData.ValueRO.Speed * deltaTime;
        Lifetime.ValueRW.Value -= deltaTime;
    }
    
    public bool ShouldDestroy => Lifetime.ValueRO.Value <= 0f;
}
```

**4. Systems:**

```csharp
public partial struct ProjectileMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        
        foreach (var (projectile, entity) in SystemAPI.Query<ProjectileAspect>().WithEntityAccess())
        {
            projectile.UpdateMovement(deltaTime);
            
            if (projectile.ShouldDestroy)
            {
                ecb.DestroyEntity(state.WorldUpdateAllocator, entity);
            }
        }
    }
}
```

### Checklist di Implementazione

- [ ] **Componenti Definiti:** Tutti i dati necessari sono in struct IComponentData
- [ ] **Authoring Configurato:** Baker converte correttamente GameObject → Entity
- [ ] **Aspects Creati:** Interfacce convenienti per gruppi di componenti correlati
- [ ] **Sistemi Implementati:** Logica di gioco in ISystem con query appropriate
- [ ] **Performance Verificate:** Uso di Job quando necessario, evitare sync points
- [ ] **Debug Setup:** Utilizzare DOTS debug tools per monitoraggio

## 7. ISystem vs SystemBase: Confronto Diretto

Quando si creano sistemi in DOTS, hai due opzioni principali: implementare l'interfaccia `ISystem` o ereditare dalla classe `SystemBase`. Ecco un confronto dettagliato per aiutarti a scegliere l'approccio migliore.

### Caratteristiche Tecniche

| Aspetto | ISystem | SystemBase |
|---------|---------|------------|
| **Tipo** | `struct` che implementa `ISystem` | `class` che eredita da `SystemBase` |
| **Burst Compilation** | ✅ Completamente supportato | ❌ Solo per i job, non il sistema principale |
| **Allocazioni** | Zero allocazioni (value type) | Allocazioni per l'istanza della classe |
| **Ereditarietà** | ❌ Non supportata (struct) | ✅ Supportata (class) |
| **Entities.ForEach** | ❌ Non disponibile | ✅ Disponibile |
| **Stato Interno** | ❌ Stateless per design | ✅ Può mantenere stato tra frame |

### Confronto Pratico: Stesso Sistema, Due Approcci

**Scenario:** Sistema che muove entità basandosi su velocità e direzione.

#### Approccio ISystem (Raccomandato)

```csharp
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Inizializzazione del sistema
        state.RequireForUpdate<MovementData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        // Iterazione esplicita e chiara
        foreach (var (transform, movement) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<MovementData>>())
        {
            transform.ValueRW.Position += movement.ValueRO.Direction * 
                                         movement.ValueRO.Speed * deltaTime;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Cleanup se necessario
    }
}
```

#### Approccio SystemBase (Legacy)

```csharp
public partial class MovementSystemBase : SystemBase
{
    protected override void OnCreate()
    {
        // Inizializzazione del sistema
        RequireForUpdate<MovementData>();
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        
        // Entities.ForEach genera codice automaticamente
        Entities.ForEach((ref LocalTransform transform, in MovementData movement) =>
        {
            transform.Position += movement.Direction * movement.Speed * deltaTime;
        }).ScheduleParallel();
    }
}
```

### Vantaggi Dettagliati di ISystem

#### 1. Performance Superiori

```csharp
// ISystem: Tutto può essere compilato con Burst
[BurstCompile]
public partial struct OptimizedSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Questo codice gira a velocità nativa
        var job = new MovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };
        job.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    public float DeltaTime;
    
    public void Execute(ref LocalTransform transform, in MovementData movement)
    {
        transform.Position += movement.Direction * movement.Speed * DeltaTime;
    }
}
```

#### 2. Controllo Esplicito

```csharp
public partial struct ExplicitControlSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Controllo completo su come iterare
        var query = SystemAPI.QueryBuilder()
            .WithAll<MovementData>()
            .WithNone<Disabled>()
            .Build();

        // Scegli tu: single-thread, job, o chunk iteration
        foreach (var (transform, movement) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<MovementData>>()
                          .WithEntityQueryOptions(EntityQueryOptions.FilterWriteGroup))
        {
            // Logica custom
        }
    }
}
```

#### 3. Design Migliore tramite Composizione

```csharp
// Interfacce per funzionalità riutilizzabili
public interface IMovementCalculator
{
    float3 CalculateMovement(float3 position, float3 direction, float speed, float deltaTime);
}

public struct LinearMovementCalculator : IMovementCalculator
{
    public float3 CalculateMovement(float3 position, float3 direction, float speed, float deltaTime)
    {
        return position + direction * speed * deltaTime;
    }
}

[BurstCompile]
public partial struct ComposedMovementSystem : ISystem
{
    private LinearMovementCalculator calculator;
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (transform, movement) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<MovementData>>())
        {
            transform.ValueRW.Position = calculator.CalculateMovement(
                transform.ValueRO.Position,
                movement.ValueRO.Direction,
                movement.ValueRO.Speed,
                deltaTime);
        }
    }
}
```

### Quando Usare SystemBase

`SystemBase` può ancora essere utile in situazioni specifiche:

#### 1. Migrazione Graduale

```csharp
public partial class LegacySystem : SystemBase
{
    // Durante la transizione da codice esistente
    protected override void OnUpdate()
    {
        // Codice legacy che deve essere gradualmente convertito
        Entities.ForEach((Entity entity, ref OldComponent comp) =>
        {
            // Logica complessa da migrare step-by-step
        }).WithoutBurst().Run();
    }
}
```

#### 2. Debug e Prototipazione Rapida

```csharp
public partial class DebugSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Per debug rapido senza preoccuparsi di Burst compatibility
        Entities.ForEach((Entity entity, ref DebugComponent debug) =>
        {
            Debug.Log($"Entity {entity}: {debug.Value}");
        }).WithoutBurst().Run();
    }
}
```

### Migrazione da SystemBase a ISystem

#### Step 1: Cambia la Struttura Base

```csharp
// DA:
public partial class MySystem : SystemBase
{
    protected override void OnUpdate() { }
}

// A:
[BurstCompile]
public partial struct MySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state) { }
}
```

#### Step 2: Sostituisci Entities.ForEach

```csharp
// DA:
Entities.ForEach((ref Transform transform, in Velocity velocity) =>
{
    transform.Position += velocity.Value * Time.DeltaTime;
}).ScheduleParallel();

// A:
foreach (var (transform, velocity) in 
         SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
{
    transform.ValueRW.Position += velocity.ValueRO.Value * SystemAPI.Time.DeltaTime;
}
```

#### Step 3: Gestisci lo Stato

```csharp
// SystemBase: stato nell'istanza della classe
public partial class StatefulSystemBase : SystemBase
{
    private float timer;
    
    protected override void OnUpdate()
    {
        timer += Time.DeltaTime;
    }
}

// ISystem: usa componenti singleton per lo stato
public struct TimerSingleton : IComponentData
{
    public float Value;
}

[BurstCompile]
public partial struct StatefulISystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var timerSingleton = SystemAPI.GetSingletonRW<TimerSingleton>();
        timerSingleton.ValueRW.Value += SystemAPI.Time.DeltaTime;
    }
}
```

### Raccomandazioni Finali

- **Per Nuovi Progetti:** Usa sempre `ISystem`
- **Per Progetti Esistenti:** Migra gradualmente i sistemi critici per le performance
- **Per Debug Temporaneo:** `SystemBase` può essere accettabile
- **Per Logica Complessa:** `ISystem` + `IJobEntity` per il massimo controllo

L'investimento nella migrazione a `ISystem` porta benefici significativi in termini di performance, chiarezza del codice e manutenibilità a lungo termine.

## 8. Tipi di Job: IJob, IJobParallel e IJobEntity

Il C# Job System è il cuore del parallelismo in DOTS. Permette di scrivere codice sicuro e performante che viene eseguito su più core della CPU. Esistono diversi tipi di job, ognuno adatto a scenari specifici. I tre più comuni sono `IJob`, `IJobParallel` e `IJobEntity`.

### IJob: Un Singolo Task in Background

`IJob` è il tipo di job più semplice. Esegue un singolo blocco di codice (`Execute`) su un unico thread di lavoro.

#### Caratteristiche

-   **Granularità:** Esegue un'unica operazione.
-   **Input:** Può ricevere dati tramite `NativeContainer` (es. `NativeArray`, `NativeList`).
-   **Uso:** Ideale per un singolo calcolo pesante che può essere eseguito in background senza bloccare il thread principale.

#### Caso d'Uso

Immagina di dover calcolare un percorso complesso per un'unica unità speciale o di dover preparare una grande struttura dati prima che altri job la utilizzino.

#### Esempio

```csharp
[BurstCompile]
public struct HeavyCalculationJob : IJob
{
    public NativeArray<float> InputData;
    public NativeArray<float> Result; // Deve avere lunghezza 1

    public void Execute()
    {
        float sum = 0;
        for (int i = 0; i < InputData.Length; i++)
        {
            sum += InputData[i];
        }
        Result[0] = sum;
    }
}

// Scheduling in un sistema
public partial struct CalculationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var input = new NativeArray<float>(1000, Allocator.TempJob);
        var result = new NativeArray<float>(1, Allocator.TempJob);
        
        var job = new HeavyCalculationJob
        {
            InputData = input,
            Result = result
        };
        
        // Schedula il job e ottieni un JobHandle per gestire le dipendenze
        var jobHandle = job.Schedule(state.Dependency);
        
        // Assicurati che il job sia completato prima di usare i risultati
        jobHandle.Complete();
        
        // Usa result[0]...
        
        input.Dispose();
        result.Dispose();
    }
}
```

### IJobParallel: Stesso Task su Molti Dati

`IJobParallel` (spesso implementato come `IJobFor` in versioni più vecchie o `IJobChunk` in contesti specifici) è progettato per eseguire la stessa operazione su ogni elemento di una collezione di dati (`NativeArray`). Il lavoro viene suddiviso automaticamente tra i thread disponibili.

#### Caratteristiche

-   **Granularità:** Esegue la stessa operazione per ogni "indice" di un set di dati.
-   **Input:** Tipicamente un `NativeArray` o un'altra collezione indicizzabile.
-   **Uso:** Perfetto per problemi "imbarazzantemente paralleli", dove ogni elemento può essere processato indipendentemente dagli altri.

#### Caso d'Uso

Aggiornare la posizione di migliaia di particelle, applicare una trasformazione a una lista di vertici, o calcolare il danno per un gruppo di nemici colpiti da un'esplosione.

#### Esempio

```csharp
[BurstCompile]
public struct UpdatePositionsJob : IJobParallel
{
    public NativeArray<float3> Positions;
    [ReadOnly] public NativeArray<float3> Velocities;
    public float DeltaTime;

    // L'indice viene passato automaticamente dal sistema di job
    public void Execute(int index)
    {
        Positions[index] += Velocities[index] * DeltaTime;
    }
}

// Scheduling in un sistema
public partial struct ParticleMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // ... ottieni NativeArray di posizioni e velocità da una query ...
        var positions = new NativeArray<float3>(1000, Allocator.TempJob);
        var velocities = new NativeArray<float3>(1000, Allocator.TempJob);

        var job = new UpdatePositionsJob
        {
            Positions = positions,
            Velocities = velocities,
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        // Schedula il job per essere eseguito su tutti i 1000 elementi.
        // Il 64 è il "batch size", un'indicazione di quanti elementi processare per thread.
        var jobHandle = job.Schedule(positions.Length, 64, state.Dependency);
        
        jobHandle.Complete();
        
        positions.Dispose();
        velocities.Dispose();
    }
}
```

### IJobEntity: Il Job Standard per la Logica di Gioco

`IJobEntity` è l'astrazione di più alto livello e la più utilizzata in DOTS. È progettata per iterare su entità che corrispondono a una query specifica. Sostituisce il vecchio `IJobForEach` ed è il modo raccomandato per implementare la logica di gioco.

#### Caratteristiche

-   **Granularità:** Esegue un'operazione per ogni entità che corrisponde a una query.
-   **Input:** Accede direttamente ai componenti dell'entità, passati come parametri al metodo `Execute`.
-   **Uso:** È il cavallo di battaglia per quasi tutta la logica di gioco: movimento, IA, danno, animazione, etc.

#### Caso d'Uso

Qualsiasi sistema che debba leggere e/o scrivere dati su un gruppo di entità. Ad esempio, un sistema che fa muovere tutti i nemici, un sistema che riduce la vita di tutte le entità avvelenate, o un sistema che fa ruotare tutti i pianeti.

#### Esempio

```csharp
// Il job stesso è una struct parziale che definisce la logica per una singola entità.
[BurstCompile]
public partial struct MovementJob : IJobEntity
{
    public float DeltaTime;

    // I parametri definiscono la query:
    // Il job girerà su tutte le entità che hanno LocalTransform e MovementData.
    // 'ref' per accesso in lettura/scrittura, 'in' per accesso in sola lettura.
    public void Execute(ref LocalTransform transform, in MovementData movement)
    {
        transform.Position += movement.Direction * movement.Speed * DeltaTime;
    }
}

// Scheduling in un sistema
[BurstCompile]
public partial struct MovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var job = new MovementJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        // ScheduleParallel() esegue il job in parallelo su tutte le entità corrispondenti.
        // Il sistema gestisce automaticamente la query e la distribuzione del lavoro.
        job.ScheduleParallel();
    }
}
```

### Tabella di Confronto

| Caratteristica      | IJob                                     | IJobParallel                             | IJobEntity                               |
| ------------------- | ---------------------------------------- | ---------------------------------------- | ---------------------------------------- |
| **Scopo**           | Un singolo task                          | Stesso task su un array di dati          | Stesso task su un set di entità          |
| **Unità di Lavoro** | L'intero metodo `Execute`                | Un singolo `index` in una collezione     | Una singola `Entity` che matcha la query |
| **Iterazione**      | Manuale (es. `for` loop all'interno)     | Automatica, basata su un indice          | Automatica, basata su una query di entità |
| **Accesso ai Dati** | `NativeContainer` passati alla struct    | `NativeArray` acceduti tramite `index`   | Componenti passati come parametri a `Execute` |
| **Scheduling**      | `job.Schedule()`                         | `job.Schedule(length, batchSize)`        | `job.Schedule()` o `job.ScheduleParallel()` |
| **Caso d'Uso Tipico** | Calcolo singolo e pesante in background | Processamento di dati grezzi (particelle) | Logica di gioco (movimento, IA, danno)   |
| **Astrazione**      | Bassa                                    | Media                                    | Alta (specifica per ECS)                 |

### Quale Job Usare?

-   Usa **`IJobEntity`** per il **95% della tua logica di gioco**. È il modo più semplice, sicuro ed efficiente per lavorare con le entità.
-   Usa **`IJobParallel`** quando devi processare dati grezzi in `NativeArray` che non sono (o non devono essere) direttamente legati a entità, come la manipolazione di vertici di una mesh o l'elaborazione di dati di un'immagine.
-   Usa **`IJob`** in rari casi in cui hai un singolo compito computazionalmente intensivo che non si presta a essere parallelizzato su più dati, ma che trarrebbe comunque beneficio dall'essere eseguito su un thread separato per non bloccare il gioco.

## 9. ITriggerEventsJob: Gestione Parallela degli Eventi Trigger

`ITriggerEventsJob` è un tipo di job specifico del pacchetto `Unity.Physics` progettato per processare in modo efficiente e parallelo gli eventi trigger generati dalla simulazione fisica. È l'equivalente in DOTS del classico `OnTriggerEnter`, ma ottimizzato per l'esecuzione multi-thread.

### Perché Usare ITriggerEventsJob?

- **Performance:** Processa centinaia o migliaia di eventi trigger in parallelo, sfruttando tutti i core della CPU.
- **Separazione della Logica:** Isola la logica di risposta alle collisioni dalla logica di gioco principale.
- **Integrazione con DOTS:** Si integra perfettamente con il flusso di lavoro basato su job e `EntityCommandBuffer`.

### Struttura di Base

Un `ITriggerEventsJob` è una `struct` che implementa l'interfaccia e definisce un metodo `Execute`.

```csharp
[BurstCompile]
public struct DamageTriggerJob : ITriggerEventsJob
{
    // Per accedere ai componenti delle entità coinvolte
    public ComponentLookup<Health> HealthLookup;
    public ComponentLookup<DamageZone> DamageZoneLookup;

    public void Execute(TriggerEvent triggerEvent)
    {
        Entity entityA = triggerEvent.EntityA;
        Entity entityB = triggerEvent.EntityB;

        // Logica per determinare chi è il giocatore e chi la zona di danno
        if (HealthLookup.HasComponent(entityA) && DamageZoneLookup.HasComponent(entityB))
        {
            ApplyDamage(entityA, entityB);
        }
        else if (HealthLookup.HasComponent(entityB) && DamageZoneLookup.HasComponent(entityA))
        {
            ApplyDamage(entityB, entityA);
        }
    }

    private void ApplyDamage(Entity target, Entity zone)
    {
        var health = HealthLookup[target];
        var damage = DamageZoneLookup[zone].DamagePerSecond;
        
        // La modifica diretta non è sicura in un job parallelo!
        // Si dovrebbe usare un EntityCommandBuffer o un componente evento.
        // Questo esempio è semplificato per chiarezza.
        health.Value -= damage;
        HealthLookup[target] = health;
    }
}
```

### Componenti Necessari

Per questo esempio, avremo bisogno di alcuni componenti:

```csharp
// Componente per entità che possono subire danni
public struct Health : IComponentData
{
    public float Value;
}

// Componente per una zona che infligge danni
public struct DamageZone : IComponentData
{
    public float DamagePerSecond;
}

// Tag per il giocatore
public struct PlayerTag : IComponentData { }
```

### Sistema di Scheduling

Il job non si esegue da solo. Deve essere schedulato da un sistema, che si occupa anche di passare le dipendenze corrette e i dati necessari come `ComponentLookup`.

```csharp
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct DamageOnTriggerSystem : ISystem
{
    private ComponentLookup<Health> healthLookup;
    private ComponentLookup<DamageZone> damageZoneLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Inizializza i lookup e dichiara le intenzioni di accesso
        healthLookup = state.GetComponentLookup<Health>();
        damageZoneLookup = state.GetComponentLookup<DamageZone>(true); // true = ReadOnly
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Aggiorna i lookup con lo stato corrente del mondo
        healthLookup.Update(ref state);
        damageZoneLookup.Update(ref state);

        var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

        var damageJob = new DamageTriggerJob
        {
            HealthLookup = healthLookup,
            DamageZoneLookup = damageZoneLookup
        };

        // Schedula il job, passando il singleton della simulazione e le dipendenze
        state.Dependency = damageJob.Schedule(simulationSingleton, state.Dependency);
    }
}
```

### Punti Chiave e Best Practices

1.  **`ComponentLookup<T>`:** È il modo corretto e sicuro per accedere ai dati dei componenti all'interno di un job. A differenza dell'accesso diretto, `ComponentLookup` è consapevole del contesto parallelo. Ricorda di chiamare `lookup.Update(ref state)` all'inizio di `OnUpdate`.

2.  **Modifiche Sicure (Thread-Safe):** Modificare direttamente i dati in un `ITriggerEventsJob` (come nell'esempio `health.Value -= ...`) può causare *race condition* se più job tentano di scrivere sullo stesso componente. La soluzione corretta è usare un `EntityCommandBuffer` o aggiungere un "componente evento" che un altro sistema processerà in seguito.

    **Esempio con Componente Evento:**

    ```csharp
    // Componente che rappresenta un evento di danno
    public struct DamageEvent : IBufferElementData
    {
        public float Amount;
    }

    [BurstCompile]
    public struct DamageTriggerJobWithEvent : ITriggerEventsJob
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public ComponentLookup<DamageZone> DamageZoneLookup;
        public ComponentLookup<PlayerTag> PlayerLookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            // ... logica per identificare player e zona ...
            
            // Invece di modificare la vita, aggiungi un evento di danno
            // Usa un indice fittizio (0) perché non stiamo iterando su una query
            ECB.AppendToBuffer(0, playerEntity, new DamageEvent 
            { 
                Amount = damageZone.Damage 
            });
        }
    }
    ```

3.  **Gruppi di Aggiornamento:** Gli `ITriggerEventsJob` dovrebbero essere schedulati dopo la simulazione fisica. Usa `[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]` e `[UpdateAfter(typeof(PhysicsSystemGroup))]` per garantire l'ordine di esecuzione corretto.

4.  **Filtrare le Collisioni:** Per evitare di processare eventi non necessari, puoi configurare i layer di collisione nel `Physics Shape` dell'authoring. Imposta la proprietà `Collides With` per definire con quali altri layer l'entità può interagire, riducendo il numero di eventi generati.