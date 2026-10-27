using UnityEngine;
using UnityEngine.InputSystem;

// Controller for player control of troops (spec §6): reads the new Input System directly, then delegates to selection
// and order planning. Every threshold is in screen pixels, so it does not depend on zoom.
//   LMB click / drag      select (box select from BoxSelectMinDragPixels); Shift toggles
//   RMB click / drag      Pursue (on an enemy), Move (on ground), or a battle line (from OrderDragMinPixels)
//   Alt                   force unlink of chained troops / keep reserves from linking (spec §6.3)
// The first selected troop is shown on the scene's unit card (TroopSelectionCard).
public sealed class TroopPlayerOrders : MonoBehaviour
{
    [SerializeField] int _playerTeamId;

    readonly TroopSelection _selection = new();
    readonly TroopSelectionCard _card = new();
    TroopOrderPlanner _planner;
    Camera _camera;
    Vector2? _selectionStart;
    Vector2? _orderStart;
    Vector2 _mousePosition;
    bool _toggleSelection;

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
        _card.Show(_selection.Selected);

        var mouse = Mouse.current;
        if (mouse == null) return;
        _mousePosition = mouse.position.ReadValue();
        bool overHud = TroopScreenSpace.OverHud(_mousePosition) || _card.Contains(_mousePosition);
        _selection.UpdateHover(overHud ? null : ScreenToWorld(_mousePosition));

        if (mouse.leftButton.wasPressedThisFrame && !overHud)
        {
            _selectionStart = _mousePosition;
            _toggleSelection = IsShiftHeld();
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
            _planner.FinishOrder(_orderStart.Value, _mousePosition, _camera, IsAltHeld());
            _orderStart = null;
        }
    }

    void OnDisable() => _selection.ClearHover();

    void FinishSelection(Vector2 start, Vector2 end)
    {
        TroopTuning tuning = TroopTuning.Active;
        if (OrderClassifier.IsBoxSelect(start, end, tuning.BoxSelectMinDragPixels))
            _selection.SelectBox(ScreenToWorld(start), ScreenToWorld(end), _toggleSelection);
        else
            _selection.SelectAtPoint(ScreenToWorld(end),
                TroopScreenSpace.PixelsToWorld(_camera, end, tuning.ClickPickRadiusPixels), _toggleSelection);
    }

    Vector2 ScreenToWorld(Vector2 screenPosition) => TroopScreenSpace.ScreenToWorld(_camera, screenPosition);

    static bool IsShiftHeld()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    static bool IsAltHeld()
    {
        var keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
    }

    void OnGUI()
    {
        TroopOrderOverlay.DrawHelp(_selection.Selected.Count);

        if (_selectionStart.HasValue && Mouse.current != null)
            TroopOrderOverlay.DrawSelectionBox(_selectionStart.Value, _mousePosition);
        if (_orderStart.HasValue && Mouse.current != null && _camera != null && _selection.Selected.Count > 0)
            DrawOrderPreview(_orderStart.Value, _mousePosition);
    }

    // Ghost preview while the right button is held: each troop's projected footprint and facing, the shared joints of
    // a multi-troop line, and the tether to any chain neighbour outside the order.
    void DrawOrderPreview(Vector2 screenStart, Vector2 screenEnd)
    {
        bool alt = IsAltHeld();
        OrderKind kind = _planner.Preview(screenStart, screenEnd, _camera, alt);
        if (kind == OrderKind.Line)
            TroopOrderOverlay.DrawPlainLine(TroopScreenSpace.ToGui(screenStart), TroopScreenSpace.ToGui(screenEnd));

        var placements = _planner.Placements;
        Vector2 drag = ScreenToWorld(screenEnd) - ScreenToWorld(screenStart);
        for (int i = 0; i < placements.Count; i++)
        {
            var placement = placements[i];
            if (kind == OrderKind.Line)
            {
                float halfWidth = LineGeometry.RankHalfWidth(placement.Length, placement.Troop.LivingSoldierCount)
                    + TroopConstants.SoldierRadius;
                TroopFootprint ghost = TroopFootprint.Rect(
                    TroopOrderPlanner.PlannedLinePoint(placement, TroopConstants.RightEnd),
                    TroopOrderPlanner.PlannedLinePoint(placement, TroopConstants.LeftEnd), placement.Facing, halfWidth);
                TroopOrderOverlay.DrawFootprint(_camera, ghost, placement.Troop.ArmyColor);
                if (i + 1 < placements.Count)
                {
                    TroopOrderPlanner.AdjacentEnds(placement, drag, out int end, out _);
                    TroopOrderOverlay.DrawJoint(TroopScreenSpace.WorldToGui(_camera, TroopOrderPlanner.PlannedLinePoint(placement, end)));
                }
            }
            else
            {
                TroopOrderOverlay.DrawMarker(TroopScreenSpace.WorldToGui(_camera, placement.Center), placement.Troop.ArmyColor);
            }
            TroopOrderOverlay.DrawFacing(_camera, placement.Center, placement.Facing, placement.Troop.NaturalLineLength * 0.25f);
        }

        foreach (var tether in _planner.Tethers)
            TroopOrderOverlay.DrawTether(TroopScreenSpace.WorldToGui(_camera, tether.From),
                TroopScreenSpace.WorldToGui(_camera, tether.To), tether.StaysLinked && !alt);
    }
}
