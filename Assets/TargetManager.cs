using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    private readonly List<Target> _targets = new List<Target>();
    private readonly List<Target> _availableTargets = new List<Target>();
    
    private Vector3 _areaSize;
    private Vector3 _areaCenter;
    
    [SerializeField]
    private Target _targetPrefab;
    public List<Target> AvailableTargets => _availableTargets;

    public bool DeactivateTarget(Target target)
    {
        if (target == null || !target.IsActive)
        {
            return false;
        }

        target.Activate(false);
        _availableTargets.Remove(target);

        if (_availableTargets.Count == 0)
        {
            Debug.Log("DeactivateAllTarget");
            MoveAllTargets();
        }

        return true;
    }

    public void GenerateInitialTargets(Vector3 areaCenter, Vector3 areaSize)
    {
        _areaSize = areaSize;
        _areaCenter = areaCenter;
        int numberOfTargets = Mathf.FloorToInt(areaSize.x * areaSize.z);

        for (int i = 0; i < numberOfTargets; i++)
        {
            Vector3 randomPosition = GenerateRandomPosition(areaCenter, areaSize);
            Target newTarget = Instantiate(_targetPrefab, randomPosition, Quaternion.identity);
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
        foreach (Target target in _targets)
        {
            target.transform.position = _areaSize;
            _availableTargets.Add(target);
        }
    }
    
    [ContextMenu("ActivateAllTargets")]
    private void ActivateAllTargets()
    {
        foreach (Target target in _targets)
        {
            target.Activate(true);
            target.transform.position = GenerateRandomPosition(_areaCenter, _areaSize);
        }
    }
}
