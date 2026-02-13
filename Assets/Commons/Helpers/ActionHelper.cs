using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ActionHelper : MonoBehaviour
{
    [SerializeField] private InputActionProperty action;
    [Space(20)]
    public UnityEvent<InputAction.CallbackContext> OnActionPerformed;

    private void OnEnable()
    {
        action.action.performed += EjecuteAction;
    }

    private void OnDisable()
    {
        action.action.performed -= EjecuteAction;
    }

    private void EjecuteAction(InputAction.CallbackContext obj)
    {
        OnActionPerformed?.Invoke(obj);
    }
}
