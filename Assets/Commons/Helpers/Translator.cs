using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Translator : MonoBehaviour
{
    [Header("Time")]
    [SerializeField] private float _animationDuration = 1f;
    [SerializeField] private float _currentTime = 0f;
    
    private IEnumerator _currentAnimation;
    [SerializeField] private bool _nextAnimationIsToTarget = true;
    
    [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0,0,1,1);
    
    [Header("Translations")]
    [SerializeField] private Vector3 _displacement = Vector3.zero;
    private Vector3 _originPosition;
    [SerializeField] private Vector3 _rotation = Vector3.zero;
    private Quaternion _originRotation;

    

    [Header("Animation Triggers")]
    public UnityEvent OnOriginReach;
    public UnityEvent OnTargetReach;
    public UnityEvent<float> OnChange;

    private void Awake()
    {
        _originPosition = transform.localPosition;
        _originRotation = transform.localRotation;
    }

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

        interpolatedValue = _curve.Evaluate(interpolatedValue);

        transform.localPosition = _originPosition + (_displacement * interpolatedValue);
        Vector3 newRotation = _rotation * interpolatedValue;
        transform.localRotation = _originRotation * Quaternion.Euler(newRotation.x, newRotation.y, newRotation.z);

        OnChange.Invoke(interpolatedValue);
    }
}
