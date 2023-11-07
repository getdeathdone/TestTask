using System.Collections.Generic;
using UnityEngine;

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

  private Vector3[] _targetPositions;
  private float [] _targetTime;
  private bool [] _targetActive;

  private readonly List<int> _deactivateTarget = new List<int>();

  private readonly List<Transform> _fishTransforms = new List<Transform>();
  private readonly List<Transform> _targetTransforms = new List<Transform>();

  private Vector3 AreaSize => _area.localScale - _areaOffset;
  private Vector3 AreaCenter => _area.position + _areaCenterOffset;

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
    for (int index = 0; index < _fishTransforms.Count; index++)
    {
      Transform variable = _fishTransforms[index];

      var dsdsd = UpdateFish(_fishCount, index, 
        _fishPositions, _fishRotation, 
        _fishMovingToInterestPoint, _fishReachToInterestPoints, 
        _fishTargetPositions, _fishTargetIndex,
        _targetActive, _targetPositions, _targetTime);

      if (!dsdsd)
      {
        continue;
      }

      variable.transform.position = _fishPositions[index];
      variable.transform.rotation = _fishRotation[index];
    }
  }

  private bool UpdateFish (
    int fishCount, int index, 
    List<Vector3> fishPositions, List<Quaternion> fishRotation, bool [] fishMovingToInterestPoint, bool [] fishReachToInterestPoints,
    Vector3 [] fishTargetPositions, int [] fishTargetIndex, bool [] targetActive, Vector3[] targetPositions, float [] targetTime)
  {
    if (fishReachToInterestPoints[index])
    {
      bool multiEating = false;
      Vector3 calculateMiddlePoint = default;

      for (int i = 0; i < fishCount; i++)
      {
        if (i == index)
        {
          continue;
        }

        if (!fishReachToInterestPoints[i] && fishTargetPositions[i] != fishTargetPositions[index])
        {
          continue;
        }

        multiEating = true;

        float middleX = (fishPositions[index].x + fishPositions[i].x) / 2;
        float middleY = (fishPositions[index].y + fishPositions[i].y) / 2;
        float middleZ = (fishPositions[index].z + fishPositions[i].z) / 2;
        calculateMiddlePoint = new Vector3(middleX, middleY, middleZ);

        break;
      }

      bool eatingComplete = !targetActive[fishTargetIndex[index]] || Eating(_fishData.TimeAtInterestPoint, fishTargetIndex[index]);
      if (!eatingComplete)
      {
        return false;
      }

      fishReachToInterestPoints[index] = false;
      fishMovingToInterestPoint[index] = false;
      bool deactivateComplete = DeactivateTarget(fishTargetIndex[index]);

      if (deactivateComplete && multiEating)
      {
        Debug.Log("Reproduce");
        //Reproduce?.Invoke(calculateMiddlePoint);
      }
    }

    fishTargetPositions[index] = FindClosestPoint(targetPositions, targetActive, fishPositions[index], out int indexTarget);
    fishTargetIndex[index] = indexTarget;

    Vector3 avoidanceMove = Vector3.zero;
    Vector3 alignmentMove = Vector3.zero;
    Vector3 cohesionMove = Vector3.zero;

    for (int i = 0; i < fishCount; i++)
    {
      if (i == index)
      {
        continue;
      }

      if (fishMovingToInterestPoint[i])
      {
        continue;
      }

      float distance = Vector3.Distance(fishPositions[index], fishPositions[i]);

      if (distance < _fishData.AvoidanceRadius)
      {
        Vector3 avoidVector = fishPositions[index] - fishPositions[i];
        avoidanceMove += avoidVector.normalized;
      }

      if (distance < _fishData.AlignmentDistance)
      {
        alignmentMove += fishRotation[i] * Vector3.forward;
      }

      if (distance < _fishData.CohesionRadius)
      {
        cohesionMove += fishPositions[i];
      }
    }

    if (cohesionMove != Vector3.zero)
    {
      cohesionMove /= fishCount;
      cohesionMove -= fishPositions[index];
    }

    Vector3 targetDirection = (fishTargetPositions[index] - fishPositions[index]).normalized;

    float distanceToTarget = Vector3.Distance(fishPositions[index], fishTargetPositions[index]);
    fishReachToInterestPoints[index] = distanceToTarget <= _fishData.StoppingReachDistance;
    fishMovingToInterestPoint[index] = distanceToTarget <= _fishData.StoppingMovingDistance;

    Vector3 moveDirection = fishMovingToInterestPoint[index] ? targetDirection
      : targetDirection + avoidanceMove + alignmentMove + cohesionMove.normalized * _fishData.CohesionWeight;

    Vector3 newPosition = fishPositions[index] + moveDirection.normalized * _fishData.Speed * Time.deltaTime;

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

    fishPositions[index] = newPosition;

    Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
    Quaternion rotation = Quaternion.Slerp(fishRotation[index], targetRotation, _fishData.RotationSpeed * Time.deltaTime);

    fishRotation[index] = rotation;
    return true;
    
    bool Eating (float timeAtInterestPoint, int indexTargetTime)
    {
      targetTime[indexTargetTime] += Time.deltaTime;
      return targetTime[indexTargetTime] >= timeAtInterestPoint;
    }
    
    Vector3 FindClosestPoint (Vector3[] points, bool[] targetActiveArray, Vector3 position, out int pointIndex)
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

        float distance = Vector3.Distance(position, points[i]);

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

  private void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(AreaCenter, AreaSize);

    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(_area.position, _area.localScale);
  }

  private void GenerateInitialTargets (Vector3 areaCenter, Vector3 areaSize)
  {
    int numberOfTargets = Mathf.FloorToInt(areaSize.x * areaSize.z);

    _targetPositions = new Vector3[numberOfTargets];
    _targetActive = new bool[numberOfTargets];
    _targetTime = new float[numberOfTargets];

    for (int i = 0; i < numberOfTargets; i++)
    {
      Vector3 randomPosition = GenerateRandomPosition(areaCenter, areaSize);
      _targetPositions[i] = randomPosition;

      var newTarget = Instantiate(_targetPrefab, randomPosition, Quaternion.identity);
      _targetTransforms.Add(newTarget);
      
      _targetActive[i] = true;
    }
  }

  private bool DeactivateTarget (int value)
  {
    if (_targetTransforms.Count == _deactivateTarget.Count || !_targetActive[value])
    {
      return false;
    }

    _targetTime[value] = 0;
    _targetActive[value] = false;
    _targetTransforms[value].gameObject.SetActive(false);
    
    _deactivateTarget.Add(value);

    if (_targetTransforms.Count == _deactivateTarget.Count)
    {
      Debug.Log("DeactivateAllTarget");
      MoveAllTargets();
    }

    return true;
  }

  private void MoveAllTargets()
  {
    for (int index = 0; index < _targetTransforms.Count; index++)
    {
      Transform target = _targetTransforms[index];

      _targetPositions[index] = AreaSize;
      target.transform.position = AreaSize;
      
      _targetActive[index] = true;
      _targetTransforms[index].gameObject.SetActive(true);
    }
  }

  [ContextMenu("ActivateAllTargets")]
  private void ActivateAllTargets()
  {
    _deactivateTarget.Clear();
    
    for (int index = 0; index < _targetTransforms.Count; index++)
    {
      Transform target = _targetTransforms[index];
      Vector3 randomPosition = GenerateRandomPosition(AreaCenter, AreaSize);

      target.transform.position = randomPosition;
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