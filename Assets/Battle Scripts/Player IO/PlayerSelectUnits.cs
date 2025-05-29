using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerSelectUnits : MonoBehaviour
{
    [SerializeField]
    UnityEvent<IUnit> highlightUnit, SelectedUnitAction;
    public static Action<IUnit> SelectUnit;
    protected static void InvokeSelectUnit(IUnit unit, bool selectOrTarget)
    {
        if (selectOrTarget)
        {
            SelectUnit?.Invoke(unit);
            instance.SelectedUnitAction?.Invoke(unit);
        }
        else
            instance.highlightUnit?.Invoke(unit);
    }
    PlayerInputMap inputs;
    [SerializeField] Army _playerArmy;
    int _unitSelectionIndex;
    public IUnit SelectedUnit
    {
        get
        {
            if(_playerArmy.Units.Count <= _unitSelectionIndex)
                _unitSelectionIndex = _playerArmy.Units.Count - 1;
            return _playerArmy.GetComponentsInChildren<IUnit>()[_unitSelectionIndex];
        }
    }
    IUnit _hoverUnit;
    public IUnit TargetUnit
    {
        get => _hoverUnit;
        set
        {
            if (_hoverUnit == value) return;
            _hoverUnit = value;
        }
    }
    public Vector2 cursorPosition
    {
        get=>transform.position;
    }
    static PlayerSelectUnits instance;
    void Awake()
    {
        inputs = new PlayerInputMap();
        instance = this;
        
    }
    private void Start()=> InvokeSelectUnit(SelectedUnit, true);
    private void OnEnable()
    {
        inputs.Enable();
        inputs.CursorControls.Select.performed += Select;
        inputs.CursorControls.ToggleUnits.performed += ToggleUnits;
    }
    private void OnDisable()
    {
        inputs.Disable();
        inputs.CursorControls.Select.performed -= Select;
        inputs.CursorControls.ToggleUnits.performed -= ToggleUnits;
    }
    [SerializeField] bool SelectUnitOnClick;
    void Select(InputAction.CallbackContext value)
    {
        if (!SelectUnitOnClick) return;
        if (_playerArmy.Units.Contains(TargetUnit))
        {
            _unitSelectionIndex = _playerArmy.Units.IndexOf(TargetUnit);
            InvokeSelectUnit(SelectedUnit, true);
        }
        
    }
    void ToggleUnits(InputAction.CallbackContext value)
    {
        _unitSelectionIndex += (value.ReadValue<float>() > 0) ? 1 : -1;
        if (_unitSelectionIndex < 0) _unitSelectionIndex = _playerArmy.Units.Count - 1;
        _unitSelectionIndex %= _playerArmy.Units.Count;
        InvokeSelectUnit(SelectedUnit, true);
        var pos = SelectedUnit.transform.position;
        Camera.main.transform.position = new(pos.x, pos.y, -5);

    }
    private void Update()
    {
        var coll = Physics2D.OverlapCircle(transform.position, 0.6f, 1 << 6);
        if (coll != null)
        {
            var _target = coll.GetComponentInParent<IUnit>();
            if(_target != TargetUnit) highlightUnit?.Invoke(_target);
            TargetUnit = _target;
        }
        else TargetUnit = null;
    }
}
