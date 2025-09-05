# Esempio di Ottimizzazione SIMD: Frustum Culling Semplice

Questo documento riassume un esempio di ottimizzazione SIMD (Single Instruction, Multiple Data) applicata al frustum culling in un ipotetico gioco. L'obiettivo è migliorare le prestazioni del rendering quando un gran numero di oggetti (100.000 palloni da spiaggia) è presente nella scena.

## Contesto del Problema

Il problema consiste nell'eseguire in modo efficiente il frustum culling per le sfere di delimitazione (bounding spheres) di 100.000 oggetti. Ogni oggetto ha una posizione, un raggio e un componente di visibilità che deve essere aggiornato.

I componenti di base sono:

```csharp
public struct SphereRadius : IComponentData  
{  
    public float Value;  
}  
  
public struct SphereVisible : IComponentData  
{  
    //public bool Value;
    public int Value;  // 1 = visible, 0 = not visible
}
```

## Preparazione: Piani del Frustum

Per prima cosa, i dati che definiscono i sei piani del frustum della telecamera vengono estratti e memorizzati. Un piano è rappresentato da un `float4`, dove `(x,y,z)` sono il vettore normale e `w` è la distanza dall'origine.

```csharp
public struct FrustumCullHelper
{
    static Camera _camera;
    static Plane[] _planesOOP = new Plane[6];

    public static void UpdateFrustumPlanes(ref NativeArray<float4> planes)
    {
        if (_camera == null)
            _camera = Camera.main;
        
        GeometryUtility.CalculateFrustumPlanes(_camera, _planesOOP);
        
        for (int i = 0; i < 6; ++i)
            planes[i] = new float4(_planesOOP[i].normal, _planesOOP[i].distance);
    }
}
```

## Iterazione 1: Implementazione Ingenua (con Loop e Branch)

La prima versione del sistema di culling utilizza un `IJobEntity` per iterare su ogni sfera. All'interno del job, un ciclo `for` scorre i sei piani del frustum. Se la sfera è esterna a un singolo piano, il ciclo viene interrotto (`break`), e la sfera è contrassegnata come non visibile.

**Sistema e Job:**

```csharp
public partial struct FrustumCullSystem : ISystem
{
    private NativeArray<float4> _planes;
    [BurstCompile] public void OnCreate(ref SystemState state)
    {
        _planes = new NativeArray<float4>(6, Allocator.Persistent);
    }
    
    [BurstCompile] public void OnDestroy(ref SystemState state)
    {
        _planes.Dispose();
    }
    
    public void OnUpdate(ref SystemState state)
    { 
        FrustumCullHelper.UpdateFrustumPlanes(ref _planes);
        DoCulling(ref state);
    }
    
    [BurstCompile] public void DoCulling(ref SystemState state)
    { 
        state.Dependency = new CullJob { Planes = _planes }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
    }
}

[BurstCompile(OptimizeFor = OptimizeFor.Performance)]
partial struct CullJob : IJobEntity
{
   [ReadOnly] public NativeArray<float4> Planes;

   void Execute(ref SphereVisible visibility, in LocalToWorld localToWorld, in SphereRadius radius)
   {
       bool visible = true;
       for (int planeID = 0; planeID < 6; ++planeID)
       {
           if (math.dot(Planes[planeID].xyz, localToWorld.Position) +
               Planes[planeID].w + radius.Value <= 0)
           {
               visible = false;
               break;
           }
       }
       visibility.Value = visible ? 1 : 0;
   }
}
```

