# Riepilogo: Controllare l'Ordine dei Sistemi in DOTS

Questo documento riassume come gestire e controllare l'ordine di esecuzione dei sistemi (System Ordering) in un progetto che utilizza il Data-Oriented Technology Stack (DOTS) di Unity.

## 1. La Pipeline di Trasformazione dei Dati

Ogni frame in un progetto ECS può essere visto come una **pipeline di trasformazione dei dati**. Il frame inizia con uno stato della simulazione al tempo `T`. I sistemi operano su questi dati, lanciando job per produrre lo stato della simulazione al tempo `T+1`. Gestire efficacemente questa pipeline significa controllare quali sistemi vengono eseguiti e in quale ordine.

## 2. Comportamento di Default e Controllo Manuale

Di default, ECS esegue i nuovi sistemi all'interno del `SimulationSystemGroup`. L'ordine esatto all'interno di questo gruppo viene deciso da Unity in modo arbitrario (ma deterministico).

Per ottenere un controllo preciso, è necessario definire esplicitamente l'ordine di esecuzione. La pratica migliore consiste nel:

1. **Creare i propri `ComponentSystemGroup`:** Raggruppare i sistemi con logiche correlate in gruppi personalizzati, che possono essere annidati all'interno dei gruppi di default come `SimulationSystemGroup`.
2. **Usare gli Attributi di Ordinamento:** Specificare l'ordine di ogni sistema rispetto agli altri.

### Attributi per il Controllo dell'Ordine

- **`[UpdateInGroup(typeof(MySystemGroup))]`**: Specifica a quale `ComponentSystemGroup` appartiene un sistema.
- **`[UpdateBefore(typeof(OtherSystem))]`**: Assicura che il sistema venga eseguito **prima** di `OtherSystem`.
- **`[UpdateAfter(typeof(OtherSystem))]`**: Assicura che il sistema venga eseguito **dopo** `OtherSystem`.

## 3. Esempio Pratico: Controllo di un Personaggio

Consideriamo un tipico flusso di controllo per un personaggio in un gioco, dove la reattività è fondamentale. L'input del giocatore deve influenzare lo stato e le animazioni del personaggio all'interno dello stesso frame.

1. **`InputSystem`**: Legge e processa gli input del giocatore all'inizio del frame.
2. **`StateMachineSystem`**: Utilizza gli input per aggiornare lo stato del personaggio (es. da "inattivo" a "in corsa").
3. **`AnimationSystem`**: Aggiorna le animazioni del personaggio per riflettere il nuovo stato.

Questo ordine garantisce che il personaggio reagisca istantaneamente agli input.

Ecco come si traduce in codice usando l'attributo `[UpdateAfter]`:

```csharp
// Questo sistema viene eseguito per primo (o secondo l'ordine di default nel suo gruppo)
public partial struct InputSystem : ISystem  
{  
    // ...  
}  

// Questo sistema viene eseguito dopo InputSystem
[UpdateAfter(typeof(InputSystem))]  
public partial struct StateMachineSystem : ISystem  
{  
    // ...  
}  

// Questo sistema viene eseguito dopo StateMachineSystem, e quindi anche dopo InputSystem
[UpdateAfter(typeof(StateMachineSystem))]  
public partial struct AnimationSystem : ISystem  
{  
    // ...  
}
```

Utilizzando questi attributi, si crea una catena di dipendenze chiara e prevedibile, rendendo la pipeline di trasformazione dei dati robusta e facile da gestire.
