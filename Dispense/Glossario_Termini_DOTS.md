# Glossario dei Termini Importanti DOTS

Questo documento contiene una raccolta completa di tutti i termini importanti trovati nei file della cartella Dispense, organizzati alfabeticamente con le loro descrizioni.

## A

### **Aliasing**

Fenomeno che si verifica quando due o più riferimenti o puntatori nel codice puntano alla stessa area di memoria, creando ambiguità per il compilatore. L'attributo `[NoAlias]` può essere usato per informare Burst che due riferimenti non saranno mai alias.

### **Allocator**

Sistema di gestione della memoria per i NativeContainer. Tipi principali:

- **`Allocator.Temp`**: Per buffer temporanei necessari solo per il frame corrente (più veloce)
- **`Allocator.TempJob`**: Per NativeContainer utilizzati all'interno di job (dura max 4 frame)
- **`Allocator.Persistent`**: Per allocazioni che devono durare più di 4 frame (più lento)

### **ArchetypeChunk (Chunk)**

Buffer di 16KB in memoria non gestita che contiene i componenti per un numero specifico di entità che corrispondono a un archetipo specifico. È l'unità base di organizzazione dei dati in ECS.

## B

### **Blob Asset**

Struttura dati immutabile utilizzata per dati al 100% read-only, come i dati di configurazione. Vengono creati con un `BlobBuilder` e possono essere serializzati in file binari per efficienza.

### **Branch (Diramazione)**

Istruzione condizionale nel codice assembly (come `if`, `switch`, `break`) che può rallentare l'esecuzione sulla CPU. Le ottimizzazioni SIMD spesso mirano a eliminare i branch.

### **Burst**

Compilatore di Unity che ottimizza il codice High-Performance C# (HPC#) traducendolo in codice assembly altamente ottimizzato. Supporta le istruzioni SIMD e può migliorare drasticamente le performance.

### **Burst Inspector**

Strumento per visualizzare il codice assembly generato da Burst, utile per verificare se il codice è stato vettorizzato correttamente e identificare ottimizzazioni SIMD.

## C

### **Cache Miss**

Situazione che si verifica quando la CPU deve accedere a dati che non sono presenti nella cache, causando rallentamenti. Minimizzare i cache miss è fondamentale per le performance in DOTS.

### **ComponentLookup**

Struttura che permette l'accesso casuale a componenti (simile a un dizionario), può essere configurata per accesso read-only passando `true` come parametro.

### **ComponentSystemGroup**

Gruppo di sistemi che vengono eseguiti insieme, permettendo di organizzare la pipeline di trasformazione dei dati e controllare l'ordine di esecuzione.

## D

### **Data-Oriented Design (DOD)**

Paradigma di programmazione che separa i dati dai sistemi, focalizzandosi sulla trasformazione efficiente di grandi quantità di dati piuttosto che sull'incapsulamento OOP.

### **Domain Reloading**

Processo che avviene in Unity all'ingresso/uscita dal Play Mode. DOTS permette di disabilitarlo per ridurre i tempi di iterazione evitando dati statici mutabili.

### **DynamicBuffer**

Componente che fornisce funzionalità simili a un array dinamico alle entità. Ha una capacità interna e può essere ridimensionato, ma il superamento della capacità causa riallocazioni costose.

## E

### **Entity**

Semplice indice che punta a un chunk specifico e a una posizione al suo interno, dove risiedono i dati dei componenti dell'entità.

### **EntityArchetype**

Descrizione di un gruppo di entità che condividono la stessa combinazione di tipi di componenti. Contiene una lista di ArchetypeChunk.

### **EntityCommandBuffer (ECB)**

Buffer che permette di registrare comandi di modifica strutturale da eseguire in un secondo momento, evitando sync point immediati e permettendo l'uso in job paralleli.

### **EntityManager**

Sistema centrale per la gestione delle entità, usato per operazioni immediate di modifica strutturale come creare/distruggere entità o aggiungere/rimuovere componenti.

### **EntityQuery**

Sistema per filtrare entità e componenti su cui operare, contiene una lista di EntityArchetype che corrispondono ai criteri specificati.

### **EnableableComponent**

Componente che implementa `IEnableableComponent` e può essere abilitato/disabilitato senza modifiche strutturali, operazione migliaia di volte più veloce dell'aggiunta/rimozione.

## F

### **Frammentazione dei Chunk**

Situazione in cui i chunk dell'archetipo non vengono utilizzati efficacemente, causando spreco di memoria e cache miss. Spesso causata da uso scorretto di Shared Components o entità "pesanti".

### **Frustum Culling**

Tecnica di ottimizzazione del rendering che determina quali oggetti sono visibili nel campo visivo della telecamera, spesso usata come esempio per ottimizzazioni SIMD.

## H

### **High-Performance C# (HPC#)**

