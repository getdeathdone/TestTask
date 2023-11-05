using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField] 
    private Transform _area;
    [SerializeField]
    private TargetManager _targetManager;
    
    [SerializeField] 
    private Transform _fishPrefab;
    [SerializeField] 
    private int _fishCount;

    private readonly List<Transform> _fishTransforms = new List<Transform>();

    private Vector3 AreaSize => _area.localScale;
    private Vector3 AreaCenter => _area.position;
    
    private void Start()
    {
        _targetManager.GenerateInitialTargets(AreaCenter, AreaSize);

        for (int i = 0; i < _fishCount; i++)
        {
            var fishPos = _targetManager.GenerateRandomPosition(AreaCenter, AreaSize);
            var fish = Instantiate(_fishPrefab, fishPos, _fishPrefab.rotation);
            _fishTransforms.Add(fish);

            if (fish.TryGetComponent(out Fish fishComponent))
            {
                fishComponent.targetManager = _targetManager;
                fishComponent.areaCenter = AreaCenter;
                fishComponent.areaSize = AreaSize;
            }
        }
    }
    
    void Update()
    {
        
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(AreaCenter, AreaSize);
    }
}