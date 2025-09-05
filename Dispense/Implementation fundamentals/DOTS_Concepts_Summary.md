# Riepilogo dei Concetti Chiave di DOTS

Questo documento riassume due concetti importanti per lavorare con il Data-Oriented Technology Stack (DOTS) di Unity.

## 1. Debugging delle Entità

Il pacchetto `Entities` include una serie di strumenti di debugging essenziali per diagnosticare problemi.

- **Familiarizza con gli strumenti:** È fondamentale conoscere le varie finestre di debug e le informazioni che forniscono per monitorare le prestazioni del progetto.
- **Risorse consigliate:**
  - **GDC 2022 Talk:** La presentazione "[DOTS authoring and debugging workflows in the Unity Editor](https://www.youtube.com/watch?v=r-4_9-b2sOA)" è un'ottima introduzione a questi strumenti.
  - **Documentazione Ufficiale:** Per dettagli approfonditi, consulta la sezione [Working in the Editor](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=manual/editor/working-in-the-editor.html) della documentazione di Entities.

## 2. Preferire `ISystem` a `SystemBase`

Quando si creano nuovi sistemi, l'approccio raccomandato è implementare l'interfaccia `ISystem` invece di ereditare dalla classe `SystemBase`.

### Principali Vantaggi di `ISystem`

- **Prestazioni Migliori:**
  - Essendo `ISystem` una `struct` senza stato proprio, l'intero sistema può essere compilato con **Burst**, migliorando notevolmente le prestazioni sul thread principale.

- **Iterazione Esplicita e Pulita:**
  - `ISystem` non supporta `Entities.ForEach()`. Al suo posto, si utilizzano:
    - `foreach` idiomatico per iterare sulle entità nel thread principale.
    - `IJobEntity` o `IJobChunk` per operazioni multi-thread.
  - L'abbandono di `Entities.ForEach()` riduce la generazione automatica di codice, che rallenta i tempi di iterazione e introduce comportamenti inaspettati.

- **Design del Codice Migliore:**
  - **Nessuna Ereditarietà:** `ISystem` non supporta l'ereditarietà, incoraggiando l'uso della composizione tramite interfacce, che porta a un design più efficiente.
  - **Chiarezza e Ottimizzazione:** Il risultato è un codice più pulito, esplicito e facilmente ottimizzabile.

Anche se `SystemBase` è ancora disponibile per facilitare l'aggiornamento di progetti esistenti, si consiglia di convertire i sistemi al nuovo formato `ISystem` per beneficiare di questi vantaggi.
