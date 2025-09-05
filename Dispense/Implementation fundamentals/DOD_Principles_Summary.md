# Riepilogo dei Principi di Data-Oriented Design (DOD)

Questo documento riassume i principi chiave per una corretta separazione tra dati e sistemi quando si utilizza il Data-Oriented Technology Stack (DOTS) di Unity.

## 1. Separare i Dati dai Sistemi

A differenza dell'OOP, dove dati e metodi sono incapsulati insieme, il DOD si basa sulla separazione tra le strutture di dati (componenti) e la logica che opera su di essi (sistemi).

- **Regola Generale:** Nessun metodo nei componenti, nessun dato nei sistemi.

### Attenzione ai Metodi sui Componenti

Avere metodi sui componenti è un anti-pattern in DOD perché incoraggia a operare su un singolo elemento alla volta, perdendo i benefici prestazionali del DOD, che derivano dalla trasformazione di interi buffer di dati.

- **Rendi i dati pubblici:** I dati dei componenti dovrebbero essere pubblici.
- **Scrivi loop nei sistemi:** I sistemi dovrebbero contenere i loop che iterano su più componenti ed eseguono le operazioni direttamente sui dati.

Se inserisci metodi nei componenti, ottieni uno di questi due risultati:

1. **Metodo non inlinato:** Subisci un calo di performance a causa della chiamata di metodo extra per ogni componente.
2. **Metodo inlinato:** Il codice generato è identico a quello che avresti scritto direttamente nel sistema, quindi non ottieni alcun vantaggio.

### Attenzione ai Dati nei Sistemi

La regola "nessun dato nei sistemi" è più sfumata. I sistemi dovrebbero essere trasformazioni di dati e memorizzare il minor stato possibile (idealmente nessuno).

- **La fonte di verità sono i componenti:** È accettabile che un sistema metta in cache i dati in strutture efficienti per l'elaborazione, a patto che questa cache venga ricreata ogni volta che i dati dei componenti potrebbero essere cambiati.
- **Non fare affidamento sulla durata delle entità:** Le entità possono essere create o distrutte in qualsiasi momento. La cache non deve sopravvivere ai componenti da cui è stata generata.
- **Usa la "System Entity":** Invece di memorizzare dati come campi di un sistema, è meglio memorizzarli in un componente associato alla "System Entity" del sistema stesso.

**Esempi di Caching Corretto:**

- **Unity Physics:** Ricostruisce una struttura di hash spaziale ogni frame per il rilevamento delle collisioni. I dati sono transitori.
- **Entities Graphics:** Mantiene una lista di batch di rendering che viene aggiornata in reazione alla creazione o distruzione dei componenti `RenderMesh`, garantendo la sincronizzazione con lo stato del mondo.

È comune per i sistemi mettere in cache elementi legati al loro funzionamento interno, come `EntityQuery` o riferimenti ad altri sistemi in `OnCreate()`.

## 2. Evitare Dati Statici Mutabili

L'uso di dati statici è una delle cause principali dei lunghi tempi di iterazione in Unity, a causa del "Domain Reloading" all'ingresso/uscita dal Play Mode.

- **DOTS è progettato per evitare dati statici:** Se il tuo progetto evita l'uso di dati statici, puoi disabilitare il "Domain Reloading" e ridurre drasticamente i tempi di iterazione durante lo sviluppo.
- **Risorsa:** Per maggiori dettagli, consulta il post sul blog di Unity [Enter Play Mode faster in Unity 2019.3](https://blog.unity.com/technology/enter-play-mode-faster-in-unity-2019-3) e il post sul forum di Joachim Ante.
