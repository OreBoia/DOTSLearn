# Guida Completa: ECS e JobSystem in Unity DOTS

## 📋 Indice

1. [Introduzione Teorica](#1-introduzione-teorica)
   - [Entity-Component-System (ECS)](#entity-component-system-ecs)
   - [JobSystem](#jobsystem)
   - [Burst Compiler](#burst-compiler)
2. [Analisi Pratica degli Script](#2-analisi-pratica-degli-script)
   - [Components (IComponentData)](#components-icomponentdata)
   - [Systems](#systems)
   - [Jobs](#jobs)
   - [Authoring e Baking](#authoring-e-baking)

---

## 1. Introduzione Teorica

### Entity-Component-System (ECS)

**ECS** è un paradigma architetturale che separa i dati (Components) dalla logica (Systems) e dagli identificatori degli oggetti (Entities). Questo approccio offre numerosi vantaggi:

#### 🔹 **Entity**

- Un **identificatore univoco** per un oggetto di gioco
- Non contiene dati o logica, è solo un ID
- Può essere creato, distrutto e modificato dinamicamente

#### 🔹 **Component**

- **Contenitore di dati puri** senza logica
- Implementa l'interfaccia `IComponentData` in Unity DOTS
- Definisce le proprietà di un'entità (posizione, velocità, salute, ecc.)

#### 🔹 **System**

- Contiene tutta la **logica di elaborazione**
- Opera su entità che hanno specifici components
- Esegue in parallelo quando possibile per massimizzare le performance

#### 📈 **Vantaggi dell'ECS:**

- **Performance superiori**: Disposizione dei dati cache-friendly
- **Parallelizzazione automatica**: I sistemi possono essere eseguiti in parallelo
- **Flessibilità**: Composizione dinamica delle entità
- **Manutenibilità**: Separazione chiara tra dati e logica

### JobSystem

Il **JobSystem** di Unity permette di **parallelizzare il codice** distribuendo il lavoro su più thread del CPU senza dover gestire manualmente i thread.

#### 🔹 **Tipi di Job principali:**

1. **IJob**: Esegue una singola operazione su un thread separato
2. **IJobParallelFor**: Divide il lavoro in batch ed esegue in parallelo
3. **IJobEntity**: Specifico per ECS, opera su entità con determinati components

#### 🔹 **Caratteristiche chiave:**

- **Thread Safety**: Unity garantisce la sicurezza dei thread
- **Dependencies**: I job possono dipendere da altri job
- **Native Collections**: Uso di NativeArray, NativeList per condividere dati tra thread
- **Burst Compilation**: Ottimizzazione automatica del codice

### Burst Compiler

**Burst** è un compilatore ottimizzante che traduce il codice C# in **codice nativo altamente ottimizzato**.

#### 📊 **Vantaggi:**

- Performance **10-20x superiori** per codice matematico intensivo
- Ottimizzazioni SIMD automatiche
- Compatibile con JobSystem ed ECS

---

## 2. Analisi Pratica degli Script

### Components (IComponentData)

I components sono strutture dati pure che implementano `IComponentData`:

#### 🔹 **RotationSpeed Component**

```csharp
public struct RotationSpeed : IComponentData
{
    public float RadiansPerSeconds;
}
```

**Funzione**: Memorizza la velocità di rotazione di un'entità in radianti per secondo.

#### 🔹 **Tank Component**

```csharp
public struct Tank : IComponentData
{
    public Entity Turret;
    public Entity Cannon;
}
```

**Funzione**: Collega un carro armato alle sue parti (torretta e cannone) tramite riferimenti ad altre entità.

#### 🔹 **Player Component**

```csharp
public struct Player : IComponentData
{
    // Component marker vuoto
}
```

**Funzione**: Component "tag" che identifica l'entità controllata dal giocatore.

#### 🔹 **CannonBall Component**

```csharp
public struct CannonBall : IComponentData
{
    public float3 Velocity;
}
```

**Funzione**: Memorizza la velocità di un proiettile in 3D.

#### 🔹 **SpawnerComponentData**

```csharp
struct SpawnerComponentData : IComponentData
{
    public Entity Prefab;
}
```

**Funzione**: Riferimento al prefab da istanziare.

#### 🔹 **Config Component**

```csharp
public struct Config : IComponentData
{
    public Entity TankPrefab;
    public Entity CannonBallPrefab;
    public int TankCount;
}
```

**Funzione**: Configurazione globale del gioco con prefab e parametri.

### Systems

I systems contengono la logica di elaborazione e operano su entità con specifici components:

#### 🔹 **CubeRotationSystem** (ISystem)

```csharp
public partial struct CubeRotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        foreach (var (trasform, rotationSpeed) in 
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
        {
            var radians = rotationSpeed.ValueRO.RadiansPerSeconds * deltaTime;
            trasform.ValueRW = trasform.ValueRW.RotateY(radians);
        }
    }
}
```

**Caratteristiche**:

- **ISystem**: Nuovo approccio basato su struct per migliori performance
- **BurstCompile**: Compilazione ottimizzata per performance superiori
- **Query**: Seleziona entità con `LocalTransform` e `RotationSpeed`
- **RefRW/RefRO**: Accesso read-write/read-only ai components

#### 🔹 **PlayerSystem** (SystemBase)

```csharp
public partial class PlayerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var inputVector = inputActions.Player.Move.ReadValue<Vector2>();
        var movement = new float3(inputVector.x, 0, inputVector.y) * SystemAPI.Time.DeltaTime * speed;

        Entities
            .WithAll<Player>()
            .ForEach((ref LocalTransform playerTransform) =>
            {
                playerTransform.Position += movement;
            }).ScheduleParallel();
    }
}
```

**Caratteristiche**:

- **SystemBase**: Approccio legacy ma ancora valido
- **Input System**: Integrazione con il nuovo Input System di Unity
- **ScheduleParallel()**: Esecuzione parallela per performance
- **WithAll<Player>()**: Filtra solo entità con component Player

#### 🔹 **ShootingSystem** con Update Order

```csharp
[UpdateBefore(typeof(TransformSystemGroup))]
public partial struct ShootingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Timer per controllare frequenza di sparo
        timer -= SystemAPI.Time.DeltaTime;
        if (timer > 0) return;
        timer = 0.3f;

        // Istanzia proiettili per ogni carro armato
        foreach (var (tank, transform, color) in 
                 SystemAPI.Query<RefRO<Tank>, RefRO<LocalTransform>, RefRO<URPMaterialPropertyBaseColor>>())
        {
            Entity cannonBallEntity = state.EntityManager.Instantiate(config.CannonBallPrefab);
            // ... logica di spawning e configurazione
        }
    }
}
```

**Caratteristiche**:

- **UpdateBefore**: Controlla l'ordine di esecuzione dei sistemi
- **Timer**: Controllo della frequenza di esecuzione
- **Entity Management**: Creazione dinamica di entità
- **Multi-Component Query**: Query su più components contemporaneamente

### Jobs

I Jobs permettono di parallelizzare operazioni intensive:

#### 🔹 **FindNearestJob** (IJob)

```csharp
[BurstCompile]
public struct FindNearestJob : IJob
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float3> SeekerPositions;
    public NativeArray<float3> NearestTargetPosition;

    public void Execute()
    {
        // Calcola la distanza quadrata da ogni seeker a ogni target
        for (int i = 0; i < SeekerPositions.Length; i++)
        {
            float3 seekerPos = SeekerPositions[i];
            float nearestDistSq = float.MaxValue;

            for (int j = 0; j < TargetPositions.Length; j++)
            {
                float3 targetPos = TargetPositions[j]; // Bug: dovrebbe essere [j]
                float distSq = math.distancesq(seekerPos, targetPos);

                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    NearestTargetPosition[i] = targetPos;
                }
            }
        }
    }
}
```

**Caratteristiche**:

- **IJob**: Esecuzione su singolo thread worker
- **ReadOnly**: Attributo per dati di sola lettura (ottimizzazioni di sicurezza)
- **NativeArray**: Container thread-safe per condividere dati
- **BurstCompile**: Ottimizzazione delle performance

#### 🔹 **FindNearestJobParallel** (IJobParallelFor)

```csharp
[BurstCompile]
public struct FindNearestJobParallel : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float3> SeekerPositions;
    public NativeArray<float3> NearestTargetPositions;

    public void Execute(int index)
    {
        float3 seekerPos = SeekerPositions[index];

        // Ottimizzazione: ricerca binaria per target più vicini
        int startIdx = TargetPositions.BinarySearch(seekerPos, new AxisXcomparer { });
        
        // ... logica di ricerca ottimizzata
    }
}
```

**Caratteristiche**:

- **IJobParallelFor**: Parallelizzazione automatica con batch
- **Execute(int index)**: Ogni thread elabora un indice specifico
- **Ottimizzazioni**: Ricerca binaria e ottimizzazioni spaziali
- **Batch Size**: Configurabile per bilanciare overhead e parallelismo

#### 🔹 **CannonBallJob** (IJobEntity)

```csharp
[BurstCompile]
public partial struct CannonBallJob : IJobEntity
{
    public EntityCommandBuffer ECB { get; set; }
    public float DeltaTime { get; set; }

    void Execute(Entity entity, ref CannonBall cannonBall, ref LocalTransform localTransform)
    {
        var gravity = new float3(0.0f, -9.82f, 0.0f);
        
        localTransform.Position += cannonBall.Velocity * DeltaTime;
        
        if (localTransform.Position.y <= 0.0f)
        {
            ECB.DestroyEntity(entity);
        }
        
        cannonBall.Velocity += gravity * DeltaTime;
    }
}
```

**Caratteristiche**:

- **IJobEntity**: Integrazione diretta con ECS
- **Source Generation**: Query automatica basata sui parametri Execute
- **EntityCommandBuffer**: Comandi differiti per modifiche strutturali
- **Entity Parameter**: Accesso diretto all'entità corrente

### Authoring e Baking

Il sistema di Authoring/Baking converte GameObjects in entità ECS:

#### 🔹 **Pattern Authoring/Baker**

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
        
        var rotationSpeed = new RotationSpeed
        {
            RadiansPerSeconds = math.radians(authoring.DegreesPerSecond)
        };
        
        AddComponent(entity, rotationSpeed);
    }
}
```

**Processo**:

1. **Authoring**: MonoBehaviour che espone proprietà nell'Inspector
2. **Baker**: Converte i dati del MonoBehaviour in components ECS
3. **Baking**: Processo di conversione da GameObject a Entity
4. **TransformUsageFlags**: Ottimizza i transform in base all'uso

#### 🔹 **Gestione delle Dipendenze tra Job**

```csharp
// Ordinamento dei target per ottimizzare la ricerca
SortJob<float3, AxisXcomparer> sortJob = TargetPositions.SortJob(new AxisXcomparer { });
JobHandle sortHandle = sortJob.Schedule();

// Job di ricerca che dipende dall'ordinamento
FindNearestJobParallel findJob = new FindNearestJobParallel
{
    TargetPositions = TargetPositions,
    SeekerPositions = SeekerPositions,
    NearestTargetPositions = NearestTargetPositions
};

// Il job di ricerca attende il completamento dell'ordinamento
JobHandle findHandle = findJob.Schedule(SeekerPositions.Length, 100, sortHandle);

// Attesa del completamento prima di usare i risultati
findHandle.Complete();
```

**Caratteristiche**:

- **JobHandle**: Rappresenta un job in esecuzione
- **Dependencies**: I job possono dipendere da altri job
- **Batch Size**: 100 elementi per batch (bilanciamento performance/overhead)
- **Complete()**: Sincronizzazione con il main thread

---

## 🎯 Riepilogo Architetturale

### **Vantaggi dell'Implementazione ECS nel Progetto:**

1. **Performance**: Sistemi ottimizzati con Burst e parallelizzazione
2. **Modularità**: Components riutilizzabili e sistemi specializzati
3. **Scalabilità**: Gestione efficiente di migliaia di entità
4. **Manutenibilità**: Separazione chiara tra dati e logica

### **Pattern Utilizzati:**

- **Data-Oriented Design**: Components come dati puri
- **Job Dependencies**: Coordinamento tra job paralleli
- **Entity Management**: Creazione/distruzione dinamica
- **System Ordering**: Controllo dell'ordine di esecuzione
- **Burst Optimization**: Compilazione nativa per performance critiche

### **Best Practices Implementate:**

1. **Burst Compilation** per tutti i sistemi critici
2. **ReadOnly attributes** per ottimizzazioni di sicurezza
3. **Native Collections** per condivisione dati thread-safe
4. **Entity Command Buffers** per modifiche strutturali differite
5. **Component Composition** per flessibilità del design

Questa architettura rappresenta un esempio eccellente di come Unity DOTS permetta di costruire sistemi altamente performanti e scalabili utilizzando paradigmi moderni di programmazione orientata ai dati.