Sottoinsieme del linguaggio C# che utilizza solo dati "blittable" e rispetta le restrizioni che permettono la compilazione tramite Burst, evitando tipi riferimento.

## I

### **IJobChunk**

Interfaccia per job che operano su interi chunk di entità, permettendo ottimizzazioni SIMD elaborando più entità contemporaneamente.

### **IJobEntity**

Interfaccia per job che operano su singole entità ma possono essere facilmente parallelizzati, più semplice da usare rispetto a IJobChunk.

### **ISystem**

Interfaccia raccomandata per creare sistemi ECS. Essendo una struct, può essere compilata interamente con Burst, offrendo performance migliori rispetto a SystemBase.

## J

### **Job System**

Sistema di Unity per la gestione del multithreading, permette di schedulare lavoro su thread worker evitando il thread principale.

### **JobHandle**

Riferimento a un job schedulato, utilizzato per gestire dipendenze tra job e per il corretto rilascio della memoria con `Dispose(JobHandle)`.

## M

### **Managed Component**

Componente dichiarato come class invece che struct, contiene riferimenti a oggetti gestiti. Sconsigliato per nuovi progetti perché impedisce l'uso di Burst.

### **Memory Leak**

Perdita di memoria che si verifica quando la memoria nativa allocata non viene rilasciata manualmente con `Dispose()`, non essendo gestita dal garbage collector.

## N

### **NativeContainer**

Strutture dati che gestiscono memoria nativa (non gestita), come NativeArray, NativeList. Devono essere rilasciate manualmente per evitare memory leak.

### **[NoAlias]**

Attributo utilizzato per informare Burst che due riferimenti o puntatori non saranno mai alias, permettendo ottimizzazioni più aggressive.

## O

### **Overhead**

Costo computazionale aggiuntivo associato alla gestione di job e sistemi. Importante bilanciare il lavoro utile con l'overhead per ottimizzare le performance.

## P

### **PlanePacket4**

Struttura utilizzata nelle ottimizzazioni SIMD per impacchettare i dati di 4 piani geometrici in formato verticale, massimizzando l'uso delle istruzioni SIMD.

## R

### **Read-Only Data**

Dati dichiarati esplicitamente in sola lettura usando `RefRO<T>`, `in`, `[ReadOnly]`, o `true` nei lookup. Permette al Job System di schedulare più efficacemente i job in parallelo.

### **[RequireMatchingQueriesForUpdate]**

Attributo che previene l'esecuzione di un sistema quando non ci sono entità che corrispondono alle sue query, utile per sistemi spesso inattivi.

## S

### **Schedule()**

Metodo per eseguire un job su un singolo thread (non il main thread), alternativa quando `ScheduleParallel()` non è possibile.

### **ScheduleParallel()**

Metodo preferito per eseguire job, suddivide il lavoro su tutti i core disponibili della CPU per massima parallelizzazione.

### **Shared Component**

Componente condiviso tra multiple entità per risparmiare memoria, ma può causare frammentazione se usato impropriamente.

### **SIMD (Single Instruction, Multiple Data)**

Tecnica di ottimizzazione che sfrutta la capacità della CPU di eseguire operazioni matematiche su un `float4` con la stessa velocità di un singolo `float`.

### **Structural Changes**

Modifiche che alterano l'archetipo di un'entità (aggiungere/rimuovere componenti, creare/distruggere entità, cambiare SharedComponent). Sono costose e possono causare sync point.

### **Sync Point**

Momento nel frame in cui il Job System deve attendere il completamento dei job, bloccando il thread principale. Compromette le performance e va evitato.

### **SystemBase**

Classe base legacy per i sistemi ECS, meno efficiente di ISystem perché non può essere completamente compilata con Burst.

## U

### **Unity.Mathematics**

Pacchetto matematico ottimizzato per DOTS che fornisce tipi come `float3`, `quaternion`, `float4x4` al posto dei corrispondenti di UnityEngine, essenziale per le ottimizzazioni SIMD.

## V

### **Vettorizzazione**

Processo automatico o manuale di ottimizzazione del codice per utilizzare istruzioni SIMD, identificabile nel Burst Inspector dalle istruzioni che finiscono in `ps` (es. `addps`, `mulps`).

### **Verticale (Uso di Unity.Mathematics)**

Tecnica di ottimizzazione SIMD che usa tre `float4` per rappresentare i componenti x, y, z di 4 vettori diversi invece di usare un `float3` per un singolo vettore.

## W

### **WithAll<T>()**

Metodo per specificare che un componente è richiesto in sola lettura in una EntityQuery.

### **WithAllRW<T>()**

Metodo per specificare l'accesso in lettura/scrittura a un componente in una EntityQuery.

---

*Questo glossario copre i termini più importanti per lavorare efficacemente con il Data-Oriented Technology Stack (DOTS) di Unity, inclusi Entity Component System (ECS), Job System, Burst, e le relative ottimizzazioni.*
