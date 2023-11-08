using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct EatJob : IJobParallelFor
{
  [ReadOnly, NativeDisableParallelForRestriction]
  public NativeArray<Vector3> FishPositions;
  [ReadOnly, NativeDisableParallelForRestriction]
  public NativeArray<Vector3> FishTargetPositionsArray;
  [ReadOnly, NativeDisableParallelForRestriction]
  public NativeArray<int> FishTargetIndexArray;
  
  [NativeDisableParallelForRestriction]
  public NativeArray<bool> MovingToInterestPoint;
  [NativeDisableParallelForRestriction]
  public NativeArray<bool> ReachToInterestPoints;
  
  [NativeDisableParallelForRestriction]
  public NativeArray<bool> TargetActive;
  [NativeDisableParallelForRestriction]
  public NativeArray<float> TargetTime;
  
  [NativeDisableParallelForRestriction]
  public NativeArray<Vector3> CalculateMiddlePoint;
  [NativeDisableParallelForRestriction]
  public NativeArray<int> FishDeactivateTarget;

  [ReadOnly]
  public float TimeAtInterestPoint;
  [ReadOnly]
  public float deltaTime;
  
  public void Execute (int index)
  {
    if (ReachToInterestPoints[index])
    {
      for (int i = 0; i < FishPositions.Length; i++)
      {
        if (i == index)
        {
          continue;
        }

        if (!ReachToInterestPoints[i] && FishTargetPositionsArray[i] != FishTargetPositionsArray[index])
        {
          continue;
        }

        float middleX = (FishPositions[index].x + FishPositions[i].x) / 2;
        float middleY = (FishPositions[index].y + FishPositions[i].y) / 2;
        float middleZ = (FishPositions[index].z + FishPositions[i].z) / 2;
        CalculateMiddlePoint[index] = new Vector3(middleX, middleY, middleZ);

        break;
      }
      
      bool eatingComplete = !TargetActive[FishTargetIndexArray[index]];
      if (!eatingComplete)
      {
        TargetTime[FishTargetIndexArray[index]] += deltaTime;
      }

      eatingComplete = TargetTime[FishTargetIndexArray[index]] >= TimeAtInterestPoint;
      if (!eatingComplete)
      {
        return;
      }

      ReachToInterestPoints[index] = false;
      MovingToInterestPoint[index] = false;

      FishDeactivateTarget[index] = FishTargetIndexArray[index];
    }
  }
}