using System;
using System.Reflection;
using DefaultNamespace;
using UnityEngine;

public enum FishParameter
{
  Speed,
  AvoidanceRadius,
  AlignmentDistance,
  CohesionWeight,
  CohesionRadius,
  RotationSpeed,
  StoppingMovingDistance,
  StoppingReachDistance,
  TimeAtInterestPoint
}

[Serializable]
public class FishData
{
    [SerializeField, Range(FishDataConstants.MIN_SPEED, FishDataConstants.MAX_SPEED)]
    private float _speed = 3.0f;

    [SerializeField, Range(FishDataConstants.MIN_AVOIDANCE_RADIUS, FishDataConstants.MAX_AVOIDANCE_RADIUS)]
    private float _avoidanceRadius = 2.0f;

    [SerializeField, Range(FishDataConstants.MIN_ALIGNMENT_DISTANCE, FishDataConstants.MAX_ALIGNMENT_DISTANCE)]
    private float _alignmentDistance = 5.0f;

    [SerializeField, Range(FishDataConstants.MIN_COHESION_WEIGHT, FishDataConstants.MAX_COHESION_WEIGHT)]
    private float _cohesionWeight = 1.0f;

    [SerializeField, Range(FishDataConstants.MIN_COHESION_RADIUS, FishDataConstants.MAX_COHESION_RADIUS)]
    private float _cohesionRadius = 5.0f;

    [SerializeField, Range(FishDataConstants.MIN_ROTATION_SPEED, FishDataConstants.MAX_ROTATION_SPEED)]
    private float _rotationSpeed = 2.0f;

    [SerializeField, Range(FishDataConstants.MIN_STOPPING_MOVING_DISTANCE, FishDataConstants.MAX_STOPPING_MOVING_DISTANCE)]
    private float _stoppingMovingDistance = 1.0f;

    [SerializeField, Range(FishDataConstants.MIN_STOPPING_REACH_DISTANCE, FishDataConstants.MAX_STOPPING_REACH_DISTANCE)]
    private float _stoppingReachDistance = 0.5f;

    [SerializeField, Range(FishDataConstants.MIN_TIME_AT_INTEREST_POINT, FishDataConstants.MAX_TIME_AT_INTEREST_POINT)]
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
  
  public float GetParameter(FishParameter parameter)
  {
      return parameter switch
      {
          FishParameter.Speed => _speed,
          FishParameter.AvoidanceRadius => _avoidanceRadius,
          FishParameter.AlignmentDistance => _alignmentDistance,
          FishParameter.CohesionWeight => _cohesionWeight,
          FishParameter.CohesionRadius => _cohesionRadius,
          FishParameter.RotationSpeed => _rotationSpeed,
          FishParameter.StoppingMovingDistance => _stoppingMovingDistance,
          FishParameter.StoppingReachDistance => _stoppingReachDistance,
          FishParameter.TimeAtInterestPoint => _timeAtInterestPoint,
          _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter, null)
      };
  }

  public void SetParameter (FishParameter parameter, float value)
  {
      switch (parameter)
      {
          case FishParameter.Speed:
              _speed = value;
              break;
          case FishParameter.AvoidanceRadius:
              _avoidanceRadius = value;
              break;
          case FishParameter.AlignmentDistance:
              _alignmentDistance = value;
              break;
          case FishParameter.CohesionWeight:
              _cohesionWeight = value;
              break;
          case FishParameter.CohesionRadius:
              _cohesionRadius = value;
              break;
          case FishParameter.RotationSpeed:
              _rotationSpeed = value;
              break;
          case FishParameter.StoppingMovingDistance:
              _stoppingMovingDistance = value;
              break;
          case FishParameter.StoppingReachDistance:
              _stoppingReachDistance = value;
              break;
          case FishParameter.TimeAtInterestPoint:
              _timeAtInterestPoint = value;
              break;
          default:
              throw new ArgumentOutOfRangeException(nameof(parameter), parameter, null);
      }
  }
}