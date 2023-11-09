using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;
public class GameManager : MonoBehaviour
{

  private readonly List<Transform> _fishTransforms = new List<Transform>();
  private readonly List<Transform> _targetTransforms = new List<Transform>();
  public event Action<int> OnUpdateFish;
  public event Action<int> OnUpdateFeed;

  [SerializeField]
  private Vector3 _areaOffset = Vector3.one;
  [SerializeField]
  private Vector3 _areaCenterOffset = Vector3.zero;
  [SerializeField]
  private Vector3 _startTargetPoint;

  [SerializeField]
  private Transform _area;
  [SerializeField]
  private Transform _targetPrefab;
  [SerializeField]
  private Transform _fishPrefab;
  [SerializeField]
  private int _fishCount = 10;
  [SerializeField]
  private FishData _fishData;

  private int _targetCount;
  private int _deactivateTarget;

  private NativeArray<Vector3> _position;
  private NativeArray<Quaternion> _rotation;

  private NativeArray<bool> _reachToInterestPoints;
  private NativeArray<bool> _movingToInterestPoint;
  private NativeArray<int> _fishTargetIndex;
  private NativeArray<Vector3> _fishTargetPositions;
  private NativeArray<Vector3> _calculateMiddlePoint;
  private NativeArray<int> _disableTarget;

  private NativeArray<Vector3> _targetPositions;
  private NativeArray<float> _targetTime;
  private NativeArray<bool> _targetActive; // init value true

  private void Awake()
  {
    _position = new NativeArray<Vector3>(_fishCount, Allocator.Persistent);
    _rotation = new NativeArray<Quaternion>(_fishCount, Allocator.Persistent);
    _movingToInterestPoint = new NativeArray<bool>(_fishCount, Allocator.Persistent);
    _reachToInterestPoints = new NativeArray<bool>(_fishCount, Allocator.Persistent);
    _fishTargetPositions = new NativeArray<Vector3>(_fishCount, Allocator.Persistent);
    _fishTargetIndex = new NativeArray<int>(_fishCount, Allocator.Persistent);
    _calculateMiddlePoint = new NativeArray<Vector3>(_fishCount, Allocator.Persistent);
    _disableTarget = new NativeArray<int>(_fishCount, Allocator.Persistent);
  }

  private void Start()
  {
    AddTargets(Mathf.FloorToInt(AreaSize.x * AreaSize.z));

    for (int i = 1; i <= _fishCount; i++)
    {
      Reproduce(default, i);
    }
  }

  private void Update()
  {
    for (int i = 0; i < _fishCount; i++)
    {
      _disableTarget[i] = -1;
    }

    EatJob eatJob = new EatJob
    {
      FishPositions = _position,
      FishTargetPositionsArray = _fishTargetPositions,
      FishTargetIndexArray = _fishTargetIndex,
      MovingToInterestPoint = _movingToInterestPoint,
      ReachToInterestPoints = _reachToInterestPoints,
      TargetActive = _targetActive,
      TargetTime = _targetTime,
      TimeAtInterestPoint = _fishData.TimeAtInterestPoint,
      CalculateMiddlePoint = _calculateMiddlePoint,
      DisableTarget = _disableTarget,
      deltaTime = Time.deltaTime
    };

    JobHandle jobHandle = eatJob.Schedule(_fishCount, 0);
    jobHandle.Complete();

    for (int i = 0; i < _fishCount; i++)
    {
      bool deactivateComplete = false;

      if (_disableTarget[i] != -1)
      {
        deactivateComplete = DeactivateTarget(_disableTarget[i]);
      }

      if (deactivateComplete && _calculateMiddlePoint[i] != default)
      {
        Debug.Log("Reproduce");
        Reproduce(_calculateMiddlePoint[i], _fishCount + 1);

        _calculateMiddlePoint[i] = default;
      }
    }

    MoveJob moveJob = new MoveJob
    {
      FishPositions = _position,
      FishRotation = _rotation,
      FishMovingToInterestPoint = _movingToInterestPoint,
      FishReachToInterestPoints = _reachToInterestPoints,
      FishTargetPositions = _fishTargetPositions,
      FishTargetIndexArray = _fishTargetIndex,
      TargetPositions = _targetPositions,
      TargetActive = _targetActive,
      AvoidanceRadius = _fishData.AvoidanceRadius,
      AlignmentDistance = _fishData.AlignmentDistance,
      CohesionRadius = _fishData.CohesionRadius,
      StoppingReachDistance = _fishData.StoppingReachDistance,
      StoppingMovingDistance = _fishData.StoppingMovingDistance,
      CohesionWeight = _fishData.CohesionWeight,
      Speed = _fishData.Speed,
      RotationSpeed = _fishData.RotationSpeed,
      AreaCenter = AreaCenter,
      AreaSize = AreaSize,
      deltaTime = Time.deltaTime
    };

    jobHandle = moveJob.Schedule(_fishCount, 0);
    jobHandle.Complete();

    for (int i = 0; i < _fishCount; i++)
    {
      if (_reachToInterestPoints[i])
      {
        continue;
      }

      _fishTransforms[i].transform.position = _position[i];
      _fishTransforms[i].transform.rotation = _rotation[i];
    }
  }

