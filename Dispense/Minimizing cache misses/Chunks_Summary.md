# Comprensione dei Chunk

## Struttura e Organizzazione in Memoria

L'organizzazione dei componenti in memoria in DOTS segue una gerarchia precisa:

- **`EntityQuery`**: Utilizzata per filtrare entità e componenti su cui operare. Contiene una lista di `EntityArchetype`.
- **`EntityArchetype`**: Descrive un gruppo di entità che corrispondono a una query. Contiene una lista di `ArchetypeChunk` (comunemente chiamati "chunk").
- **`ArchetypeChunk` ("chunk")**: Un buffer di 16KB in memoria non gestita. Contiene i componenti per un certo numero di entità che corrispondono a un archetipo specifico.
- **`Entity`**: Un semplice indice che punta a un chunk specifico e a una posizione al suo interno, dove risiedono i dati dei componenti dell'entità.

## Frammentazione dei Chunk

La frammentazione dei chunk si verifica quando i chunk dell'archetipo non vengono utilizzati in modo efficiente. Questo porta a due problemi principali:

1. **Spreco di memoria**: Se i chunk non sono pieni, lo spazio rimanente viene sprecato.
2. **Cache miss**: Ogni volta che un sistema deve saltare da un chunk a un altro per processare l'entità successiva, si verifica un cache miss, rallentando le prestazioni.

Un esempio estremo: 100.000 entità, ognuna con un archetipo unico, occuperebbero ciascuna un chunk diverso, portando a un consumo di oltre 1.5GB di memoria (in gran parte vuota) e a un cache miss per ogni singola entità processata.

## Cause Comuni di Frammentazione

### 1. Componenti Condivisi (Shared Components)

Un uso scorretto dei componenti condivisi è una causa comune di frammentazione. I componenti condivisi sono utili per raggruppare un gran numero di entità in un numero relativamente piccolo di sottogruppi che non cambiano spesso.

**Regole per l'uso corretto dei componenti condivisi:**

- È utile per i tuoi sistemi operare su sottogruppi individuali.
- Esiste un numero relativamente piccolo di questi sottogruppi.
- La memoria risparmiata utilizzando componenti condivisi è maggiore della memoria persa a causa della creazione di più archetipi.

### 2. Prefab

I prefab possono causare frammentazione perché hanno un archetipo diverso dalle entità che istanziano.

- Le entità prefab hanno un componente `Prefab` che le fa ignorare implicitamente dalle `EntityQuery`.
- Quando un prefab viene istanziato, questo componente viene rimosso dalla nuova copia.
- Di conseguenza, il prefab originale e le sue istanze hanno archetipi diversi.
- Ogni prefab caricato con un archetipo differente occupa il proprio chunk da 16KB, che può sommarsi rapidamente, specialmente in giochi con generazione procedurale.

### 3. Entità "Pesanti" (Heavy Entities)

Un'entità "pesante" è un'entità con un gran numero di componenti o con componenti che contengono molti dati.

- Un numero minore di entità pesanti può entrare in un singolo chunk da 16KB.
- Iterare su un gran numero di queste entità significa attraversare più confini di chunk, causando più cache miss.

**Soluzione**: Scomporre le entità pesanti in un numero maggiore di entità più leggere. Invece di rappresentare un "oggetto" del mondo di gioco (come un personaggio) con una singola entità, si possono dividere i suoi componenti in entità separate in base al `SystemGroup` che li processa (es. IA, pathfinding, fisica, animazione). In questo modo, ogni sistema itera su chunk pieni solo dei dati di cui ha bisogno, migliorando l'efficienza.
