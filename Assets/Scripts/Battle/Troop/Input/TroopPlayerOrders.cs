using UnityEngine;
using UnityEngine.InputSystem;

// Controller for player control of troops (spec §6): reads the new Input System directly, then delegates to selection
// and order planning. Every threshold is in screen pixels, so it does not depend on zoom.
//   LMB click / drag      select (box select from BoxSelectMinDragPixels, starting off the player's troops); Shift toggles.
//                         A click on a troop selects just it; a double click selects its whole chain (SU-129).
//   LMB drag from a troop press on one of the player's troops, keep the button down and drag: on release the selection
//                         (that troop, or its chain after a double click) moves to the release point (SU-129)
//   RMB click / drag      Pursue (on an enemy), Move (on ground); a drag from OrderDragMinPixels is a battle line for
//                         two or more troops and a Move for one troop (a single troop is never given a facing)
//   Alt                   force unlink of chained troops / keep reserves from linking (spec §6.3)
//   P / gamepad Start     pause and resume (GamePause), keeping the game speed
// Speed bar (BattleSpeedHud, top-right): Pause, ½ speed, Play and 2× speed (GamePause.Speed). It is hidden while
// deploying and once the battle is over. While paused, the camera, selection and orders all still work; troops carry
// out their orders when time runs again.
// During the deployment phase (Placement set) a Move or Line puts the troops straight there instead of marching them,
// inside the deployment zone; attack orders are ignored. Clicks on UI (the Start Battle button) are not orders.
// The first selected troop and the troop under the cursor are shown on troop cards (TroopCardHud), and the soldier under
// the cursor gets a small HP bar over its head (SoldierHealthBar). A hovered troop with ranged weapons shows a
// transparent mask of where it can shoot (TroopRangeMask). Every selected troop, and the troop under the cursor, with a
// Move, Line or Attack order shows the path it is taking with arrows to its waypoint (TroopOrderPathView).
// Engaged troops show a red heat map across the gap between their lines (TroopEngagementView): most intense in the
// middle of the gap, fading toward each troop's line and the ends of the engagement. F5 toggles it.
public sealed class TroopPlayerOrders : MonoBehaviour
{
    [SerializeField] int _playerTeamId;
    [SerializeField, Tooltip("Shown while the battle is paused (P, gamepad Start or the speed bar).")] GameObject _pausedIndicator;

    [Header("Range mask (hovered ranged troop)")]
    [SerializeField, Tooltip("Fill colour. Its alpha is ignored: see the fill alphas.")]
    Color _rangeMaskFillColor = new(0.15f, 0.7f, 0.6f, 1f);
    [SerializeField, Range(0f, 1f), Tooltip("Fill opacity where the least fire reaches.")] float _rangeMaskMinFillAlpha = 0.15f;
    [SerializeField, Range(0f, 1f), Tooltip("Fill opacity where all of the troop's fire reaches.")] float _rangeMaskMaxFillAlpha = 0.55f;
    [SerializeField, Range(0f, 1f), Tooltip("How much darker the fill gets where all of the troop's fire reaches.")]
    float _rangeMaskHeatDarken = 0.45f;
    [SerializeField] Color _rangeMaskOutline = new(0.05f, 0.4f, 0.85f, 1f);
    [SerializeField, Min(0f), Tooltip("Outline width in screen pixels.")] float _rangeMaskOutlinePixels = 3f;

