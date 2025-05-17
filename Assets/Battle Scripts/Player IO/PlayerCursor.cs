using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCursor : MonoBehaviour
{
    PlayerInputMap inputs;
    void Awake()
    {
        Cursor.visible = false;
        inputs = new PlayerInputMap();
        _cursor=transform;
    }
    private void OnEnable()
    {
        Cursor.visible = false;
        inputs.Enable();
        inputs.CursorControls.MoveCursor.performed += MoveCursor;
        inputs.CursorControls.MoveCursor.canceled += MoveCursor;
        inputs.CursorControls.SetCursor.performed += SetCursor;
    }
    private void OnDisable()
    {
        Cursor.visible = true;
        inputs.Disable();
        inputs.CursorControls.MoveCursor.performed -= MoveCursor;
        inputs.CursorControls.MoveCursor.canceled -= MoveCursor;
        inputs.CursorControls.SetCursor.performed -= SetCursor;
    }
    Transform _cursor;
    Vector2 cursorDirection = Vector2.zero;
    [SerializeField, Range(15, 35)]
    float cursorSpeed;
    Vector2 CursorPositionOffset;
    protected void MoveCursor(InputAction.CallbackContext value)
    {
        cursorDirection = value.ReadValue<Vector2>().normalized * cursorSpeed;
    }
    protected void SetCursor(InputAction.CallbackContext value)
    {

        CursorPositionOffset = Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.transform.position;
    }
    void Update()
    {
        CursorPositionOffset += cursorDirection * Time.unscaledDeltaTime;
        _cursor.position = (Vector2)Camera.main.transform.position + CursorPositionOffset;
    }
}
