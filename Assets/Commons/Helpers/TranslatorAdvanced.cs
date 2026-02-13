using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TranslatorAdvanced : MonoBehaviour
{
    [Header("Time")]
    [SerializeField] private float _animationDuration = 1f;
    [SerializeField] private float _currentTime = 0f;
    
    private IEnumerator _currentAnimation;
    [SerializeField] private bool _nextAnimationIsToTarget = true;
    
    [Header("Position")]
    [SerializeField] private AnimationCurve _xPositionCurve = AnimationCurve.Linear(0,0,1,0);
    [SerializeField] private AnimationCurve _yPositionCurve = AnimationCurve.Linear(0,0,1,0);
    [SerializeField] private AnimationCurve _zPositionCurve = AnimationCurve.Linear(0,0,1,0);
    
    [Header("Rotation")]
    [SerializeField] private AnimationCurve _xRotationCurve = AnimationCurve.Linear(0,0,1,0);
    [SerializeField] private AnimationCurve _yRotationCurve = AnimationCurve.Linear(0,0,1,0);
    [SerializeField] private AnimationCurve _zRotationCurve = AnimationCurve.Linear(0,0,1,0);


    [Header("Animation Triggers")]
    public UnityEvent OnOriginReach;
    public UnityEvent OnTargetReach;
    public UnityEvent<float> OnChange;

    public void ToOrigin()
    {
        _nextAnimationIsToTarget = true;
        ChangeAnimation(ToOriginAnimation());
    }

    public void ToTarget()
    {
        _nextAnimationIsToTarget = false;
        ChangeAnimation(ToTargetAnimation());
    }

    public void ToReverse()
    {
        if (_nextAnimationIsToTarget)
        {
            ToTarget();
        }
        else
        {
            ToOrigin();
        }
    }

    private void ChangeAnimation(IEnumerator newAnimation)
    {
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
        }

        _currentAnimation = newAnimation;
        StartCoroutine(_currentAnimation);
    }

    private IEnumerator ToOriginAnimation()
    {
        while (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            _currentTime = Math.Clamp(_currentTime, 0, _animationDuration);

            SetPositionForCurrentTime();

            yield return null;
        }

        _currentAnimation = null;
        OnOriginReach.Invoke();
    }

    private IEnumerator ToTargetAnimation()
    {
        while (_currentTime < _animationDuration)
        {
            _currentTime += Time.deltaTime;
            _currentTime = Math.Clamp(_currentTime, 0, _animationDuration);

            SetPositionForCurrentTime();

            yield return null;
        }

        _currentAnimation = null;
        OnTargetReach.Invoke();
    }

    private void SetPositionForCurrentTime()
    {
        float interpolatedValue = _currentTime / _animationDuration;

        Vector3 position = new Vector3(
                _xPositionCurve.Evaluate(interpolatedValue),
                _yPositionCurve.Evaluate(interpolatedValue),
                _zPositionCurve.Evaluate(interpolatedValue)
        );
        
        Vector3 rotation = new Vector3(
            _xRotationCurve.Evaluate(interpolatedValue),
            _yRotationCurve.Evaluate(interpolatedValue),
            _zRotationCurve.Evaluate(interpolatedValue)
        );

        transform.localPosition = position;
        transform.localRotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);

        OnChange.Invoke(interpolatedValue);
    }
}