    [Header("Order path (selected troops and the hovered troop)")]
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
        "soldiers: a soldier is engaged when an enemy soldier is within A_Distance + 2 soldier radii + this (world units). " +
        "Facing lines show heat only where they are within A_Distance of each other.")]
    float _engagementReachSlack = 0.45f;

    readonly TroopSelection _selection = new();
    readonly TroopCardHud _cards = new();
    readonly BattleSpeedHud _speedHud = new();
    readonly CombatLogHud _combatLog = new();
    readonly SoldierHealthBar _healthBar = new();
    readonly TroopRangeMask _rangeMask = new();
    readonly TroopOrderPathView _orderPath = new();
    readonly TroopEngagementView _engagementViewMesh = new();
    TroopOrderPlanner _planner;
    Camera _camera;
    Vector2? _selectionStart;
    // The player's troop under the left press, if any: dragging from it moves the selection (SU-129).
    Troop _pressTroop;
    Troop _lastClickTroop;
    float _lastClickTime;
    Vector2? _orderStart;
    Vector2 _mousePosition;
    bool _toggleSelection;
    bool _paused;

    public bool IsPaused => _paused;

    // While set (the deployment phase), Move and Line orders place troops at once through it instead of ordering them.
    public ITroopPlacement Placement { get; set; }

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
        // Each battle starts at normal speed.
        GamePause.SetSpeed(BattleSpeedHud.NormalSpeed);
        // The paused indicator sits mid-screen; it must not swallow clicks, since orders can be given while paused.
        if (_pausedIndicator != null)
            foreach (var graphic in _pausedIndicator.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                graphic.raycastTarget = false;
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
        bool overLog = _combatLog.Contains(_mousePosition);
        bool overHud = _cards.Contains(_mousePosition) || _speedHud.Contains(_mousePosition) || overLog;
        // Scrolling the combat log does not zoom the camera.
        PlayerControls.CameraControls.ZoomBlocked = overLog;
        _selection.UpdateHover(overHud ? null : ScreenToWorld(_mousePosition),
            TroopScreenSpace.PixelsToWorld(_camera, _mousePosition, TroopTuning.Active.ClickPickRadiusPixels));
        _cards.Update(_selection.Selected, _selection.Hovered, _mousePosition);
        _healthBar.Update(_selection.Troops, overHud ? null : ScreenToWorld(_mousePosition));
        _rangeMask.Update(_selection.Hovered, RangeMaskStyle());

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f5Key.wasPressedThisFrame) _engagementView = NextEngagementView(_engagementView);
        _engagementViewMesh.Update(_selection.Troops, _engagementView, EngagementStyle());

        if (PausePressed(keyboard)) SetPaused(!_paused);
        // Selection and orders work while paused too: troops act on them once time runs again.

        bool overUi = IsPointerOverUi();
        if (mouse.leftButton.wasPressedThisFrame && !overHud && !overUi)
        {
            _selectionStart = _mousePosition;
            _toggleSelection = IsShiftHeld();
            PressOnTroop(_mousePosition);
        }
        if (mouse.leftButton.wasReleasedThisFrame && _selectionStart.HasValue)
        {
            if (_pressTroop != null) FinishTroopDrag(_selectionStart.Value, _mousePosition);
            else FinishSelection(_selectionStart.Value, _mousePosition);
            _selectionStart = null;
            _pressTroop = null;
        }

        if (mouse.rightButton.wasPressedThisFrame && !overHud && !overUi)
            _orderStart = _mousePosition;
        if (mouse.rightButton.wasReleasedThisFrame && _orderStart.HasValue)
        {
            if (Placement != null) _planner.FinishPlacement(_orderStart.Value, _mousePosition, _camera, Placement);
            else _planner.FinishOrder(_orderStart.Value, _mousePosition, _camera, IsAltHeld());
            _orderStart = null;
        }
    }

    void OnDisable()
    {
        _selection.ClearHover();
        _rangeMask.Hide();
        _engagementViewMesh.Hide();
    }

    // Pauses or resumes the battle. Does nothing while something else (a cutscene, deployment) has stopped time.
    public void SetPaused(bool paused)
    {
        if (paused == _paused) return;
        if (paused && GamePause.IsPaused) return;
        _paused = paused;
        GamePause.Set(this, paused);
        if (_pausedIndicator != null) _pausedIndicator.SetActive(paused);
    }

    // Sets the game speed (GamePause.Speed) and resumes the battle if the player had paused it.
    public void SetSpeed(float speed)
    {
        GamePause.SetSpeed(speed);
        SetPaused(false);
    }

    // A speed bar button: Pause pauses (keeping the speed); the others set their speed and resume.
    public void PressSpeedButton(BattleSpeedButton button)
    {
        if (button == BattleSpeedButton.Pause) SetPaused(true);
        else SetSpeed(BattleSpeedHud.SpeedOf(button));
    }

    // The speed bar shows while the battle is being fought (or in a scene without a Battle).
    static bool ShowSpeedBar()
    {
        var battle = Battle.Instance;
        return battle == null || battle.Phase == BattlePhase.Fighting;
    }

    static bool IsPointerOverUi() =>
        UnityEngine.EventSystems.EventSystem.current != null &&
        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

    // Same keys as the Pause action in PlayerInputMap: P, or Start on a gamepad.
    static bool PausePressed(Keyboard keyboard) =>
        (keyboard != null && keyboard.pKey.wasPressedThisFrame) ||
        (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

    void OnDestroy()
    {
        GamePause.Release(this);
        // Leave the battle's speed behind: the campaign and the next battle run at normal speed.
        GamePause.SetSpeed(BattleSpeedHud.NormalSpeed);
        _speedHud.Destroy();
        PlayerControls.CameraControls.ZoomBlocked = false;
        _rangeMask.Destroy();
        _engagementViewMesh.Destroy();
    }

    // A left press on one of the player's troops selects it at once (a double click: its whole chain), so a drag that
    // follows moves what was just selected (SU-129).
    void PressOnTroop(Vector2 screenPosition)
    {
        Troop troop = _selection.TroopAtPoint(ScreenToWorld(screenPosition),
            TroopScreenSpace.PixelsToWorld(_camera, screenPosition, TroopTuning.Active.ClickPickRadiusPixels),
            _playerTeamId, sameTeam: true);
        _pressTroop = troop;
        if (troop == null)
        {
            _lastClickTroop = null;
            return;
        }
        bool doubleClick = troop == _lastClickTroop &&
                           Time.unscaledTime - _lastClickTime <= TroopTuning.Active.DoubleClickSeconds;
        _selection.SelectTroop(troop, doubleClick, _toggleSelection);
        // A third click starts a new double click rather than completing another one.
        _lastClickTroop = doubleClick ? null : troop;
        _lastClickTime = Time.unscaledTime;
    }

    // Left release after a press on a troop: the selection was made on the press; a drag of at least OrderDragMinPixels
    // (without Shift) moves the selection to the release point, or places it while deploying.
    void FinishTroopDrag(Vector2 start, Vector2 end)
    {
        if (!IsTroopDrag(start, end)) return;
        if (Placement != null) _planner.FinishPlacementMove(end, _camera, Placement);
        else _planner.FinishMove(end, _camera, IsAltHeld());
    }

    bool IsTroopDrag(Vector2 start, Vector2 end) =>
        _pressTroop != null && !_toggleSelection && _selection.Selected.Count > 0 &&
        Vector2.Distance(start, end) >= TroopTuning.Active.OrderDragMinPixels;

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
        if (_camera != null) DrawOrderPaths();
        if (_camera != null) _healthBar.Draw(_camera);
        _cards.Draw();
        _combatLog.Draw();

        if (ShowSpeedBar())
        {
            var active = BattleSpeedHud.ActiveButton(_paused || GamePause.IsPaused, GamePause.Speed);
            if (_speedHud.Draw(active, out var clicked)) PressSpeedButton(clicked);
        }
        else _speedHud.Hide();

        if (_selectionStart.HasValue && Mouse.current != null && _camera != null && _pressTroop != null)
        {
            if (IsTroopDrag(_selectionStart.Value, _mousePosition)) DrawMovePreview(_mousePosition);
        }
        else if (_selectionStart.HasValue && Mouse.current != null)
            TroopOrderOverlay.DrawSelectionBox(_selectionStart.Value, _mousePosition);
        if (_orderStart.HasValue && Mouse.current != null && _camera != null && _selection.Selected.Count > 0)
            DrawOrderPreview(_orderStart.Value, _mousePosition);
    }

    // The order arrows of every selected troop, all the time, and of the hovered troop (any team), each drawn once.
    void DrawOrderPaths()
    {
        TroopOrderPathView.Style style = OrderPathStyle();
        Troop hovered = _selection.Hovered;
        bool hoveredDrawn = false;
        foreach (var troop in _selection.Selected)
        {
            if (troop == null) continue;
            _orderPath.Draw(_camera, troop, style);
            if (troop == hovered) hoveredDrawn = true;
        }
        if (!hoveredDrawn) _orderPath.Draw(_camera, hovered, style);
    }

    // Ghost preview of a left-drag move (SU-129): a marker at each troop's planned spot and its chain tethers.
    void DrawMovePreview(Vector2 screenEnd)
    {
        bool alt = IsAltHeld();
        _planner.PreviewMove(screenEnd, _camera, alt);
        foreach (var placement in _planner.Placements)
            TroopOrderOverlay.DrawMarker(TroopScreenSpace.WorldToGui(_camera, placement.Center), placement.Troop.ArmyColor);
        foreach (var tether in _planner.Tethers)
            TroopOrderOverlay.DrawTether(TroopScreenSpace.WorldToGui(_camera, tether.From),
                TroopScreenSpace.WorldToGui(_camera, tether.To), tether.StaysLinked && !alt);
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
