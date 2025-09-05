using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FindNearest : MonoBehaviour
{
    // void Update()
    // {
    //     // Find the nearest Target.
    //     // When comparing distances, it's cheaper to compare
    //     // the squares of the distances because doing so
    //     // avoids computing square roots.
    //     foreach (var seekTaransform in Spawner.SeekerTransforms)
    //     {
    //         Vector3 seekerPos = seekTaransform.localPosition;
    //         Vector3 nearestTargetPos = default;
    //         float nearestDistSq = float.MaxValue;

    //         foreach (var targetTransform in Spawner.TargetTransforms)
    //         {
    //             Vector3 offset = targetTransform.localPosition - seekerPos;
    //             float distSq = offset.sqrMagnitude;

    //             if (distSq < nearestDistSq)
    //             {
    //                 nearestDistSq = distSq;
    //                 nearestTargetPos = targetTransform.localPosition;
    //             }
    //         }

    //         Debug.DrawLine(seekerPos, nearestTargetPos);
    //     }
    // }

    //! NEW CODE

    // The size of our arrays does not need to vary, so rather than create
    // new arrays every field, we'll create the arrays in Awake() and store them
    // in these fields.

    NativeArray<float3> TargetPositions;
    NativeArray<float3> SeekerPositions;
    NativeArray<float3> NearestTargetPositions;

    void Start()
    {
        Spawner spawner = Object.FindAnyObjectByType<Spawner>();

        // We use the Persistent allocator because these arrays must
        // exist for the run of the program.

        TargetPositions = new NativeArray<float3>(spawner.NumTargets, Allocator.Persistent);
        SeekerPositions = new NativeArray<float3>(spawner.NumSeekers, Allocator.Persistent);
        NearestTargetPositions = new NativeArray<float3>(spawner.NumTargets, Allocator.Persistent);
    }

    // We are responsible for disposing of our allocations
    // when we no longer need them.

    void OnDestroy()
    {
        TargetPositions.Dispose();
        SeekerPositions.Dispose();
        NearestTargetPositions.Dispose();
    }

    void Update()
    {
        // Copy every target transform to a NativeArray.
        for (int i = 0; i < TargetPositions.Length; i++)
        {
            // Vector3 is implicitly converted to float3
            TargetPositions[i] = Spawner.TargetTransforms[i].localPosition;
        }

        // Copy every seeker transform to a NativeArray.

        for (int i = 0; i < SeekerPositions.Length; i++)
        {
            // Vector3 is implicitly converted to float3
            SeekerPositions[i] = Spawner.SeekerTransforms[i].localPosition;
        }

        // To schedule a job, we first need to create an instance and populate its fields.
        FindNearestJob findJob = new FindNearestJob
        {
            TargetPositions = TargetPositions,
            SeekerPositions = SeekerPositions,
            NearestTargetPosition = NearestTargetPositions
        };

        // Schedule() puts the job instance on the job queue.
        JobHandle findHandle = findJob.Schedule();

        // The Complete method will not return until the job represented by
        // the handle finishes execution. Effectively, the main thread waits
        // here until the job is done.

        findHandle.Complete();

        // Draw a debug line from each seeker to its nearest target.
        for (int i = 0; i < SeekerPositions.Length; i++)
        {
            //float3 is implictly converted to Vector3
            Debug.DrawLine(SeekerPositions[i], NearestTargetPositions[i]);
        }
    }
}
