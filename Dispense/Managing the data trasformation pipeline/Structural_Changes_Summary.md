# Riepilogo: Gestire le Modifiche Strutturali in DOTS

Questo documento riassume perché le **modifiche strutturali** (`structural changes`) in DOTS sono costose e come gestirle per evitare cali di performance.

## 1. Cosa Sono le Modifiche Strutturali e Perché Sono Costose?

Una modifica strutturale avviene ogni volta che si altera l'archetipo di un'entità. Le operazioni più comuni sono:

- Aggiungere o rimuovere un componente.
- Creare o distruggere un'entità.
- Cambiare il valore di uno `SharedComponent`.

Ogni modifica strutturale innesca un processo complesso e potenzialmente costoso:

1. **Ricerca dell'Archetipo**: ECS cerca se esiste già un archetipo per la nuova combinazione di componenti.
2. **Creazione dell'Archetipo**: Se non esiste, ne crea uno nuovo.
3. **Allocazione del Chunk**: Trova o alloca un nuovo chunk di memoria per il nuovo archetipo.
4. **Copia dei Dati**: Copia tutti i dati dei componenti dell'entità dal vecchio chunk al nuovo.
5. **Aggiornamento dell'Entità**: Aggiorna i puntatori interni per far sì che l'entità faccia riferimento alla sua nuova posizione.
6. **Pulizia del Vecchio Chunk**: Rimuove l'entità dal vecchio chunk (spesso con un'operazione di `swap_back`, che può spostare un'altra entità).
7. **Invalidazione della Cache delle Query**: Invalida la cache di tutte le `EntityQuery` che facevano riferimento ai chunk modificati.

Sebbene ogni singolo passo sia veloce, eseguirli per migliaia di entità in un singolo frame può causare un impatto significativo sulle performance, spesso manifestandosi come **sync point** che bloccano il thread principale.

## 2. Linee Guida per la Gestione delle Modifiche Strutturali

### a. Preferire gli `Enableable Components`

L'abilitazione/disabilitazione di un componente che implementa `IEnableableComponent` **non è una modifica strutturale**. È un'operazione estremamente veloce (migliaia di volte più rapida) e dovrebbe essere la prima scelta per cambiare dinamicamente il comportamento di un'entità.

**Svantaggi da considerare:**

- **Performance dei Job**: I chunk con un mix di componenti abilitati e disabilitati possono ridurre la capacità di Burst di vettorizzare il codice, rallentando l'iterazione.
- **Costo di Memoria**: Ogni `EnableableComponent` occupa spazio, riducendo il numero di entità per chunk e potenzialmente aumentando la frammentazione della memoria.
- **Costo su Disco**: Aumentano lo spazio occupato da prefab e subscene.

Per modifiche poco frequenti, aggiungere/rimuovere componenti può ancora essere la scelta migliore.

### b. Usare `EntityManager` con `EntityQuery` per Modifiche di Massa

Quando una modifica strutturale è necessaria, il modo più efficiente per applicarla a molte entità è passare una `EntityQuery` direttamente a `EntityManager` (es. `EntityManager.AddComponent<T>(query)`). Questo metodo opera su interi chunk invece che su singole entità, risultando molto più veloce.

**Attenzione:** Se la query include un `EnableableComponent`, le performance calano drasticamente perché ECS deve prima identificare le singole entità abilitate e poi applicare la modifica una per una.

### c. Usare `EntityCommandBuffer` (ECB) per Evitare Sync Point

Un `EntityCommandBuffer` (ECB) permette di "registrare" comandi di modifica strutturale da eseguire in un secondo momento, tipicamente durante un sync point già esistente (es. `EndSimulationEntityCommandBufferSystem`).

**Perché usare un ECB?**

- **Evita Nuovi Sync Point**: A differenza di `EntityManager`, che esegue le modifiche immediatamente creando un sync point, l'ECB posticipa l'operazione.
- **Utilizzo nei Job Paralleli**: Permette di registrare modifiche strutturali dall'interno di job schedulati e parallelizzati, cosa impossibile con `EntityManager`.

**Svantaggio:** Passare una `EntityQuery` a un ECB è più lento che passarlo a `EntityManager`, perché l'ECB risolve la query in un array di entità al momento della registrazione, perdendo il vantaggio di operare su interi chunk.

### d. Creare Entità in Modo Efficiente

Evitare di costruire entità aggiungendo un componente alla volta. Questo crea archetipi intermedi inutili e causa molteplici modifiche strutturali.

```csharp
// MALE! Causa 3 modifiche strutturali.
var entity = state.EntityManager.CreateEntity();
state.EntityManager.AddComponent<Foo>(entity);
state.EntityManager.AddComponent<Bar>(entity);
state.EntityManager.AddComponent<Baz>(entity);  

// BENE! Crea l'archetipo una volta e poi l'entità.
var newEntityArchetype = state.EntityManager.CreateArchetype(typeof(Foo), typeof(Bar), typeof(Baz));  
var entity = EntityManager.CreateEntity(newEntityArchetype);

// MEGLIO ANCORA! Per creare molte entità identiche.
var entities = new NativeArray<Entity>(10000, Allocator.Temp);
state.EntityManager.CreateEntity(newEntityArchetype, entities);
```

### e. Usare `ComponentTypeSet` per Modifiche Multiple

Per aggiungere o rimuovere più componenti contemporaneamente, usare `ComponentTypeSet` per raggruppare le operazioni in un'unica modifica strutturale.

### f. Disabilitare Sistemi Interi

Se un sistema non deve più processare alcuna entità, invece di rimuovere un componente da tutte le entità, si può disabilitare il sistema stesso usando `SystemState.RequireForUpdate()`.

### g. Profilare le Modifiche Strutturali

Usare il modulo **Structural Changes** nel **Unity Profiler** per monitorare l'impatto delle modifiche strutturali e identificare i colli di bottiglia.
