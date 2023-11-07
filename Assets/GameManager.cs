using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
  [SerializeField]
  private Transform _area;
  [SerializeField]
  private Vector3 _areaOffset = Vector3.one;
  [SerializeField]
  private Vector3 _areaCenterOffset = Vector3.zero;
  [SerializeField]
  private TargetManager _targetManager;

  [SerializeField]
  private FishData _fishData;

  [SerializeField]
  private Transform _fishPrefab;
  [SerializeField]
  private int _fishCount;

  private List<Vector3> _fishPositions;
  private List<Quaternion> _fishRotation;
  private bool [] _fishReachToInterestPoints;
  private bool [] _fishMovingToInterestPoint;
  private Vector3 [] _fishTargetPositions;
  private int [] _fishTargetIndex;

  private readonly List<Transform> _fishTransforms = new List<Transform>();

  private Vector3 AreaSize => _area.localScale - _areaOffset;
  private Vector3 AreaCenter => _area.position + _areaCenterOffset;

  private void Start()
  {
    _targetManager.GenerateInitialTargets(AreaCenter, AreaSize);

    /*_availableTargets = new List<bool>(_targets.Count);
    _availableTargets.AddRange(Enumerable.Repeat(true, _targets.Count));*/

    _fishPositions = new List<Vector3>(_fishCount);
    _fishRotation = new List<Quaternion>(_fishCount);

    _fishReachToInterestPoints = new bool[_fishCount];
    _fishMovingToInterestPoint = new bool[_fishCount];
    _fishTargetPositions = new Vector3[_fishCount];
    _fishTargetIndex = new int[_fishCount];

    for (int i = 0; i < _fishCount; i++)
    {
      var fishPos = _targetManager.GenerateRandomPosition(AreaCenter, AreaSize);
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
      var dsdsd = UpdateFish(_fishCount, index, _fishPositions, _fishRotation, _fishMovingToInterestPoint, _fishReachToInterestPoints, _fishTargetPositions, _fishTargetIndex);

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
    List<Vector3> fishPositions, List<Quaternion> fishRotation, 
    bool [] fishMovingToInterestPoint, bool [] fishReachToInterestPoints,
    Vector3 [] fishTargetPositions, int [] fishTargetIndex)
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
      
      bool eatingComplete = _targetManager.AvailableTargets[fishTargetIndex[index]] == null || _targetManager.AvailableTargets[fishTargetIndex[index]].Eating(_fishData.TimeAtInterestPoint);

      if (!eatingComplete)
      {
        return false;
      }

      fishReachToInterestPoints[index] = false;
      fishMovingToInterestPoint[index] = false;
      bool deactivateComplete = _targetManager.DeactivateTarget(_targetManager.AvailableTargets[fishTargetIndex[index]], fishTargetIndex[index]);

      if (deactivateComplete && multiEating)
      {
        Debug.Log("Reproduce");
        //Reproduce?.Invoke(calculateMiddlePoint);
      }
    }

    fishTargetPositions[index] = FindClosestPoint(_targetManager.AvailableTargets, fishPositions[index], out int indexTarget);
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
  }

  private Vector3 FindClosestPoint (List<Target> points, Vector3 position, out int index)
  {
    if (points == null || points.Count == 0)
    {
      index = -1;
      return Vector3.zero;
    }
    
    index = 0;
    Target closestPoint = null;
    float closestDistance = 0;

    for (int i = index; i < points.Count; i++)
    {
      if (points[i] == null)
      {
        continue;
      }

      index = i;
      closestPoint = points[i];
      closestDistance = Vector3.Distance(position, closestPoint.transform.position);
      break;
    }

    for (int i = index + 1; i < points.Count; i++)
    {
      if (points[i] == null)
      {
        continue;
      }
      
      float distance = Vector3.Distance(position, points[i].transform.position);

      if (distance < closestDistance)
      {
        index = i;
        closestPoint = points[i];
        closestDistance = distance;
      }
    }

    return closestPoint.transform.position;
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
}