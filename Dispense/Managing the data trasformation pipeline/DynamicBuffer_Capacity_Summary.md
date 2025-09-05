# Gestione della capacità dei DynamicBuffer

I `DynamicBuffer` sono componenti che aggiungono funzionalità simili a quelle di un array alle entità. Possono essere ridimensionati dinamicamente, ad esempio utilizzando il metodo `Add()`.

## Problemi di riallocazione

Come una `List` in C#, un `DynamicBuffer` ha una capacità interna. Se il numero di elementi supera questa capacità, viene riallocato un nuovo array più grande.

- **Capacità iniziale:** Se un `DynamicBuffer` rimane entro la sua capacità iniziale, viene memorizzato direttamente nel chunk.
- **Superamento della capacità:** Se la capacità viene superata, ECS alloca memoria all'esterno del chunk e sposta l'intero `DynamicBuffer`.

Questo introduce diversi problemi:

1. **Costo della riallocazione:** L'allocazione di un nuovo storage e la copia degli elementi esistenti richiede tempo.
2. **Cache miss:** Ogni accesso futuro al `DynamicBuffer` causa un cache miss, poiché i dati non sono più inline nel chunk dell'entità.
3. **Frammentazione del chunk:** Lo spazio precedentemente occupato dal buffer nel chunk rimane vuoto e inutilizzato.

## Impostazione della capacità

### Capacità interna predefinita

La capacità predefinita è calcolata tramite `TypeManager.DefaultBufferCapacityNumerator` (solitamente 128 byte).

### Impostazione tramite attributo

Se si conosce in anticipo la capacità necessaria, è possibile dichiararla con l'attributo `[InternalBufferCapacity]`. Finché il buffer non supera questa capacità, non verrà mai riallocato.

```csharp
// My buffer can contain up to 42 elements inline in the chunk
// If I add any more then ECS will reallocate the buffer onto a heap  
[InternalBufferCapacity(42)]  
public struct MyBufferElement : IBufferElementData  
{  
    public int Value;  
}
```

### Impostazione dinamica della capacità

Quando non è possibile prevedere la capacità al momento della compilazione, si può gestire dinamicamente per evitare allocazioni ad ogni aggiunta.

- **`EnsureCapacity()`**: Forza la riallocazione del buffer in un'area di memoria abbastanza grande da contenere la capacità specificata, evitando riallocazioni multiple.
- **`TrimExcess()`**: Se i buffer dinamici occupano troppa memoria a causa di un padding non più necessario, questo metodo li ridimensiona alla dimensione effettiva.
