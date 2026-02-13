using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SteeringWheel : MonoBehaviour
{
    [Header("Wheel Setup")]
    [SerializeField, Range(1, 180)]
    private float _wheelMaxAngle = 90;
    [SerializeField] private GameObject _wheelAnchor;

    [Header("Gear Shift Setup")] 
    [SerializeField] private float _gearShiftAnimSmothTime = 0.25f;
    [SerializeField, Range(1, 90)]
    private float _gearShiftMaxAngle = 45;
    [SerializeField] private GameObject _gearShiftAnchor;
    
    private IEnumerator _gearShiftAnim;
    private float _gearShiftTargetAngle = 0;
    private float _gearShiftCurrentAngle = 0;
    private float _gearShiftCurrentSpeed = 0;

    [Header("Buttons")]
    [SerializeField] private MeshRenderer _btX;
    [SerializeField] private MeshRenderer _btY;
    [SerializeField] private MeshRenderer _btA;
    [SerializeField] private MeshRenderer _btB;
    
    [Header("Dpad")]
    [SerializeField] private MeshRenderer _btLeft;
    [SerializeField] private MeshRenderer _btUp;
    [SerializeField] private MeshRenderer _btDonw;
    [SerializeField] private MeshRenderer _btRight;

    [Header("Triggers")]
    [SerializeField] private MeshRenderer _leftTrigger;
    [SerializeField] private MeshRenderer _rightTrigger;
    
    private SteeringWheelInput _input;

    private const string EMISION_NAME = "_EMISSION";
    
    private void Awake()
    {
        _input = new SteeringWheelInput();
        _input.SteeringWheel.Enable();
        
        _input.SteeringWheel.RotateWheel.performed += OnRotateWheel;

        _input.SteeringWheel.GearShiftUp.performed += OnGearShiftUp;
        _input.SteeringWheel.GearShiftDown.performed += OnGearShiftDown;
        
        _input.SteeringWheel.X.performed += OnX;
        _input.SteeringWheel.Y.performed += OnY;
        _input.SteeringWheel.A.performed += OnA;
        _input.SteeringWheel.B.performed += OnB;
        
        _input.SteeringWheel.Left.performed += OnLeft;
        _input.SteeringWheel.Up.performed += OnUp;
        _input.SteeringWheel.Down.performed += OnDown;
        _input.SteeringWheel.Right.performed += OnRight;
        
        _input.SteeringWheel.WheelLeftTriggerBt.performed += OnLeftTrigger;
        _input.SteeringWheel.WheelRightTriggerBt.performed += OnRightTrigger;
    }

    private void OnRotateWheel(InputAction.CallbackContext obj)
    {
        float axisValue = obj.ReadValue<float>();
        float angle = Mathf.LerpUnclamped(0, _wheelMaxAngle, axisValue);
        _wheelAnchor.transform.localRotation = Quaternion.Euler(0,0,angle);
    }
    
    private void OnGearShiftUp(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            GearShiftAnimTo(-_gearShiftMaxAngle);
        else
            GearShiftAnimTo(0);
    }
    
    private void OnGearShiftDown(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            GearShiftAnimTo(_gearShiftMaxAngle);
        else
            GearShiftAnimTo(0);
    }

    private void GearShiftAnimTo(float target)
    {
        _gearShiftTargetAngle = target;
        if (_gearShiftAnim == null)
        {
            _gearShiftAnim = GearShiftAnim();
            StartCoroutine(_gearShiftAnim);
        }
    }
    
    IEnumerator GearShiftAnim()
    {
        while (_gearShiftTargetAngle != _gearShiftCurrentAngle)
        {
            _gearShiftCurrentAngle = Mathf.SmoothDamp(_gearShiftCurrentAngle, _gearShiftTargetAngle,
                ref _gearShiftCurrentSpeed, _gearShiftAnimSmothTime);
            _gearShiftAnchor.transform.localRotation = Quaternion.Euler(_gearShiftCurrentAngle,0,0);
            yield return new WaitForEndOfFrame();
        }

        _gearShiftAnim = null;
    }
    
    private void OnX(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btX.material.EnableKeyword(EMISION_NAME);
        else
            _btX.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnY(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btY.material.EnableKeyword(EMISION_NAME);
        else
            _btY.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnA(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btA.material.EnableKeyword(EMISION_NAME);
        else
            _btA.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnB(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btB.material.EnableKeyword(EMISION_NAME);
        else
            _btB.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnLeft(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btLeft.material.EnableKeyword(EMISION_NAME);
        else
            _btLeft.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnUp(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btUp.material.EnableKeyword(EMISION_NAME);
        else
            _btUp.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnDown(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btDonw.material.EnableKeyword(EMISION_NAME);
        else
            _btDonw.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnRight(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _btRight.material.EnableKeyword(EMISION_NAME);
        else
            _btRight.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnLeftTrigger(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _leftTrigger.material.EnableKeyword(EMISION_NAME);
        else
            _leftTrigger.material.DisableKeyword(EMISION_NAME);
    }
    
    private void OnRightTrigger(InputAction.CallbackContext obj)
    {
        if (obj.action.IsPressed())
            _rightTrigger.material.EnableKeyword(EMISION_NAME);
        else
            _rightTrigger.material.DisableKeyword(EMISION_NAME);
    }
}
