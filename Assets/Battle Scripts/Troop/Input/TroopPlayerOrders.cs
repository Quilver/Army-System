using UnityEngine;
using UnityEngine.InputSystem;

// Controller for player control of troops (spec §6): reads the new Input System directly, then delegates to selection
// and order planning. Every threshold is in screen pixels, so it does not depend on zoom.
//   LMB click / drag      select (box select from BoxSelectMinDragPixels); Shift toggles
//   RMB click / drag      Pursue (on an enemy), Move (on ground); a drag from OrderDragMinPixels is a battle line for
//                         two or more troops and a Move for one troop (a single troop is never given a facing)
//   Alt                   force unlink of chained troops / keep reserves from linking (spec §6.3)
//   P / gamepad Start     pause and resume (Time.timeScale); no selection or orders while paused
// The first selected troop and the troop under the cursor are shown on troop cards (TroopCardHud), and the soldier under
// the cursor gets a small HP bar over its head (SoldierHealthBar). A hovered troop with ranged weapons shows a
// transparent mask of where it can shoot (TroopRangeMask). A hovered troop with a Move, Line or Attack order shows the
// path it is taking with arrows to its waypoint (TroopOrderPathView).
// Engaged troops show a red heat map across the gap between their lines (TroopEngagementView): most intense in the
// middle of the gap, fading toward each troop's line and the ends of the engagement. F5 toggles it.
public sealed class TroopPlayerOrders : MonoBehaviour
{
    [SerializeField] int _playerTeamId;
    [SerializeField, Tooltip("Shown while the battle is paused (P or gamepad Start).")] GameObject _pausedIndicator;

    [Header("Range mask (hovered ranged troop)")]
    [SerializeField, Tooltip("Fill colour. Its alpha is ignored: see the fill alphas.")]
    Color _rangeMaskFillColor = new(0.15f, 0.7f, 0.6f, 1f);
    [SerializeField, Range(0f, 1f), Tooltip("Fill opacity where the least fire reaches.")] float _rangeMaskMinFillAlpha = 0.15f;
    [SerializeField, Range(0f, 1f), Tooltip("Fill opacity where all of the troop's fire reaches.")] float _rangeMaskMaxFillAlpha = 0.55f;
    [SerializeField, Range(0f, 1f), Tooltip("How much darker the fill gets where all of the troop's fire reaches.")]
    float _rangeMaskHeatDarken = 0.45f;
    [SerializeField] Color _rangeMaskOutline = new(0.05f, 0.4f, 0.85f, 1f);
    [SerializeField, Min(0f), Tooltip("Outline width in screen pixels.")] float _rangeMaskOutlinePixels = 3f;

    [Header("Order path (hovered troop)")]
    [SerializeField, Tooltip("Path and arrows of a Move or Line order.")] Color _orderPathMoveColor = new(0.95f, 0.95f, 0.85f, 0.95f);
    [SerializeField, Tooltip("Path and arrows of an Attack (pursue) order.")] Color _orderPathAttackColor = new(1f, 0.3f, 0.2f, 0.95f);

