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
    GenerateInitialTargets(AreaCenter, AreaSize);

    _fishPositions = new List<Vector3>(_fishCount);
    _fishRotation = new List<Quaternion>(_fishCount);

    _fishReachToInterestPoints = new bool[_fishCount];
    _fishMovingToInterestPoint = new bool[_fishCount];
    _fishTargetPositions = new Vector3[_fishCount];
    _fishTargetIndex = new int[_fishCount];

    for (int i = 0; i < _fishCount; i++)
    {
      var fishPos = GenerateRandomPosition(AreaCenter, AreaSize);
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
      changeTransform[index] = UpdateFish(_fishCount, index, _fishPositions, _fishMovingToInterestPoint, _fishReachToInterestPoints, _fishTargetPositions, _fishTargetIndex,
        _targetActive, _targetPositions, _targetTime);

      position[index] = _fishPositions[index];
      rotation[index] = _fishRotation[index];
      movingToInterestPoint[index] = _fishMovingToInterestPoint[index];
      reachToInterestPoints[index] = _fishReachToInterestPoints[index];
      fishTargetPositions[index] = _fishTargetPositions[index];
      fishTargetIndex[index] = _fishTargetIndex[index];
    }

    JobHandle jobHandle = default;

    for (int index = 0; index < _fishCount; index++)
    {
      MoveJob moveJob = new MoveJob
      {
        index = index,
        FishCount = _fishCount,
        FishPositions = position,
        FishRotation = rotation,
        FishMovingToInterestPoint = movingToInterestPoint,
        FishReachToInterestPoints = reachToInterestPoints,
        FishTargetPositions = fishTargetPositions,
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

      jobHandle = moveJob.Schedule(jobHandle);
    }

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
  }

  private bool UpdateFish (
    int fishCount, int index, List<Vector3> fishPositions, bool [] fishMovingToInterestPoint, bool [] fishReachToInterestPoints, Vector3 [] fishTargetPositionsArray,
    int [] fishTargetIndexArray, bool [] targetActive, Vector3 [] targetPositions, float [] targetTime)
  {
    if (fishReachToInterestPoints[index])
    {
      Vector3 calculateMiddlePoint = default;

      for (int i = 0; i < fishCount; i++)
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

    fishTargetPositionsArray[index] = FindClosestPoint(targetPositions, targetActive, fishPositions[index], out int indexTarget);
    fishTargetIndexArray[index] = indexTarget;

    return true;

    bool Eating (float timeAtInterestPoint, int indexTargetTime)
    {
      targetTime[indexTargetTime] += Time.deltaTime;
      return targetTime[indexTargetTime] >= timeAtInterestPoint;
    }

    Vector3 FindClosestPoint (Vector3 [] points, bool [] targetActiveArray, Vector3 vector3, out int pointIndex)
    {
      pointIndex = -1;
      float closestDistance = float.MaxValue;
      Vector3 closestPoint = Vector3.zero;

      for (int i = 0; i < points.Length; i++)
      {
        if (!targetActiveArray[i])
        {
          continue;
        }

        float distance = Vector3.Distance(vector3, points[i]);

        if (distance < closestDistance)
        {
          pointIndex = i;
          closestPoint = points[i];
          closestDistance = distance;
        }
      }

      return closestPoint;
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

  private void GenerateInitialTargets (Vector3 areaCenter, Vector3 areaSize)
  {
    _targetCount = Mathf.FloorToInt(areaSize.x * areaSize.z);

    _targetPositions = new Vector3[_targetCount];
    _targetActive = new bool[_targetCount];
    _targetTime = new float[_targetCount];

    for (int i = 0; i < _targetCount; i++)
    {
      Vector3 randomPosition = GenerateRandomPosition(areaCenter, areaSize);
      _targetPositions[i] = randomPosition;

      var newTarget = Instantiate(_targetPrefab, randomPosition, Quaternion.identity);
      _targetTransforms.Add(newTarget);

      _targetActive[i] = true;
    }
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
      MoveAllTargets();
    }

    return true;

    void MoveAllTargets()
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

  [ContextMenu("ActivateAllTargets")]
  private void ActivateAllTargets()
  {
    _deactivateTarget = 0;

    for (int index = 0; index < _targetCount; index++)
    {
      Vector3 randomPosition = GenerateRandomPosition(AreaCenter, AreaSize);

      _targetTransforms[index].transform.position = randomPosition;
      _targetPositions[index] = randomPosition;
    }
  }

  private Vector3 GenerateRandomPosition (Vector3 areaCenter, Vector3 areaSize)
  {
    float x = Random.Range(areaCenter.x - areaSize.x / 2, areaCenter.x + areaSize.x / 2);
    float y = Random.Range(areaCenter.y - areaSize.y / 2, areaCenter.y + areaSize.y / 2);
    float z = Random.Range(areaCenter.z - areaSize.z / 2, areaCenter.z + areaSize.z / 2);
    return new Vector3(x, y, z);
  }
}