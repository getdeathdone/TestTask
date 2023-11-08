using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct MoveJob : IJob
{
  [ReadOnly]
  public int index;
  [ReadOnly]
  public int FishCount;

  public NativeArray<Vector3> FishPositions;
  public NativeArray<Quaternion> FishRotation;
  public NativeArray<bool> FishMovingToInterestPoint;
  public NativeArray<bool> FishReachToInterestPoints;

  [ReadOnly]
  public NativeArray<Vector3> FishTargetPositions;

  [ReadOnly]
  public float AvoidanceRadius;
  [ReadOnly]
  public float AlignmentDistance;
  [ReadOnly]
  public float CohesionRadius;
  [ReadOnly]
  public float StoppingReachDistance;
  [ReadOnly]
  public float StoppingMovingDistance;
  [ReadOnly]
  public float CohesionWeight;
  [ReadOnly]
  public float Speed;
  [ReadOnly]
  public float RotationSpeed;
  [ReadOnly]
  public float deltaTime;

  [ReadOnly]
  public Vector3 AreaCenter;
  [ReadOnly]
  public Vector3 AreaSize;

  public void Execute()
  {
    Vector3 avoidanceMove = Vector3.zero;
    Vector3 alignmentMove = Vector3.zero;
    Vector3 cohesionMove = Vector3.zero;

    for (int i = 0; i < FishCount; i++)
    {
      if (i == index)
      {
        continue;
      }
      
      if (FishMovingToInterestPoint[i])
      {
        continue;
      }

      float distance = Vector3.Distance(FishPositions[index], FishPositions[i]);

      if (distance < AvoidanceRadius)
      {
        Vector3 avoidVector = FishPositions[index] - FishPositions[i];
        avoidanceMove += avoidVector.normalized;
      }

      if (distance < AlignmentDistance)
      {
        alignmentMove += FishRotation[i] * Vector3.forward;
      }

      if (distance < CohesionRadius)
      {
        cohesionMove += FishPositions[i];
      }
    }

    if (cohesionMove != Vector3.zero)
    {
      cohesionMove /= FishCount;
      cohesionMove -= FishPositions[index];
    }

    Vector3 targetDirection = (FishTargetPositions[index] - FishPositions[index]).normalized;

    float distanceToTarget = Vector3.Distance(FishPositions[index], FishTargetPositions[index]);
    FishReachToInterestPoints[index] = distanceToTarget <= StoppingReachDistance;
    FishMovingToInterestPoint[index] = distanceToTarget <= StoppingMovingDistance;

    Vector3 moveDirection = FishMovingToInterestPoint[index] ? targetDirection : targetDirection + avoidanceMove + alignmentMove + cohesionMove.normalized * CohesionWeight;

    Vector3 newPosition = FishPositions[index] + moveDirection.normalized * Speed * deltaTime;

    float halfX = AreaSize.x / 2;
    float halfY = AreaSize.y / 2;
    float halfZ = AreaSize.z / 2;

    if (newPosition.x < AreaCenter.x - halfX || newPosition.x > AreaCenter.x + halfX)
    {
      newPosition.x = Mathf.Clamp(newPosition.x, AreaCenter.x - halfX, AreaCenter.x + halfX);
      moveDirection.x *= -1;
    }

    if (newPosition.y < AreaCenter.y - halfY || newPosition.y > AreaCenter.y + halfY)
    {
      newPosition.y = Mathf.Clamp(newPosition.y, AreaCenter.y - halfY, AreaCenter.y + halfY);
      moveDirection.y *= -1;
    }

    if (newPosition.z < AreaCenter.z - halfZ || newPosition.z > AreaCenter.z + halfZ)
    {
      newPosition.z = Mathf.Clamp(newPosition.z, AreaCenter.z - halfZ, AreaCenter.z + halfZ);
      moveDirection.z *= -1;
    }

    FishPositions[index] = newPosition;

    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
    Quaternion rotation = Quaternion.Slerp(FishRotation[index], targetRotation, RotationSpeed * deltaTime);

    FishRotation[index] = rotation;
  }
}
