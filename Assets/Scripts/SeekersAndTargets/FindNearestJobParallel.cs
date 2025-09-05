using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UIElements;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

[BurstCompile]
public struct FindNearestJobParallel : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float3> SeekerPositions;

    public NativeArray<float3> NearestTargetPositions;

    // An IJobParallelFor's Execute() method takes an index parameter and 
    // is called once for each index, from 0 up to the index count:
    public void Execute(int index)
    {
        float3 seekerPos = SeekerPositions[index];

        // Find the target with the closest X coord.
        int startIdx = TargetPositions.BinarySearch(seekerPos, new AxisXcomparer { });

        // When no precise match is found, BinarySearch returns the bitwise negation of the last-searched offset.
        // So when startIdx is negative, we flip the bits again, but we then must ensure the index is within bounds.
        if (startIdx < 0) startIdx = ~startIdx;
        if (startIdx >= TargetPositions.Length) startIdx = TargetPositions.Length - 1;

        // The position of the target with the closest X coord.
        float3 nearestTargetPos = TargetPositions[startIdx];
        float nearestDistSq = math.distancesq(seekerPos, nearestTargetPos);

        // Searching upwards through the array for a closer target.
        Search( seekerPos,
                startIdx + 1,
                TargetPositions.Length,
                +1,
                ref nearestTargetPos,
                ref nearestDistSq);

        // Search downwards through the array for a closer target.
        Search( seekerPos,
                startIdx - 1,
                -1,
                -1,
                ref nearestTargetPos,
                ref nearestDistSq);

        NearestTargetPositions[index] = nearestTargetPos;
    }

    private void Search(float3 seekerPos,
                        int startIdx,
                        int endIdx,
                        int step,
                        ref float3 nearestTargetPos,
                        ref float nearestDistSq)
    {
        for (int i = startIdx; i != endIdx; i += step)
        {
            float3 targetPos = TargetPositions[i];
            float xdiff = seekerPos.x - targetPos.x;

            // If the square of the x distance is greater than the current nearest, we can stop searching.
            if ((xdiff * xdiff) > nearestDistSq) break;

            float distSq = math.distancesq(targetPos, seekerPos);

            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearestTargetPos = targetPos;
            }
        }
    }
}

public struct AxisXcomparer : IComparer<float3>
{
    public int Compare(float3 a, float3 b)
    {
        return a.x.CompareTo(b.x);
    }
}