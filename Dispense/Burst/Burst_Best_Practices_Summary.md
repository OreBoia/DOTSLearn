# Best Practice per Burst e Unity.Mathematics

## 1. Utilizzare Unity.Mathematics

Invece della tradizionale API `Mathf`, è fondamentale utilizzare il pacchetto `Unity.Mathematics` per tutte le operazioni matematiche nel codice DOTS. Lo stesso vale per i tipi di dati:

- Usa `float3` invece di `Vector3`
- Usa `quaternion` invece di `Quaternion`
- Usa `float4x4` invece di `Matrix4x4`

**Perché?** I tipi di dati in `Unity.Mathematics` sono la base per le ottimizzazioni SIMD (Single Instruction, Multiple Data) implementate dal compilatore Burst. Il codice che utilizza `Unity.Mathematics` è notevolmente più veloce quando compilato con Burst rispetto alle controparti OOP di `UnityEngine.Mathf`.

## 2. Comprendere gli Operatori

Gli operatori aritmetici per i tipi di `Unity.Mathematics` spesso si comportano in modo diverso rispetto a quelli per i tipi di `UnityEngine`. Per i tipi SIMD come `float3` o `float4x4`, quasi tutti gli operatori aritmetici vengono applicati *component-wise* (elemento per elemento).

Questo è particolarmente importante per le matrici:

- `Matrix4x4.operator *`: Esegue una moltiplicazione matriciale standard.
- `float4x4.operator *`: Esegue un'operazione *component-wise*.
- Per una moltiplicazione matriciale standard con `float4x4`, si deve usare la funzione `math.mul()`.

## 3. Sapere Cosa e Come Compilare con Burst

Dovresti usare Burst per compilare quanto più codice DOTS possibile. Burst può compilare qualsiasi codice che aderisce agli standard di High-Performance C# (HPC#).

Per indicare a Burst di compilare un metodo o una struct, si usa l'attributo `[BurstCompile]`. Nel codice ECS, questo significa generalmente aggiungerlo a:

- **Struct dei Job** (ma non al metodo `Execute()` al loro interno).
- **Metodi `OnCreate()`, `OnUpdate()`, `OnDestroy()` in un `ISystem`** (ma non alla struct `ISystem` stessa).

## 4. Incorporare i Generatori `Random` nei Componenti

La struct `Unity.Mathematics.Random` è un modo efficiente per generare numeri pseudo-casuali. Tuttavia, richiede attenzione nel codice multithread.

**Problema comune:** Creare una singola istanza di `Random` sul thread principale e passarla ai job. Questo va evitato. Poiché i dati vengono copiati per ogni job, ogni thread avrà la propria copia della struct `Random` originale, tutte inizializzate con lo stesso *seed*. Questo non è affatto casuale e, peggio ancora, lo stato non viene mai copiato indietro, portando i job a generare sempre gli stessi numeri "casuali" ad ogni frame.

**Soluzione semplice ed efficace:** Avere un'istanza di `Random` per ogni entità, memorizzata in un componente. I job possono usare questa istanza per generare i numeri casuali necessari per quella specifica entità. In questo modo, lo stato del generatore sarà unico per ogni entità e persisterà finché il componente esisterà.
