using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerSystem : SystemBase
{
    private DefaultInputActions inputActions;
    
    protected override void OnCreate()
    {
        inputActions = new DefaultInputActions();
        inputActions.Enable();
    }

    protected override void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
        }
    }

    protected override void OnUpdate()
    {
        float speed = 5.0f;
        
        var inputVector = inputActions.Player.Move.ReadValue<Vector2>();
        var movement = new float3(inputVector.x, 0, inputVector.y) * SystemAPI.Time.DeltaTime * speed;

        // // Debug per verificare l'input
        // if (math.lengthsq(movement) > 0)
        // {
        //     Debug.Log($"Input: {inputVector}, Movement: {movement}");
        // }

        CompleteDependency();

        Entities
            .WithAll<Player>()
            .ForEach((ref LocalTransform playerTransform) =>
            {
                playerTransform.Position += movement;
            }).ScheduleParallel();

        CompleteDependency();

        Entities
            .WithAll<Player>()
            .WithoutBurst()
            .ForEach((in LocalTransform playerTransform) =>
            {
                // Muovi la camera per seguire il giocatore
                var cameraTransform = Camera.main.transform;
                var playerPos = playerTransform.Position;
                
                cameraTransform.position = (Vector3)playerPos - 10.0f * (Vector3)playerTransform.Forward() + new Vector3(0, 5f, 0);
                cameraTransform.LookAt(playerPos);
            }).Run();
    }
}