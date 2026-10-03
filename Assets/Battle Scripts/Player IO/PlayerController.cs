using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField]
    UnityEvent<IUnit> highlightUnit, SelectedUnitAction;
    //public static Action<IUnit> SelectUnit;
    protected static void InvokeSelectUnit(IUnit unit, bool selectOrTarget)
    {
        if (selectOrTarget)
        {
            //SelectUnit?.Invoke(unit);
            instance.SelectedUnitAction?.Invoke(unit);
        }
        else
            instance.highlightUnit?.Invoke(unit);
    }
    #endregion
    PlayerInputMap inputs;
    static PlayerController instance;
    // Use this for initialization
    void Awake()
    {
        instance = this;
        Cursor.visible = false;
        inputs = new PlayerInputMap();
        
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
