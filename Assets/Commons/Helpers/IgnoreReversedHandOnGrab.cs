using Autohand;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Autohand.Grabbable))]
public class IgnoreReversedHandOnGrab : MonoBehaviour
{
    private List<Collider> _collidersToIgnoreHands = new();

    private Grabbable _grabbable;

    private Hand _leftHand, _rightHand;

    private List<Hand> _currentHands = new();

    void Start()
    {
        _collidersToIgnoreHands.AddRange(gameObject.GetComponentsInChildren<Collider>());

        _grabbable = GetComponent<Grabbable>();
        _leftHand = AutoHandPlayer.Instance.handLeft;
        _rightHand = AutoHandPlayer.Instance.handRight;

        _grabbable.OnGrabEvent += OnGrab;
        _grabbable.OnReleaseEvent += OnRelease;
    }

    public void OnGrab(Hand hand, Grabbable grabable)
    {
        if(_currentHands.Count <= 0)
        {
            SetIgnore(true);
        }

        _currentHands.Add(hand);
    }
    public void OnRelease(Hand hand, Grabbable grabable)
    {
        _currentHands.Remove(hand);

        if (_currentHands.Count <= 0)
        {
            SetIgnore(false);
        }        
    }

    private void SetIgnore(bool ignore)
    {
        if(ignore)
        {
            foreach (Collider col in _collidersToIgnoreHands)
            {
                _leftHand.HandIgnoreCollider(col, true);
                _rightHand.HandIgnoreCollider(col, true);
            }
        }
        else
        {
            foreach (Collider col in _collidersToIgnoreHands)
            {
                _leftHand.HandIgnoreCollider(col, true);
                _rightHand.HandIgnoreCollider(col, true);
            }
        }
        
    }

    private void OnDestroy()
    {
        _grabbable.OnGrabEvent -= OnGrab;
        _grabbable.OnReleaseEvent -= OnRelease;
    }
}
