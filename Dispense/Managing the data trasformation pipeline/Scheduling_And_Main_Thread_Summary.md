# Riepilogo: Schedulazione dei Job e Gestione del Main Thread in DOTS

Questo documento riassume come schedulare i job in modo efficiente e come gestire il lavoro che deve essere eseguito sul thread principale (main thread) per massimizzare le performance in DOTS.

## 1. Schedulare i Job per il Multithreading

Per sfruttare le moderne CPU multi-core, è essenziale spostare quanto più lavoro possibile dal thread principale. In DOTS, questo si ottiene tramite il **Job System**. Esistono tre modi per schedulare un job:

1. **`ScheduleParallel()`**: È il metodo preferito. Esegue un job (o un `Entities.ForEach`) suddividendo il lavoro su **tutti i core disponibili** della CPU. Va usato ogni volta che l'algoritmo e l'accesso ai dati possono essere parallelizzati in sicurezza.

2. **`Schedule()`**: Da usare quando `ScheduleParallel()` non è possibile. Esegue il lavoro su un **singolo thread**, ma che non è il thread principale (a meno che il main thread non sia bloccato in attesa di altre dipendenze). È un'ottima alternativa per evitare di bloccare il thread principale.

3. **`Run()`**: È l'ultima risorsa. Esegue il job **sincronamente sul thread principale**, bloccandolo. Prima di eseguire il job, il Job System forza il completamento di tutte le dipendenze già schedulate, creando un **sync point**.

## 2. Sync Point e Structural Changes

I **sync point** sono momenti nel frame in cui il Job System deve attendere il completamento dei job, bloccando il thread principale. Possono compromettere gravemente le performance e vanno gestiti con attenzione.

La causa principale dei sync point sono le **structural changes** (modifiche strutturali) nell'ECS World, che devono avvenire sul thread principale per evitare race condition. Le modifiche strutturali includono:

- Creare o distruggere entità.
- Aggiungere o rimuovere componenti da un'entità.
- Cambiare il valore di uno `SharedComponent`.

### La Soluzione: Entity Command Buffers (ECB)

Per evitare di eseguire modifiche strutturali immediate (e quindi creare un sync point), si usano gli **Entity Command Buffers (ECB)**. Un ECB permette di "registrare" una serie di comandi per modifiche strutturali. Questi comandi vengono poi eseguiti tutti insieme in un momento specifico del frame da un `EntityCommandBufferSystem` (come il comunemente usato `EndSimulationEntityCommandBufferSystem`).

## 3. Gestire il Lavoro sul Main Thread con i `SystemGroup`

A volte, è inevitabile eseguire del lavoro sul thread principale, specialmente quando si interagisce con oggetti gestiti (managed) come `GameObject` e `MonoBehaviour`. Un caso d'uso comune è copiare dati dal mondo OOP a DOTS all'inizio del frame e viceversa alla fine.

La pratica migliore è **raggruppare questo lavoro in `SystemGroup` specifici** per evitare di introdurre sync point nel mezzo della pipeline di simulazione. Si possono creare gruppi che vengono eseguiti all'inizio e alla fine del `SimulationSystemGroup`.

Ecco un esempio di come separare i processi su dati gestiti da quelli su dati non gestiti:

```csharp
using Unity.Entities; 

// Un gruppo che viene eseguito all'inizio del SimulationSystemGroup
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)] 
[UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))] 
public partial class PreSimulationSystemGroup : ComponentSystemGroup { }

// Un gruppo che viene eseguito alla fine del SimulationSystemGroup
[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)] 
[UpdateAfter(typeof(EndSimulationEntityCommandBufferSystem))] 
public partial class PostSimulationSystemGroup : ComponentSystemGroup { }

// Sistema per copiare dati da oggetti managed a componenti ECS
[UpdateInGroup(typeof(PreSimulationSystemGroup))] 
public partial class CopyManagedDataToECSSystem : SystemBase 
{ 
   // Copia i dati da MonoBehaviours nei componenti
   protected override void OnUpdate() { } 
} 

// Sistema che processa la simulazione ECS (può essere multithread)
[UpdateInGroup(typeof(SimulationSystemGroup))] 
public partial struct ProcessDataSystem : ISystem 
{ 
   // Processa la simulazione ECS
   public void OnUpdate(ref SystemState state) { } 
} 

// Sistema per copiare i risultati da ECS agli oggetti managed
[UpdateInGroup(typeof(PostSimulationSystemGroup))] 
public partial struct CopyECSToManagedDataSystem : ISystem 
{ 
   // Copia i dati processati dai componenti agli oggetti managed
   public void OnUpdate(ref SystemState state) { } 
}
```

Questo approccio isola il lavoro sul thread principale ai margini del frame, permettendo alla simulazione principale di essere eseguita in parallelo senza interruzioni.