    [Header("Engagement heat map (F5 toggles)")]
    [SerializeField, Tooltip("Mode at the start of the battle.")] EngagementViewMode _engagementView = EngagementViewMode.HeatMap;
    [SerializeField, Tooltip("Colour where the heat is low.")] Color _engagementHeatLight = new(1f, 0.35f, 0.3f, 1f);
    [SerializeField, Tooltip("Colour where the heat is full.")] Color _engagementHeatDark = new(0.45f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 1f), Tooltip("Opacity where the heat is full (the middle of a fully engaged line gap).")]
    float _engagementHeatMaxAlpha = 0.75f;
    [SerializeField, Min(0f), Tooltip("World units over which the heat fades to nothing at each end of the engagement.")]
    float _engagementHeatEndFade = 2f;
    [SerializeField, Min(0f), Tooltip("Flank and rear attacks (lines not facing each other) draw the band between the engaged " +
        "soldiers: a soldier is engaged when an enemy soldier is within EngagementGap + 2 soldier radii + this (world units).")]
    float _engagementReachSlack = 0.45f;

    readonly TroopSelection _selection = new();
    readonly TroopCardHud _cards = new();
    readonly SoldierHealthBar _healthBar = new();
    readonly TroopRangeMask _rangeMask = new();
    readonly TroopOrderPathView _orderPath = new();
    readonly TroopEngagementView _engagementViewMesh = new();
    TroopOrderPlanner _planner;
    Camera _camera;
    Vector2? _selectionStart;
    Vector2? _orderStart;
    Vector2 _mousePosition;
    bool _toggleSelection;
    bool _paused;

    public bool IsPaused => _paused;

    public EngagementViewMode EngagementView
    {
        get => _engagementView;
        set => _engagementView = value;
    }

    // Heat map <-> Off.
    public static EngagementViewMode NextEngagementView(EngagementViewMode mode) =>
        mode == EngagementViewMode.HeatMap ? EngagementViewMode.Off : EngagementViewMode.HeatMap;

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
        bool overHud = _cards.Contains(_mousePosition);
        _selection.UpdateHover(overHud ? null : ScreenToWorld(_mousePosition),
            TroopScreenSpace.PixelsToWorld(_camera, _mousePosition, TroopTuning.Active.ClickPickRadiusPixels));
        _cards.Update(_selection.Selected, _selection.Hovered, _mousePosition);
        _healthBar.Update(_selection.Troops, overHud ? null : ScreenToWorld(_mousePosition));
        _rangeMask.Update(_selection.Hovered, RangeMaskStyle());

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f5Key.wasPressedThisFrame) _engagementView = NextEngagementView(_engagementView);
        _engagementViewMesh.Update(_selection.Troops, _engagementView, EngagementStyle());

        if (PausePressed(keyboard)) SetPaused(!_paused);
        // No selection or orders while paused.
        if (_paused)
        {
            _selectionStart = null;
            _orderStart = null;
            return;
        }

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

    void OnDisable()
    {
        _selection.ClearHover();
        _rangeMask.Hide();
        _engagementViewMesh.Hide();
    }

    // Pauses or resumes the battle. Does nothing while something else (a cutscene) has stopped time.
    public void SetPaused(bool paused)
    {
        if (paused == _paused) return;
        if (paused && Time.timeScale == 0f) return;
        _paused = paused;
        Time.timeScale = paused ? 0f : 1f;
        if (_pausedIndicator != null) _pausedIndicator.SetActive(paused);
    }

    // Same keys as the Pause action in PlayerInputMap: P, or Start on a gamepad.
    static bool PausePressed(Keyboard keyboard) =>
        (keyboard != null && keyboard.pKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

    void OnDestroy()
    {
        if (_paused) Time.timeScale = 1f;
        _rangeMask.Destroy();
        _engagementViewMesh.Destroy();
    }

    void FinishSelection(Vector2 start, Vector2 end)
    {
        TroopTuning tuning = TroopTuning.Active;
        if (OrderClassifier.IsBoxSelect(start, end, tuning.BoxSelectMinDragPixels))
            _selection.SelectBox(ScreenToWorld(start), ScreenToWorld(end), _toggleSelection);
        else
            _selection.SelectAtPoint(ScreenToWorld(end),
                TroopScreenSpace.PixelsToWorld(_camera, end, tuning.ClickPickRadiusPixels), _toggleSelection);
    }

    TroopRangeMask.Style RangeMaskStyle() => new()
    {
        Fill = _rangeMaskFillColor,
        MinFillAlpha = _rangeMaskMinFillAlpha,
        MaxFillAlpha = _rangeMaskMaxFillAlpha,
        HeatDarken = _rangeMaskHeatDarken,
        Outline = _rangeMaskOutline,
        OutlinePixels = _rangeMaskOutlinePixels,
    };

    TroopEngagementView.Style EngagementStyle() => new()
    {
        HeatLight = _engagementHeatLight,
        HeatDark = _engagementHeatDark,
        MaxHeatAlpha = _engagementHeatMaxAlpha,
        EndFade = _engagementHeatEndFade,
        ReachSlack = _engagementReachSlack,
    };

    TroopOrderPathView.Style OrderPathStyle() => new()
    {
        Move = _orderPathMoveColor,
        Attack = _orderPathAttackColor,
    };

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
        if (_camera != null) _orderPath.Draw(_camera, _selection.Hovered, OrderPathStyle());
        if (_camera != null) _healthBar.Draw(_camera);
        _cards.Draw();

        if (_selectionStart.HasValue && Mouse.current != null)
            TroopOrderOverlay.DrawSelectionBox(_selectionStart.Value, _mousePosition);
        if (_orderStart.HasValue && Mouse.current != null && _camera != null && _selection.Selected.Count > 0)
            DrawOrderPreview(_orderStart.Value, _mousePosition);
    }

    // Ghost preview while the right button is held: each troop's projected footprint (or a marker for a point order), the
    // shared joints of a multi-troop line, and the tether to any chain neighbour outside the order. No facing arrow:
    // troops set their facing themselves (toward the threat, or from the chain line).
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
        }

        foreach (var tether in _planner.Tethers)
            TroopOrderOverlay.DrawTether(TroopScreenSpace.WorldToGui(_camera, tether.From),
                TroopScreenSpace.WorldToGui(_camera, tether.To), tether.StaysLinked && !alt);
    }
}