Questa implementazione, sebbene funzionale, introduce un "branch" (il `break` e l'`if`) nel codice assembly, che può rallentare l'esecuzione sulla CPU.

## Iterazione 2: Rimozione del Branch

Per eliminare il branch, il ciclo sui piani viene "srotolato" (unrolled) in una singola espressione booleana. Tutti e sei i test vengono eseguiti sempre.

```csharp
[BurstCompile(OptimizeFor = OptimizeFor.Performance)]
partial struct CullJob : IJobEntity
{
   [ReadOnly] public NativeArray<float4> Planes;

   void Execute(ref SphereVisible visibility, in LocalToWorld localToWorld, in SphereRadius radius)
   {
       var pos = localToWorld.Position;
       visibility.Value =
           (math.dot(Planes[0].xyz, pos) + Planes[0].w + radius.Value > 0) &&
           (math.dot(Planes[1].xyz, pos) + Planes[1].w + radius.Value > 0) &&
           (math.dot(Planes[2].xyz, pos) + Planes[2].w + radius.Value > 0) &&
           (math.dot(Planes[3].xyz, pos) + Planes[3].w + radius.Value > 0) &&
           (math.dot(Planes[4].xyz, pos) + Planes[4].w + radius.Value > 0) &&
           (math.dot(Planes[5].xyz, pos) + Planes[5].w + radius.Value > 0) ? 1 : 0;
   }
}
```

Anche se esegue più calcoli, questa versione è più veloce perché l'esecuzione è più lineare e prevedibile per la CPU.

## Iterazione 3: Riorganizzazione dei Dati per SIMD (Packing dei Piani)

Per sfruttare le istruzioni SIMD, i dati dei piani vengono riorganizzati. Invece di un array di 6 piani, i dati vengono impacchettati in due `PlanePacket4`, dove ogni campo (`Xs`, `Ys`, `Zs`, `Distances`) contiene i dati corrispondenti di 4 piani diversi.

**Nuove Strutture e Metodi Helper:**

```csharp
public struct PlanePacket4
{
    public float4 Xs;
    public float4 Ys;
    public float4 Zs;
    public float4 Distances;
}

public static void CreatePlanePackets(ref NativeArray<PlanePacket4> planePackets)
{
    var planes = new NativeArray<float4>(6, Allocator.Temp);
    FrustumCullHelper.UpdateFrustumPlanes(ref planes);
    
    int cullingPlaneCount = planes.Length;
    int packetCount = (cullingPlaneCount + 3) >> 2;

    for (int i = 0; i < cullingPlaneCount; i++)
    {
        var p = planePackets[i >> 2];
        p.Xs[i & 3] = planes[i].x;
        p.Ys[i & 3] = planes[i].y;
        p.Zs[i & 3] = planes[i].z;
        p.Distances[i & 3] = planes[i].w;
        planePackets[i >> 2] = p;
    }

    // Popola i piani rimanenti con valori che risultano sempre "dentro"
    for (int i = cullingPlaneCount; i < 4 * packetCount; ++i)
    {
        var p = planePackets[i >> 2];
        p.Xs[i & 3] = 1.0f;
        p.Ys[i & 3] = 0.0f;
        p.Zs[i & 3] = 0.0f;
        p.Distances[i & 3] = 1e9f; // Un numero molto grande
        planePackets[i >> 2] = p;
    }
} 
```

**Sistema e Job Aggiornati:**

```csharp
public partial struct FrustumCullSystem : ISystem
{
    private NativeArray<PlanePacket4> _planePackets;
    // ... OnCreate e OnDestroy aggiornati per usare _planePackets ...
    
    public void OnUpdate(ref SystemState state)
    { 
        FrustumCullHelper.CreatePlanePackets(ref _planePackets);
        DoCulling(ref state);
    }
    
    [BurstCompile] public void DoCulling(ref SystemState state)
    {
        state.Dependency = new CullJob { PlanePackets = _planePackets }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
    }

    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    partial struct CullJob : IJobEntity
    {
        [ReadOnly] public NativeArray<PlanePacket4> PlanePackets;

        void Execute(ref SphereVisible visibility, in LocalToWorld localToWorld, in SphereRadius radius)
        {
            var pos = localToWorld.Position;
            var p0 = PlanePackets[0];
            var p1 = PlanePackets[1];
            
            bool4 masks = (p0.Xs * pos.x + p0.Ys * pos.y + p0.Zs * pos.z + p0.Distances + radius.Value <= 0) |
                          (p1.Xs * pos.x + p1.Ys * pos.y + p1.Zs * pos.z + p1.Distances + radius.Value <= 0);
    
            visibility.Value = masks.Equals(new bool4(false)) ? 1 : 0;
        }
    }
}
```

Questa soluzione esegue il test di una sfera contro 4 piani contemporaneamente, riducendo il numero di operazioni matematiche. Tuttavia, poiché 6 non è un multiplo di 4, il secondo pacchetto di piani contiene dati ridondanti, sprecando potenza di calcolo.

## Iterazione 4: Packing delle Sfere (IJobChunk)

L'approccio più efficiente consiste nell'impacchettare i dati delle sfere anziché quelli dei piani. Utilizzando un `IJobChunk`, è possibile processare 4 sfere alla volta. I dati di posizione e raggio di 4 sfere vengono "mescolati" (shuffled) in un formato verticale (tutte le `x`, tutte le `y`, ecc.) per massimizzare l'uso dei registri SIMD.

```csharp
[BurstCompile] public void DoCulling(ref SystemState state)
{
   var query = SystemAPI.QueryBuilder()
       .WithAll<LocalToWorld, SphereRadius>()
       .WithAllRW<SphereVisible>()
       .Build();
  
   state.Dependency = new CullJob
   {
       LocalToWorldTypeHandle = SystemAPI.GetComponentTypeHandle<LocalToWorld>(true),
       RadiusTypeHandle = SystemAPI.GetComponentTypeHandle<SphereRadius>(true),
       FP = _planes,
       VisibilityTypeHandle = SystemAPI.GetComponentTypeHandle<SphereVisible>()
   }.ScheduleParallel(query, state.Dependency);
   state.Dependency.Complete();
}

[BurstCompile(OptimizeFor = OptimizeFor.Performance)]
partial struct CullJob : IJobChunk
{
   [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorldTypeHandle;
   [ReadOnly] public ComponentTypeHandle<SphereRadius> RadiusTypeHandle;
   [ReadOnly] public NativeArray<float4> FP;
  
   public ComponentTypeHandle<SphereVisible> VisibilityTypeHandle;
  
   public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
   {
       var chunkTransforms = chunk.GetNativeArray(ref LocalToWorldTypeHandle).AsReadOnly();
       var chunkRadii = chunk.GetNativeArray(ref RadiusTypeHandle).Reinterpret<float>();
       var chunkVis = chunk.GetNativeArray(ref VisibilityTypeHandle);
      
       var p0 = FP[0];
       var p1 = FP[1];
       var p2 = FP[2];
       var p3 = FP[3];
       var p4 = FP[4];
       var p5 = FP[5];

       // Processa le sfere in batch di 4
       for (var i = 0; chunk.Count - i >= 4; i += 4)
       {
           // Carica e "mescola" 4 posizioni
           var a = chunkTransforms[i].Position;
           var b = chunkTransforms[i+1].Position;
           var c = chunkTransforms[i+2].Position;
           var d = chunkTransforms[i+3].Position;
           var Xs = new float4(a.x, b.x, c.x, d.x);
           var Ys = new float4(a.y, b.y, c.y, d.y);
           var Zs = new float4(a.z, b.z, c.z, d.z);
          
           // Carica 4 raggi
           var Radii = chunkRadii.ReinterpretLoad<float4>(i);
          
           // Esegui il test di 6 piani contro 4 sfere
           bool4 mask =
               p0.x * Xs + p0.y * Ys + p0.z * Zs + p0.w + Radii > 0.0f &
               p1.x * Xs + p1.y * Ys + p1.z * Zs + p1.w + Radii > 0.0f &
               p2.x * Xs + p2.y * Ys + p2.z * Zs + p2.w + Radii > 0.0f &
               p3.x * Xs + p3.y * Ys + p3.z * Zs + p3.w + Radii > 0.0f &
               p4.x * Xs + p4.y * Ys + p4.z * Zs + p4.w + Radii > 0.0f &
               p5.x * Xs + p5.y * Ys + p5.z * Zs + p5.w + Radii > 0.0f;

           chunkVis.ReinterpretStore(i, new int4(mask));
       }

       // Processa le sfere rimanenti individualmente
       for (var i = (chunk.Count >> 2) << 2; i < chunk.Count; ++i)
       {
           var pos = chunkTransforms[i].Position;
           var radius = chunkRadii[i];

           int visible =
               (math.dot(p0.xyz, pos) + p0.w + radius > 0.0f &&
                math.dot(p1.xyz, pos) + p1.w + radius > 0.0f &&
                math.dot(p2.xyz, pos) + p2.w + radius > 0.0f &&
                math.dot(p3.xyz, pos) + p3.w + radius > 0.0f &&
                math.dot(p4.xyz, pos) + p4.w + radius > 0.0f &&
                math.dot(p5.xyz, pos) + p5.w + radius > 0.0f) ? 1 : 0;
          
           chunkVis[i] = new SphereVisible { Value = visible };
       }
   }
}
```

Questo è l'approccio più veloce perché minimizza il numero totale di operazioni, anche se richiede un lavoro extra per impacchettare e spacchettare i dati ad ogni frame.

## Conclusioni sulle Prestazioni

| Versione | Descrizione | Operazioni Matematiche (stima) | Tempo CPU Mediano |
|---|---|---|---|
| 1 | Loop sui piani con `break` | ~1,100,000 | 0.24 ms |
| 2 | Loop srotolato (no `break`) | 1,600,000 | 0.22 ms |
| 3 | Piani impacchettati (SIMD) | 600,000 | 0.14 ms |
| 4 | Sfere impacchettate (SIMD) | 475,000 | 0.11 ms |

- **Il conteggio delle operazioni è un buon predittore delle prestazioni**: meno operazioni di solito significano maggiore velocità.
- **La rimozione dei branch è vantaggiosa**: la versione 2 è più veloce della 1 nonostante esegua più calcoli, grazie all'assenza di salti condizionali.
- **La versione 4 è la più veloce**: impacchettare i dati delle sfere sfrutta al meglio le capacità SIMD della CPU. La sua velocità non è 4 volte superiore a causa del costo aggiuntivo dello "shuffling" dei dati ad ogni frame. Se i dati potessero essere mantenuti in un formato SIMD-friendly per tutto il tempo, le prestazioni
