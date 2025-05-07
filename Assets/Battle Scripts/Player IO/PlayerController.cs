using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
namespace PlayerControls
{

}
public abstract class PlayerController : MonoBehaviour
{
    #region Event hooks
    public UnityEvent<Vector2> movedPositionOfCursor;
    public UnityEvent<IUnit> highlightUnit, SelectedUnit;
    #endregion
    Player inputs;
    // Use this for initialization
    void Awake()
    {
        Cursor.visible = false;
        inputs = new Player();
    }
    private void OnEnable()
    {
        inputs.Enable();
        inputs.CursorControls.MoveCursor.performed += MoveCursor;
        inputs.CursorControls.MoveCursor.canceled += MoveCursor;
        inputs.CursorControls.SetCursor.performed += SetCursor;
        inputs.CursorControls.Select.performed += Select;
        inputs.CursorControls.Order.performed += Order;
        inputs.CursorControls.Pause.performed += Pause;
        inputs.CursorControls.ToggleUnits.performed += ToggleUnits;


    }
    private void OnDisable()
    {
        inputs.Disable();
        inputs.CursorControls.MoveCursor.performed -= MoveCursor;
        inputs.CursorControls.MoveCursor.canceled -= MoveCursor;
        inputs.CursorControls.SetCursor.performed -= SetCursor;
        inputs.CursorControls.Select.performed -= Select;
        inputs.CursorControls.Order.performed -= Order;
        inputs.CursorControls.Pause.performed -= Pause;
        inputs.CursorControls.ToggleUnits.performed -= ToggleUnits;
    }
    protected abstract void MoveCursor(InputAction.CallbackContext value);
    protected abstract void SetCursor(InputAction.CallbackContext value);
    protected abstract void Select(InputAction.CallbackContext value);
    protected abstract void Order(InputAction.CallbackContext value);
    protected abstract void Pause(InputAction.CallbackContext value);
    protected abstract void ToggleUnits(InputAction.CallbackContext value);
    
}
