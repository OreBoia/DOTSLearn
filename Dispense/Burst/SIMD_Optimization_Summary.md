# Ottimizzazione SIMD con Burst

Quando le ottimizzazioni standard non sono sufficienti, specialmente per job che eseguono intense operazioni matematiche su grandi set di dati (es. culling custom, manipolazione di dati di pixel/vertici), è il momento di considerare l'ottimizzazione SIMD.

## Cos'è SIMD?

**SIMD** sta per **Single Instruction, Multiple Data**. In termini di `Unity.Mathematics`, è una tecnica che sfrutta la capacità della CPU di eseguire alcune operazioni matematiche su un `float4` con la stessa velocità con cui le esegue su un singolo `float`.

## Verificare la Vettorizzazione Automatica con il Burst Inspector

Burst è abbastanza bravo a vettorizzare automaticamente il codice in molti casi. Il primo passo è sempre controllare l'assembly generato per il tuo job tramite il **Burst Inspector**.

Anche senza essere esperti di assembly, è possibile farsi un'idea della complessità del job:

- **Istruzioni SIMD (Buono):** Cerca istruzioni che finiscono in `ps` (es. `addps`, `mulps`). Se ne vedi molte concatenate tra loro, significa che il codice è stato vettorizzato.
- **Istruzioni Scalari (Cattivo):** Cerca istruzioni che finiscono in `ss` (es. `addss`, `mulss`). Queste operano su singoli valori e indicano una mancanza di vettorizzazione.
- **Mix di Istruzioni (Cattivo):** Un mix di istruzioni `ps` e `ss` indica una vettorizzazione parziale.

L'obiettivo è rifattorizzare il codice per aiutare il compilatore Burst a eliminare quante più istruzioni scalari (`ss`) possibile.

## Best Practice per Scrivere Codice SIMD-friendly

1. **Familiarizza con il Burst Inspector**: Usalo per avere un'idea di quanto sia ottimale il tuo codice e per avere una base di confronto mentre apporti modifiche.
2. **Elimina i Branch (Diramazioni)**: Le CPU sono più veloci nell'eseguire una sequenza lineare di istruzioni piuttosto che codice con diramazioni condizionali (`if`, `switch`, ecc.).
3. **Preferisci Batch di Dati più Ampi**: In linea con i principi DOD, processa buffer di molti elementi piuttosto che elementi singoli.
4. **Usa `Unity.Mathematics` in Verticale**: Questo è un concetto chiave per massimizzare il throughput. Invece di usare un `float3` per rappresentare un singolo vettore (x, y, z), usa tre `float4` per rappresentare i componenti di 4 vettori diversi:
    - Un `float4` per i valori `x` di 4 vettori.
    - Un `float4` per i valori `y` di 4 vettori.
    - Un `float4` per i valori `z` di 4 vettori.
    In questo modo, puoi processare 4 vettori con lo stesso codice che altrimenti ne processerebbe uno solo.
