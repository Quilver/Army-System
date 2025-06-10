using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(LineRenderer))]
public class PlayerOrderUnits : MonoBehaviour
{
    PlayerInputMap inputs;
    public Vector2 cursorPosition
    {
        get => transform.position;
    }
    PlayerSelectUnits _unitSelection;
    PlayerSelectUnits UnitSelected
    {
        get
        {
            if (_unitSelection == null)_unitSelection=GetComponent<PlayerSelectUnits>();
            return _unitSelection;
        }
    }
    void Awake()
    {
        inputs = new PlayerInputMap();
        _line = GetComponent<LineRenderer>();
    }
    private void OnEnable()
    {
        inputs.Enable();
        inputs.CursorControls.Select.performed += StartSelectOrder;
        inputs.CursorControls.Select.canceled += ExecuteSelectOrder;
        inputs.CursorControls.Order.performed += StartMoveOrder;
        inputs.CursorControls.Order.canceled += ExecuteMoveOrder;
        inputs.CursorControls.Pause.performed += Pause;


    }
    private void OnDisable()
    {
        inputs.Disable();
        inputs.CursorControls.Select.performed -= StartSelectOrder;
        inputs.CursorControls.Select.canceled -= ExecuteSelectOrder;
        inputs.CursorControls.Order.performed -= StartMoveOrder;
        inputs.CursorControls.Order.canceled -= ExecuteMoveOrder;
        inputs.CursorControls.Pause.performed -= Pause;
    }
    Vector2? _OrderStart;
    [SerializeField, Range(1, 5)] float _minDistanceForDirection = 2;
    void StartMoveOrder(InputAction.CallbackContext value)
    {
        if (_OrderStart.HasValue) {
            _OrderStart = null;
            return;
        }
        if (UnitSelected.TargetUnit != null && UnitSelected.SelectedUnit != UnitSelected.TargetUnit)
        {
            UnitSelected.SelectedUnit.GetComponent<MovementSystem.IMoveOrders>().MoveTo(UnitSelected.TargetUnit.transform);
            return;
        }
        _OrderStart = cursorPosition;
    }
    void ExecuteMoveOrder(InputAction.CallbackContext value)
    {
        if (!_OrderStart.HasValue) return;
        if (Vector2.Distance(_OrderStart.Value, cursorPosition) < _minDistanceForDirection)
            UnitSelected.SelectedUnit.GetComponent<MovementSystem.IMoveOrders>().MoveTo(cursorPosition);
        else
            UnitSelected.SelectedUnit.GetComponent<MovementSystem.IMoveOrders>().MoveTo(_OrderStart.Value, cursorPosition);
        _OrderStart = null;
    }
    void StartSelectOrder(InputAction.CallbackContext value)
    {
        if (_OrderStart.HasValue)
        {
            _OrderStart = null;
            return;
        }
        if (UnitSelected.TargetUnit != null)
        {
            //return;
        }
        _OrderStart = cursorPosition;
    }
    void ExecuteSelectOrder(InputAction.CallbackContext value)
    {
        if (!_OrderStart.HasValue) return;
        if (UnitSelected.TargetUnit !=null)
            UnitSelected.SelectedUnit.GetComponent<OrderTarget>().Order(UnitSelected.TargetUnit.transform);
        else
            UnitSelected.SelectedUnit.GetComponent<OrderTarget>().Order(cursorPosition);
        _OrderStart = null;
    }
    void Pause(InputAction.CallbackContext value) => Time.timeScale = (Time.timeScale != 1) ? 0 : 1;
    LineRenderer _line;
    private void Update()
    {
        if (!CanDraw())
        {
            _line.enabled = false;
            return;
        }
        if (!_OrderStart.HasValue) return;
        if (_minDistanceForDirection > Vector2.Distance(cursorPosition, _OrderStart.Value))
            return;
        _line.enabled = true;
        float MaxLineLength = 10;
        var dir = _OrderStart.Value - cursorPosition;
        dir = -Vector2.ClampMagnitude(dir, MaxLineLength);
        _line.SetPosition(0, _OrderStart.Value);
        _line.SetPosition(1, _OrderStart.Value + dir);

    }
    bool CanDraw()
    {
        if (!_OrderStart.HasValue) return false;
        else if (_minDistanceForDirection > Vector2.Distance(cursorPosition, _OrderStart.Value))
            return false;
        else return true;
    }
}