  private void OnDestroy()
  {
    _position.Dispose();
    _rotation.Dispose();
    _movingToInterestPoint.Dispose();
    _reachToInterestPoints.Dispose();
    _fishTargetPositions.Dispose();
    _fishTargetIndex.Dispose();
    _targetPositions.Dispose();
    _targetActive.Dispose();
    _targetTime.Dispose();
    _calculateMiddlePoint.Dispose();
    _disableTarget.Dispose();
  }

  public void AddTwentyTargets()
  {
    AddTargets(20);
  }

  public void ResetAllTargets()
  {
    for (int i = 0; i < _targetCount; i++)
    {
      Vector3 randomPosition = GenerateRandomPosition();
      _targetTransforms[i].transform.position = randomPosition;
      _targetTransforms[i].gameObject.SetActive(true);
      ResetTarget(i);
    }

    _deactivateTarget = 0;
  }

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(AreaCenter, AreaSize);

    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(_area.position, _area.localScale);
  }

  private void Reproduce (Vector3 fishPos, int fishNumber)
  {
    if (fishNumber > _fishCount)
    {
      NativeArray<Vector3> newFishPosition = new NativeArray<Vector3>(fishNumber, Allocator.Persistent);
      NativeArray<Quaternion> newRotation = new NativeArray<Quaternion>(fishNumber, Allocator.Persistent);
      NativeArray<bool> newMovingToInterestPoint = new NativeArray<bool>(fishNumber, Allocator.Persistent);
      NativeArray<bool> newReachToInterestPoints = new NativeArray<bool>(fishNumber, Allocator.Persistent);
      NativeArray<Vector3> newFishTargetPositions = new NativeArray<Vector3>(fishNumber, Allocator.Persistent);
      NativeArray<int> newFishTargetIndex = new NativeArray<int>(fishNumber, Allocator.Persistent);

      NativeArray<int> newFishDeactivateTarget = new NativeArray<int>(fishNumber, Allocator.Persistent);
      NativeArray<Vector3> newCalculateMiddlePoint = new NativeArray<Vector3>(fishNumber, Allocator.Persistent);

      for (int i = 0; i < _fishCount; i++)
      {
        newFishPosition[i] = _position[i];
        newRotation[i] = _rotation[i];
        newMovingToInterestPoint[i] = _movingToInterestPoint[i];
        newReachToInterestPoints[i] = _reachToInterestPoints[i];
        newFishTargetPositions[i] = _fishTargetPositions[i];
        newFishTargetIndex[i] = _fishTargetIndex[i];
        newFishDeactivateTarget[i] = _disableTarget[i];
        newCalculateMiddlePoint[i] = _calculateMiddlePoint[i];
      }

      newFishDeactivateTarget[_fishCount] = -1;

      _position.Dispose();
      _rotation.Dispose();
      _movingToInterestPoint.Dispose();
      _reachToInterestPoints.Dispose();
      _fishTargetPositions.Dispose();
      _fishTargetIndex.Dispose();
      _calculateMiddlePoint.Dispose();
      _disableTarget.Dispose();

      _position = newFishPosition;
      _rotation = newRotation;
      _movingToInterestPoint = newMovingToInterestPoint;
      _reachToInterestPoints = newReachToInterestPoints;
      _fishTargetPositions = newFishTargetPositions;
      _fishTargetIndex = newFishTargetIndex;
      _calculateMiddlePoint = newCalculateMiddlePoint;
      _disableTarget = newFishDeactivateTarget;

      _fishCount = fishNumber;
    }

    Vector3 position = fishPos == default ? GenerateRandomPosition() : fishPos;
    Quaternion fishRot = _fishPrefab.rotation;

    Transform fish = Instantiate(_fishPrefab, position, fishRot);
    fish.SetParent(_area);
    _fishTransforms.Add(fish);

    _position[fishNumber - 1] = position;
    _rotation[fishNumber - 1] = fishRot;

    OnUpdateFish?.Invoke(_fishCount);
  }

  private void AddTargets (int addTargetCount)
  {
    int newTargetCount = addTargetCount + _targetCount;

    NativeArray<Vector3> newTargetPositions = new NativeArray<Vector3>(newTargetCount, Allocator.Persistent);
    NativeArray<float> newTargetTime = new NativeArray<float>(newTargetCount, Allocator.Persistent);
    NativeArray<bool> newTargetActive = new NativeArray<bool>(newTargetCount, Allocator.Persistent);

    for (int i = 0; i < _targetCount; i++)
    {
      newTargetPositions[i] = _targetPositions[i];
      newTargetTime[i] = _targetTime[i];
      newTargetActive[i] = _targetActive[i];
    }

    _targetPositions.Dispose();
    _targetTime.Dispose();
    _targetActive.Dispose();

    _targetPositions = newTargetPositions;
    _targetTime = newTargetTime;
    _targetActive = newTargetActive;

    for (int i = _targetCount; i < newTargetCount; i++)
    {
      Vector3 position = GenerateRandomPosition();
      Transform newTarget = Instantiate(_targetPrefab, position, _targetPrefab.rotation);
      newTarget.SetParent(_area);
      _targetTransforms.Add(newTarget);
      ResetTarget(i);
    }

    _targetCount = newTargetCount;
    OnUpdateFeed?.Invoke(_targetCount);
  }

  private bool DeactivateTarget (int index)
  {
    if (_targetCount == _deactivateTarget || !_targetActive[index])
    {
      return false;
    }

    _targetActive[index] = false;
    _targetTransforms[index].gameObject.SetActive(false);

    _deactivateTarget++;

    if (_targetCount == _deactivateTarget)
    {
      Debug.Log("DeactivateAllTarget");

      for (int i = 0; i < _targetCount; i++)
      {
        _targetTransforms[i].transform.position = StartTargetPointRelativeToCenter;
        ResetTarget(i);
      }
    }

    return true;
  }

  private void ResetTarget (int i)
  {
    _targetTime[i] = 0;
    _targetActive[i] = true;
    _targetPositions[i] = _targetTransforms[i].position;
  }

  private Vector3 GenerateRandomPosition()
  {
    Vector3 areaCenter = AreaCenter;
    Vector3 areaSize = AreaSize;

    float x = Random.Range(areaCenter.x - areaSize.x / 2, areaCenter.x + areaSize.x / 2);
    float y = Random.Range(areaCenter.y - areaSize.y / 2, areaCenter.y + areaSize.y / 2);
    float z = Random.Range(areaCenter.z - areaSize.z / 2, areaCenter.z + areaSize.z / 2);
    return new Vector3(x, y, z);
  }

  private Vector3 AreaSize => _area.localScale - _areaOffset;
  private Vector3 AreaCenter => _area.position + _areaCenterOffset;
  private Vector3 StartTargetPointRelativeToCenter => AreaCenter + _startTargetPoint;

  public FishData FishData => _fishData;
}