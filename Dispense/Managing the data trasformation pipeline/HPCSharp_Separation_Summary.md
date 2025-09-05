# Riepilogo: Separare High-Performance C# (HPC#) da C# Standard in DOTS

Questo documento spiega l'importanza di separare il codice C# ad alte prestazioni (HPC#), compatibile con Burst, dal codice C# standard che interagisce con oggetti gestiti (managed objects).

## 1. Il Principio della Separazione

Per ottenere le massime prestazioni in DOTS, è fondamentale scrivere codice che sia **High-Performance C# (HPC#)**. Questo significa utilizzare dati "blittable" e rispettare le restrizioni che permettono la compilazione tramite **Burst**.

- **HPC#**: Codice che non utilizza tipi riferimento (classi), consentendo a Burst di ottimizzarlo e al Job System di eseguirlo in parallelo.
- **C# Standard**: Codice che interagisce con oggetti gestiti come `MonoBehaviour` o `Animator`. Questo codice non può essere compilato con Burst e deve essere eseguito sul thread principale.

**L'obiettivo è isolare il più possibile queste due tipologie di codice.** Mescolare codice DOTS con tipi riferimento impedisce di sfruttare Burst e la parallelizzazione, vanificando gran parte dei benefici prestazionali.

### Managed `IComponentData`

È possibile dichiarare un `IComponentData` come una `class` invece che come una `struct`. Questo crea un **managed component**, utile come misura temporanea durante la migrazione di un progetto da codice managed a ECS. Per i nuovi progetti, è una pratica da evitare. È possibile disabilitare questa funzionalità aggiungendo `UNITY_DISABLE_MANAGED_COMPONENTS` agli "Scripting Defines" nelle impostazioni del player.

## 2. Esempio: Un Approccio Inefficiente

Consideriamo un sistema che gestisce la logica di un'IA e aggiorna un `Animator` di un `GameObject`.

```csharp
public enum CharacterActivity { Idle, Run, Jump, Shoot }

public class AnimAIState : IComponentData
{
   public Animator Animator; // Un riferimento a un componente managed
   public CharacterActivity Activity;
}

public partial struct AnimAISystem : ISystem
{
   public void OnUpdate(ref SystemState state)
   {
       foreach (var animAIState in SystemAPI.Query<AnimAIState>())
       {
           animAIState.Activity = SomeComplexStateMachineLogic();
           animAIState.Animator.SetInteger("State", (int)animAIState.Activity);
       }
   }
}
```

**Il problema:** Il riferimento all'`Animator` rende il componente `AnimAIState` un "managed component" (una classe). Di conseguenza:

- Il componente non è "blittable".
- Il sistema non può essere compilato con Burst.
- Il lavoro deve essere eseguito interamente sul thread principale tramite un `foreach`.

Anche se la `SomeComplexStateMachineLogic()` fosse scritta in puro HPC#, non potrebbe beneficiare delle ottimizzazioni di Burst, perdendo un'enorme opportunità di performance.

## 3. Esempio: Un Approccio Ottimizzato e Separato

La soluzione consiste nel separare i dati e i sistemi.

1. **Dati Separati**: Creiamo un componente `struct` per i dati della logica e un componente `class` solo per il riferimento all'oggetto managed.
2. **Sistemi Separati**: Creiamo un sistema HPC# per la logica pesante e un sistema non-HPC# per la sola interazione con l'oggetto managed.

```csharp
// Componente managed solo per il riferimento
public partial class AnimatorRef : IComponentData { public Animator Animator; }

// Componente struct per i dati della logica (blittable)
public partial struct AIState : IComponentData { public CharacterActivity Activity; };

// Job Burst-compilato per la logica pesante
[BurstCompile]
public partial struct AIStateJob : IJobEntity
{
   public void Execute(ref AIState aiState)
   {
       aiState.Activity = SomeComplexStateMachineLogic();
   }
}

// Sistema che schedula il job in parallelo
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct AIStateSystem : ISystem
{
   [BurstCompile]
   public void OnUpdate(ref SystemState state)
   {
       new AIStateJob().ScheduleParallel();
   }
}

// Sistema che gira sul main thread per aggiornare l'Animator
[UpdateInGroup(typeof(PostSimulationSystemGroup))]
public partial struct AnimatorRefSystem : ISystem
{
   public void OnUpdate(ref SystemState state)
   {
       foreach (var (AIState, animatorRef) in SystemAPI.Query<RefRO<AIState>, AnimatorRef>())
       {
           animatorRef.Animator.SetInteger("State", (int)AIState.ValueRO.Activity);
       }
   }
}
```

**I vantaggi di questo approccio:**

- La logica complessa (`AIStateJob`) viene eseguita in un job **Burst-compilato e parallelizzato**, sfruttando al massimo la CPU.
- Il sistema `AnimatorRefSystem`, che deve interagire con un oggetto managed, viene eseguito sul thread principale ma svolge un compito minimo: leggere il risultato e passarlo all'Animator.
- Usando `PostSimulationSystemGroup`, ci assicuriamo che `AnimatorRefSystem` venga eseguito **dopo** il completamento del job, utilizzando il sync point di fine frame per sincronizzare i dati.

Questo design isola il lavoro lento e non parallelizzabile, permettendo alla parte più pesante della logica di essere eseguita con prestazioni massime.
