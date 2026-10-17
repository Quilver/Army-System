using UnityEngine;
using UnityEngine.InputSystem;

// Controller for player control of troops: reads the mouse, then delegates to selection and order planning.
public sealed class TroopPlayerOrders : MonoBehaviour
{
    [SerializeField] int _playerTeamId;
    [SerializeField, Min(1f)] float _selectionDragThreshold = 10f;
    [SerializeField, Min(0.1f)] float _clickSelectionRadius = 0.6f;
    [SerializeField, Min(0.1f)] float _clickOrderThreshold = 0.5f;

    readonly TroopSelection _selection = new();
    readonly System.Collections.Generic.List<Vector2> _previewDots = new();
    TroopOrderPlanner _planner;
    Camera _camera;
    Vector2? _selectionStart;
    Vector2? _orderStart;
    Vector2 _mousePosition;
    bool _addToSelection;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _camera = Camera.main;
        _planner = new TroopOrderPlanner(_selection);
    }

    void Update()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;
        _selection.PlayerTeamId = _playerTeamId;
        _selection.Refresh();
        _selection.RemoveDestroyed();

        var mouse = Mouse.current;
        if (mouse == null) return;
        _mousePosition = mouse.position.ReadValue();
        bool overHud = TroopScreenSpace.OverHud(_mousePosition);
        _selection.UpdateHover(overHud ? null : ScreenToWorld(_mousePosition));

        if (mouse.leftButton.wasPressedThisFrame && !overHud)
        {
            _selectionStart = _mousePosition;
            _addToSelection = IsShiftHeld();
        }
        if (mouse.leftButton.wasReleasedThisFrame && _selectionStart.HasValue)
        {
            FinishSelection(_selectionStart.Value, _mousePosition);
            _selectionStart = null;
        }

        if (mouse.rightButton.wasPressedThisFrame && !overHud)
            _orderStart = _mousePosition;
        if (mouse.rightButton.wasReleasedThisFrame && _orderStart.HasValue)
        {
            _planner.FinishOrder(
                ScreenToWorld(_orderStart.Value), ScreenToWorld(_mousePosition), _clickOrderThreshold, _clickSelectionRadius);
            _orderStart = null;
        }
    }

    void OnDisable() => _selection.ClearHover();

    void FinishSelection(Vector2 start, Vector2 end)
    {
        if ((end - start).sqrMagnitude >= _selectionDragThreshold * _selectionDragThreshold)
            _selection.SelectBox(ScreenToWorld(start), ScreenToWorld(end), _addToSelection);
        else
            _selection.SelectAtPoint(ScreenToWorld(end), _clickSelectionRadius, _addToSelection);
    }

    Vector2 ScreenToWorld(Vector2 screenPosition) => TroopScreenSpace.ScreenToWorld(_camera, screenPosition);

    static bool IsShiftHeld()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    void OnGUI()
    {
        TroopOrderOverlay.DrawHelp(_selection.Selected.Count);

        if (_selectionStart.HasValue && Mouse.current != null)
            TroopOrderOverlay.DrawSelectionBox(_selectionStart.Value, _mousePosition);
        if (_orderStart.HasValue && Mouse.current != null)
            DrawOrderPreview(_orderStart.Value, _mousePosition);
    }

    void DrawOrderPreview(Vector2 screenStart, Vector2 screenEnd)
    {
        Vector2 worldStart = ScreenToWorld(screenStart);
        Vector2 worldEnd = ScreenToWorld(screenEnd);
        Vector2 guiStart = TroopScreenSpace.ToGui(screenStart);
        int count = _selection.Selected.Count;
        if (count == 0 || (worldEnd - worldStart).magnitude < _clickOrderThreshold)
        {
            TroopOrderOverlay.DrawPlainLine(guiStart, TroopScreenSpace.ToGui(screenEnd));
            return;
        }

        _planner.ComputeLineLayout(worldStart, worldEnd, out Vector2 lineEnd, out _, out bool clamped);
        _previewDots.Clear();
        for (int i = 0; i < count; i++)
            _previewDots.Add(TroopScreenSpace.WorldToGui(_camera, TroopOrderPlanner.LinePoint(worldStart, lineEnd, i, count)));
        TroopOrderOverlay.DrawLayout(guiStart, TroopScreenSpace.WorldToGui(_camera, lineEnd), clamped, _previewDots);
    }
}