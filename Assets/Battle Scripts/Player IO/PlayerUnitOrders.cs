using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
namespace PlayerControls
{
    public class PlayerUnitOrders : PlayerController
    {
        [SerializeField]
        Army playerArmy;
        Vector2 cursorDirection = Vector2.zero;
        [SerializeField, Range(15, 35)]
        float cursorSpeed;
        Vector2 CursorPositionOffset;
        IUnit SelectedUnitV
        {
            get
            {

                return playerArmy.GetComponentsInChildren<IUnit>()[_unitSelectionIndex];
            }
        }
        [SerializeField]
        IUnit unit;
        private void Start()
        {
            unit = SelectedUnitV;
            _cursor = transform;
            Debug.Log(SelectedUnitV);
            SelectedUnit?.Invoke(SelectedUnitV);
        }
        protected override void MoveCursor(InputAction.CallbackContext value)
        {
            cursorDirection = value.ReadValue<Vector2>().normalized * cursorSpeed;
        }
        protected override void SetCursor(InputAction.CallbackContext value)
        {

            CursorPositionOffset = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;
        }
        [SerializeField]
        int _unitSelectionIndex;
        protected override void ToggleUnits(InputAction.CallbackContext value)
        {
            _unitSelectionIndex = (_unitSelectionIndex + 1) % playerArmy.GetComponentsInChildren<IUnit>().Length;
            SelectedUnit?.Invoke(SelectedUnitV);
            var pos = SelectedUnitV.transform.position;
            Camera.main.transform.position = new(pos.x, pos.y, -5);
        }
        protected override void Order(InputAction.CallbackContext value)
        {
            if (SelectedUnitV == null) return;
            if (HoverUnit == null)
                SelectedUnitV.transform.GetComponent<SteeringSystem.IMoveOrders>().MoveTo(_cursor.position);
            else
                SelectedUnitV.GetComponent<SteeringSystem.IMoveOrders>().MoveTo(HoverUnit.transform);
        }
        public UnityEvent PauseGame, UnpauseGame;
        protected override void Pause(InputAction.CallbackContext value)
        {
            if (Time.timeScale == 1) {
                Time.timeScale = 0;
                PauseGame?.Invoke();
            }
            else
            {
                Time.timeScale = 1;
                UnpauseGame?.Invoke();
            }
        }

        protected override void Select(InputAction.CallbackContext value)
        {

        }
        Transform _cursor;
        private void Update()
        {
            UpdateCursorPosition();
            UpdateHover();
        }
        void UpdateCursorPosition()
        {
            CursorPositionOffset += cursorDirection * Time.unscaledDeltaTime;
            transform.position = (Vector2)Camera.main.transform.position + CursorPositionOffset;
        }
        IUnit _hoverUnit;
        IUnit HoverUnit
        {
            get=>_hoverUnit;
            set
            {
                if (_hoverUnit == value) return;
                _hoverUnit = value;
                highlightUnit?.Invoke(_hoverUnit);
            }
        }
        void UpdateHover()
        {
            var coll = Physics2D.OverlapCircle(transform.position, 0.6f, 1 << 6);
            if (coll != null)
                HoverUnit = coll.GetComponentInParent<IUnit>();
            else HoverUnit = null;
            if (HoverUnit == null) _cursor.GetComponent<SpriteRenderer>().color = Color.white;
            else if (HoverUnit.GetComponentInParent<Army>().controller == Army.Controller.Player)
                _cursor.GetComponent<SpriteRenderer>().color = Color.blue;
            else _cursor.GetComponent<SpriteRenderer>().color = Color.red;
        }

    }
}