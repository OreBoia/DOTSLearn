# Riepilogo: Dichiarare Dati in Sola Lettura in DOTS

Questo documento riassume l'importanza e le tecniche per dichiarare i dati come `read-only` (sola lettura) il più spesso possibile quando si lavora con il Data-Oriented Technology Stack (DOTS) di Unity.

## 1. Perché Dichiarare Dati in Sola Lettura?

Dichiarare esplicitamente quali dati sono in sola lettura per un determinato sistema è fondamentale. Questa informazione permette al **Job System** di schedulare i job in modo molto più efficiente, parallelizzando le operazioni e sfruttando al massimo i core della CPU. Una corretta dichiarazione `read-only` previene race condition e migliora drasticamente le performance.

## 2. Tecniche per Dichiarare Dati Read-Only

### a. Usare Blob Asset per Dati di Configurazione

Per dati che sono al 100% `read-only`, come i dati di configurazione, la pratica migliore è usare i **Blob Asset**.

- **Creazione:** I dati vengono impacchettati in un Blob Asset usando un `BlobBuilder`.
- **Serializzazione:** È ancora più efficiente serializzare il Blob Asset direttamente in un file binario durante la build. Al runtime, il file viene semplicemente caricato e deserializzato, evitando costose operazioni di parsing da altri formati.

### b. Dichiarare l'Accesso nelle Query

Le query sono il modo per specificare su quali entità e componenti un sistema deve operare. È essenziale dichiarare correttamente l'accesso `read-only`.

- **`foreach` con `SystemAPI.Query()`:**
  - Usa `RefRO<T>` per componenti in sola lettura.
  - Usa `RefRW<T>` per componenti in lettura/scrittura.

- **`IJobEntity`:**
  - Nei parametri del metodo `Execute()`, usa la keyword `in` per dichiarare un componente come `read-only`.
  - Usa `ref` per i componenti che devono essere modificati.

- **`EntityQueryBuilder` (o `SystemAPI.QueryBuilder()`):**
  - Usa `WithAll<T>()` per indicare che un componente è richiesto in sola lettura.
  - Usa `WithAllRW<T>()` per specificare l'accesso in lettura/scrittura.

### c. Usare l'Attributo `[ReadOnly]` nei Job

Quando si definisce una struct per un `IJob`, tutti i campi che non vengono modificati dal metodo `Execute()` devono essere marcati con l'attributo `[ReadOnly]`.

```csharp
[BurstCompile]
public partial struct MyJob : IJobEntity
{
   [ReadOnly] public ComponentLookup<MyData> MyDataLookup;
   // ...
}
```

### d. Usare Versioni Read-Only di `ComponentLookup` e `BufferLookup`

Per l'accesso casuale a componenti o buffer (simile a un dizionario), si usano `ComponentLookup` e `BufferLookup`.

- Quando ottieni il lookup, passa `true` come parametro per indicare che l'accesso sarà solo in lettura:

    ```csharp
    var myLookup = SystemAPI.GetComponentLookup<MyComponent>(true); // true per read-only
    ```

- Quando si usa con `Entities.ForEach()`, è necessario aggiungere anche `WithReadOnly()` alla definizione del ForEach per garantire la sicurezza in parallelo.

## 3. Esempio Pratico: `FollowSpline`

Immaginiamo un sistema che muove entità lungo una spline.

### a. Definizione dei Dati (Componenti)

Per prima cosa, definiamo i componenti e i dati necessari.

```csharp
public struct FollowingSplineTag : IComponentData { }

public struct SplinePath : IComponentData
{
   public Entity Spline;
   public float Distance;
}

public struct SplinePointsBuffer : IBufferElementData
{
   public float3 SplinePoint;
}

public struct SplineLength : IComponentData
{
   public float Value;
}

public struct SplineHelper
{
   public static LocalTransform FollowSpline(DynamicBuffer<SplinePointsBuffer> pointsBuf, float length, float distance)
   {
       // Esegui il calcolo della spline e restituisci un nuovo LocalTransform qui
       return LocalTransform.Identity; // Placeholder
   }
}
```

### b. Implementazione con `foreach` in `ISystem` (Main Thread)

Questa è l'implementazione più semplice che viene eseguita sul thread principale.

```csharp
// All'interno di OnUpdate() in un ISystem
var lengthLookup = SystemAPI.GetComponentLookup<SplineLength>(true);
var pointsBufferLookup = SystemAPI.GetBufferLookup<SplinePointsBuffer>(true);

foreach (var (transform, path) in
        SystemAPI.Query<RefRW<LocalTransform>, RefRO<SplinePath>>()
        .WithAll<FollowingSplineTag>())
{
   var splineLength = lengthLookup[path.ValueRO.Spline].Value;
   var pointsBuf = pointsBufferLookup[path.ValueRO.Spline];
   transform.ValueRW = SplineHelper.FollowSpline(pointsBuf, splineLength, path.ValueRO.Distance);
}
```

### c. Implementazione con `IJobEntity` (Multithread)

Questa versione utilizza un `IJobEntity` per eseguire il lavoro in parallelo su più thread.

```csharp
// Dichiarazione del Job
[BurstCompile]
[WithAll(typeof(FollowingSplineTag))]
public partial struct FollowSplineJob : IJobEntity
{
   [ReadOnly] public ComponentLookup<SplineLength> LengthLookup;
   [ReadOnly] public BufferLookup<SplinePointsBuffer> PointsBufferLookup;
  
   public void Execute(ref LocalTransform transform, in SplinePath path)
   {
       var splineLength = LengthLookup[path.Spline].Value;
       var pointsBuf = PointsBufferLookup[path.Spline];
       transform = SplineHelper.FollowSpline(pointsBuf, splineLength, path.Distance);
   }
}

// Schedulazione del Job in OnUpdate()
var job = new FollowSplineJob
{
   LengthLookup = SystemAPI.GetComponentLookup<SplineLength>(true),
   PointsBufferLookup = SystemAPI.GetBufferLookup<SplinePointsBuffer>(true)
};
job.ScheduleParallel();
```

### d. Implementazione con `Entities.ForEach` in `SystemBase`

Infine, ecco come apparirebbe lo stesso codice usando `Entities.ForEach` in un `SystemBase` (approccio meno recente).

```csharp
// All'interno di OnUpdate() in un SystemBase
var lengthLookup = GetComponentLookup<SplineLength>(true);
var pointsBufferLookup = GetBufferLookup<SplinePointsBuffer>(true);

Entities
    .WithAll<FollowingSplineTag>()
    .WithReadOnly(lengthLookup)
    .WithReadOnly(pointsBufferLookup)
    .ForEach((ref LocalTransform transform, in SplinePath path) =>
    {
        var splineLength = lengthLookup[path.Spline].Value;
        var pointsBuf = pointsBufferLookup[path.Spline];
        transform = SplineHelper.FollowSpline(pointsBuf, splineLength, path.Distance);
    }).ScheduleParallel();
```
