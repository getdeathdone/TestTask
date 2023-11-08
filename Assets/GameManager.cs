using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
  [SerializeField]
  private Vector3 _areaOffset = Vector3.one;
  [SerializeField]
  private Vector3 _areaCenterOffset = Vector3.zero;

  [SerializeField]
  private Transform _area;
  [SerializeField]
  private Transform _targetPrefab;
  [SerializeField]
  private Transform _fishPrefab;
  [SerializeField]
  private int _fishCount;
  [SerializeField]
  private FishData _fishData;

  private bool _changeTargetTransform;

  private List<Vector3> _fishPositions;
  private List<Quaternion> _fishRotation;
  private bool [] _fishReachToInterestPoints;
  private bool [] _fishMovingToInterestPoint;
  private Vector3 [] _fishTargetPositions;
  private int [] _fishTargetIndex;

  private Vector3 [] _targetPositions;
  private float [] _targetTime;
  private bool [] _targetActive;
  private int _targetCount;
  private int _deactivateTarget;

  private NativeArray<Vector3> position;
  private NativeArray<Quaternion> rotation;
  private NativeArray<bool> movingToInterestPoint;
  private NativeArray<bool> reachToInterestPoints;
  private NativeArray<Vector3> fishTargetPositions;
  private NativeArray<int> fishTargetIndex;
  private NativeArray<Vector3> targetPositions;
  private NativeArray<bool> targetActive;

  private readonly List<Transform> _fishTransforms = new List<Transform>();
  private readonly List<Transform> _targetTransforms = new List<Transform>();

  private Vector3 AreaSize => _area.localScale - _areaOffset;
  private Vector3 AreaCenter => _area.position + _areaCenterOffset;

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(AreaCenter, AreaSize);

    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(_area.position, _area.localScale);
  }

  private void Awake()
  {
    position = new NativeArray<Vector3>(_fishCount, Allocator.Persistent);
    rotation = new NativeArray<Quaternion>(_fishCount, Allocator.Persistent);
    movingToInterestPoint = new NativeArray<bool>(_fishCount, Allocator.Persistent);
    reachToInterestPoints = new NativeArray<bool>(_fishCount, Allocator.Persistent);
    fishTargetPositions = new NativeArray<Vector3>(_fishCount, Allocator.Persistent);
    fishTargetIndex = new NativeArray<int>(_fishCount, Allocator.Persistent);
  }

  private void Start()
  {
    _targetCount = Mathf.FloorToInt(AreaSize.x * AreaSize.z);
    GenerateInitialTargets();

    targetPositions = new NativeArray<Vector3>(_targetCount, Allocator.Persistent);
    targetActive = new NativeArray<bool>(_targetCount, Allocator.Persistent);

    _fishPositions = new List<Vector3>(_fishCount);
    _fishRotation = new List<Quaternion>(_fishCount);

    _fishReachToInterestPoints = new bool[_fishCount];
    _fishMovingToInterestPoint = new bool[_fishCount];
    _fishTargetPositions = new Vector3[_fishCount];
    _fishTargetIndex = new int[_fishCount];

    for (int i = 0; i < _fishCount; i++)
    {
      var fishPos = GenerateRandomPosition();
      var fishRot = _fishPrefab.rotation;
      _fishPositions.Add(fishPos);
      _fishRotation.Add(fishRot);
      Reproduce(fishPos, fishRot);
    }
  }

  private void Update()
  {
    bool [] changeTransform = new bool[_fishCount];

    for (int index = 0; index < _fishCount; index++)
    {
      changeTransform[index] = UpdateFish(index, _fishPositions, _fishMovingToInterestPoint, _fishReachToInterestPoints, _fishTargetPositions, _fishTargetIndex, _targetActive,
        _targetTime);

      position[index] = _fishPositions[index];
      rotation[index] = _fishRotation[index];
      movingToInterestPoint[index] = _fishMovingToInterestPoint[index];
      reachToInterestPoints[index] = _fishReachToInterestPoints[index];
    }

    if (_changeTargetTransform)
    {
      for (int i = 0; i < _targetCount; i++)
      {
        targetPositions[i] = _targetPositions[i];
        targetActive[i] = _targetActive[i];
      }
    }

    MoveJob moveJob = new MoveJob
    {
      FishPositions = position,
      FishRotation = rotation,
      FishMovingToInterestPoint = movingToInterestPoint,
      FishReachToInterestPoints = reachToInterestPoints,
      FishTargetPositions = fishTargetPositions,
      FishTargetIndexArray = fishTargetIndex,
      TargetPositions = targetPositions,
      TargetActive = targetActive,
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

    JobHandle jobHandle = moveJob.Schedule(_fishCount, 0);
    jobHandle.Complete();

    for (int i = 0; i < _fishCount; i++)
    {
      _fishPositions[i] = position[i];
      _fishRotation[i] = rotation[i];
      _fishMovingToInterestPoint[i] = movingToInterestPoint[i];
      _fishReachToInterestPoints[i] = reachToInterestPoints[i];
      _fishTargetPositions[i] = fishTargetPositions[i];
      _fishTargetIndex[i] = fishTargetIndex[i];

      if (!changeTransform[i])
      {
        continue;
      }

      _fishTransforms[i].transform.position = _fishPositions[i];
      _fishTransforms[i].transform.rotation = _fishRotation[i];
    }
  }

  private void OnDestroy()
  {
    position.Dispose();
    rotation.Dispose();
    movingToInterestPoint.Dispose();
    reachToInterestPoints.Dispose();
    fishTargetPositions.Dispose();
    fishTargetIndex.Dispose();
    targetPositions.Dispose();
    targetActive.Dispose();
  }

  private bool UpdateFish (
    int index, List<Vector3> fishPositions, bool [] fishMovingToInterestPoint, bool [] fishReachToInterestPoints, Vector3 [] fishTargetPositionsArray, int [] fishTargetIndexArray,
    bool [] targetActive, float [] targetTime)
  {
    if (fishReachToInterestPoints[index])
    {
      Vector3 calculateMiddlePoint = default;

      for (int i = 0; i < fishPositions.Count; i++)
      {
        if (i == index)
        {
          continue;
        }

        if (!fishReachToInterestPoints[i] && fishTargetPositionsArray[i] != fishTargetPositionsArray[index])
        {
          continue;
        }

        float middleX = (fishPositions[index].x + fishPositions[i].x) / 2;
        float middleY = (fishPositions[index].y + fishPositions[i].y) / 2;
        float middleZ = (fishPositions[index].z + fishPositions[i].z) / 2;
        calculateMiddlePoint = new Vector3(middleX, middleY, middleZ);

        break;
      }

      bool eatingComplete = !targetActive[fishTargetIndexArray[index]] || Eating(_fishData.TimeAtInterestPoint, fishTargetIndexArray[index]);

      if (!eatingComplete)
      {
        return false;
      }

      fishReachToInterestPoints[index] = false;
      fishMovingToInterestPoint[index] = false;
      bool deactivateComplete = DeactivateTarget(fishTargetIndexArray[index]);

      if (calculateMiddlePoint != default && deactivateComplete)
      {
        Debug.Log("Reproduce");
        //Reproduce?.Invoke(calculateMiddlePoint);
      }
    }

    return true;

    bool Eating (float timeAtInterestPoint, int indexTargetTime)
    {
      targetTime[indexTargetTime] += Time.deltaTime;
      return targetTime[indexTargetTime] >= timeAtInterestPoint;
    }
  }

  private void Reproduce (Vector3 fishPos, Quaternion fishRot)
  {
    if (fishRot == default)
    {
      fishRot = _fishPrefab.rotation;
    }

    var fish = Instantiate(_fishPrefab, fishPos, fishRot);
    _fishTransforms.Add(fish);
  }

  private void GenerateInitialTargets()
  {
    _targetPositions = new Vector3[_targetCount];
    _targetActive = new bool[_targetCount];
    _targetTime = new float[_targetCount];

    for (int i = 0; i < _targetCount; i++)
    {
      var newTarget = Instantiate(_targetPrefab);
      _targetTransforms.Add(newTarget);

      _targetActive[i] = true;
    }

    ChangeAllTargetTransform();
  }

  private bool DeactivateTarget (int index)
  {
    if (_targetCount == _deactivateTarget || !_targetActive[index])
    {
      return false;
    }

    _targetTime[index] = 0;
    _targetActive[index] = false;
    _targetTransforms[index].gameObject.SetActive(false);

    _deactivateTarget++;

    if (_targetCount == _deactivateTarget)
    {
      Debug.Log("DeactivateAllTarget");
      ResetAllTargets();
    }

    return true;

    void ResetAllTargets()
    {
      for (int index = 0; index < _targetCount; index++)
      {
        _targetPositions[index] = AreaSize;
        _targetTransforms[index].transform.position = AreaSize;

        _targetActive[index] = true;
        _targetTransforms[index].gameObject.SetActive(true);
      }
    }
  }

  [ContextMenu("ChangeAllTargetTransform")]
  private void ChangeAllTargetTransform()
  {
    _deactivateTarget = 0;
    _changeTargetTransform = true;

    for (int index = 0; index < _targetCount; index++)
    {
      Vector3 randomPosition = GenerateRandomPosition();

      _targetPositions[index] = randomPosition;
      _targetTransforms[index].transform.position = randomPosition;
    }
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
}