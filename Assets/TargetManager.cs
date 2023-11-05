using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    private readonly List<Transform> _targets = new List<Transform>();
    private readonly List<Transform> _availableTargets = new List<Transform>();
    
    private Vector3 _areaSize;
    private Vector3 _areaCenter;
    
    [SerializeField]
    private Transform _targetPrefab;
    public List<Transform> AvailableTargets => _availableTargets;

    public void DeactivateTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        _availableTargets.Remove(target);
        target.gameObject.SetActive(false);

        if (_availableTargets.Count == 0)
        {
            Debug.Log("DeactivateAllTarget");
            MoveAllTargets();
        }
    }

    public void GenerateInitialTargets(Vector3 areaCenter, Vector3 areaSize)
    {
        _areaSize = areaSize;
        _areaCenter = areaCenter;
        int numberOfTargets = Mathf.FloorToInt(areaSize.x * areaSize.z);

        for (int i = 0; i < numberOfTargets; i++)
        {
            Vector3 randomPosition = GenerateRandomPosition(areaCenter, areaSize);
            Transform newTarget = Instantiate(_targetPrefab, randomPosition, Quaternion.identity);
            _targets.Add(newTarget);
            _availableTargets.Add(newTarget);
        }
    }

    public Vector3 GenerateRandomPosition(Vector3 areaCenter, Vector3 areaSize)
    {
        float x = Random.Range(areaCenter.x - areaSize.x / 2, areaCenter.x + areaSize.x / 2);
        float y = Random.Range(areaCenter.y - areaSize.y / 2, areaCenter.y + areaSize.y / 2);
        float z = Random.Range(areaCenter.z - areaSize.z / 2, areaCenter.z + areaSize.z / 2);
        return new Vector3(x, y, z);
    }

    private void MoveAllTargets()
    {
        foreach (Transform target in _targets)
        {
            target.gameObject.SetActive(true);
            _availableTargets.Add(target);
            
            target.position = _areaSize;
        }
    }
    
    [ContextMenu("RandomizeAllTargets")]
    private void RandomizeAllTargets()
    {
        foreach (Transform target in _targets)
        {
            target.position = GenerateRandomPosition(_areaCenter, _areaSize);
        }
    }
}
