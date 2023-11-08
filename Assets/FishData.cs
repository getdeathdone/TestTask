using System;
using UnityEngine;

[Serializable]
public class FishData
{
  [SerializeField]
  private float _speed = 3.0f;
  [SerializeField]
  private float _avoidanceRadius = 2.0f;
  [SerializeField]
  private float _alignmentDistance = 5.0f;
  [SerializeField]
  private float _cohesionWeight = 1.0f;
  [SerializeField]
  private float _cohesionRadius = 5.0f;
  [SerializeField]
  private float _rotationSpeed = 2.0f;
  [SerializeField]
  private float _stoppingMovingDistance = 1.0f;
  [SerializeField]
  private float _stoppingReachDistance = 0.5f;
  [SerializeField]
  private float _timeAtInterestPoint = 5f;

  public float Speed => _speed;
  public float AvoidanceRadius => _avoidanceRadius;
  public float AlignmentDistance => _alignmentDistance;
  public float CohesionWeight => _cohesionWeight;
  public float CohesionRadius => _cohesionRadius;
  public float RotationSpeed => _rotationSpeed;
  public float StoppingMovingDistance => _stoppingMovingDistance;
  public float StoppingReachDistance => _stoppingReachDistance;
  public float TimeAtInterestPoint => _timeAtInterestPoint;
}