using UnityEngine;
public class Target : MonoBehaviour
{
  [SerializeField]
  private Light _eatParticles;
  private float _timeElapsedAtInterestPoint;
  private bool _isActive = true;

  public bool IsActive => _isActive;

  public bool Eating(float timeAtInterestPoint)
  {
    /*if(!_eatParticles.enabled)
    {
      _eatParticles.enabled = true;
    }*/

    _timeElapsedAtInterestPoint += Time.deltaTime;
    return _timeElapsedAtInterestPoint >= timeAtInterestPoint;
  }

  public void Activate(bool value)
  {
    if (!value)
    {
      _timeElapsedAtInterestPoint = 0;
    }

    _isActive = value;
    _eatParticles.enabled = value;
    gameObject.SetActive(value);
  }
}