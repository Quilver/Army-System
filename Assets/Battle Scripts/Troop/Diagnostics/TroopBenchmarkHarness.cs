using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Visual test harness for troop behaviour. Builds the benchmark cases (TroopBenchmarkCases) and provides pause,
// single-step, slow motion, case focus, gizmos and a live inspection panel.
//
// Starts automatically in the scene named AutoStartScene. In other scenes, add this component to any GameObject.
// Existing troops in the scene are removed on start (ClearExistingTroops), so the cases run on a controlled layout.
//
// Keys: Space pause/resume   . step one frame   - / = slower / faster   Left / Right previous / next case
//       R rebuild all cases   G gizmos (TroopDiagnostics)
// Mouse: left-click selects player troops (team 0), right-drag orders them (TroopPlayerOrders).
[DisallowMultipleComponent]
public sealed class TroopBenchmarkHarness : MonoBehaviour
{
    const string AutoStartScene = "Level 1 Troop Test";
    const float ViewHalfHeight = 50f;
    const float PanelWidth = 360f;
    const float CaseListHeight = 240f;
    const float WoundDamagePerHit = 20f;
    static readonly float[] Speeds = { 0.1f, 0.25f, 0.5f, 1f, 2f };

    [SerializeField] Vector2 _origin = Vector2.zero;
    [SerializeField, Min(20f)] float _spacing = 110f;
    [SerializeField] bool _clearExistingTroops = true;
    [SerializeField] bool _gizmos = true;

    readonly List<BenchmarkCase> _cases = new();
    BenchmarkProfiles _profiles;
    Vector2 _caseScroll;
    GUIStyle _activeButton;
    int _active;
    int _speedIndex = 3;
    bool _paused;
    bool _stepRequested;
    bool _stepping;
    int _obstacleLayer = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (SceneManager.GetActiveScene().name != AutoStartScene) return;
        if (FindFirstObjectByType<TroopBenchmarkHarness>() != null) return;
        new GameObject("Troop Benchmark Harness").AddComponent<TroopBenchmarkHarness>();
    }

    // --- Lifecycle ---

    void Start()
    {
        _obstacleLayer = ObstacleLayer();
        if (FindFirstObjectByType<TroopCombatWorld>() == null) gameObject.AddComponent<TroopCombatWorld>();
        _profiles = new BenchmarkProfiles();
        if (_clearExistingTroops) ClearExistingTroops();
        BuildAll();
        ApplyGizmos();
        Focus(0);
    }

    void Update()
    {
        HandleKeys();
        for (int i = 0; i < _cases.Count; i++) _cases[i].Tick(Time.deltaTime);
    }

    // Pause and single-step are applied here, after the troops have updated. Time.deltaTime is read at the start of each
    // frame, so a change made in LateUpdate takes effect on the next frame. A step therefore runs exactly one frame at
    // the current speed, then pauses again.
    void LateUpdate()
    {
        if (_stepping)
        {
            _stepping = false;
        }
        else if (_stepRequested)
        {
            _stepRequested = false;
            _stepping = true;
            Time.timeScale = Speeds[_speedIndex];
            return;
        }
        Time.timeScale = _paused ? 0f : Speeds[_speedIndex];
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        _profiles?.Dispose();
        _profiles = null;
    }

    // --- Keys and time control ---

    void HandleKeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.spaceKey.wasPressedThisFrame) TogglePause();
        if (keyboard.periodKey.wasPressedThisFrame) StepFrame();
        if (keyboard.minusKey.wasPressedThisFrame) ChangeSpeed(-1);
        if (keyboard.equalsKey.wasPressedThisFrame) ChangeSpeed(1);
        if (keyboard.leftArrowKey.wasPressedThisFrame) Focus(_active - 1);
        if (keyboard.rightArrowKey.wasPressedThisFrame) Focus(_active + 1);
        if (keyboard.rKey.wasPressedThisFrame) BuildAll();
        if (keyboard.gKey.wasPressedThisFrame) SetGizmos(!_gizmos);
    }

    void TogglePause() => _paused = !_paused;

    void StepFrame()
    {
        _paused = true;
        _stepRequested = true;
    }

    void ChangeSpeed(int direction) =>
        _speedIndex = Mathf.Clamp(_speedIndex + direction, 0, Speeds.Length - 1);

    // --- Case lifecycle ---

    void BuildAll()
    {
        ClearCases();
        var definitions = TroopBenchmarkCases.All;
        for (int i = 0; i < definitions.Count; i++) _cases.Add(null);
        for (int i = 0; i < definitions.Count; i++) BuildCase(i);
        _active = Mathf.Clamp(_active, 0, Mathf.Max(0, _cases.Count - 1));
    }

    void RebuildCase(int index)
    {
        if (index < 0 || index >= _cases.Count) return;
        if (_cases[index] != null && _cases[index].Root != null) Destroy(_cases[index].Root);
        BuildCase(index);
    }

    void BuildCase(int index)
    {
        CaseDefinition definition = TroopBenchmarkCases.All[index];
        var benchmarkCase = new BenchmarkCase
        {
            Index = index,
            Name = definition.Name,
            Expectation = definition.Expectation,
            Origin = _origin + Vector2.right * (index * _spacing),
            Root = new GameObject("Case " + definition.Name),
        };
        benchmarkCase.Root.transform.SetParent(transform, false);

        definition.Build(new BenchmarkBuilder(_profiles, benchmarkCase, _obstacleLayer));
        benchmarkCase.Schedule.Sort((a, b) => a.Time.CompareTo(b.Time));
        if (benchmarkCase.Primary == null && benchmarkCase.Troops.Count > 0)
            benchmarkCase.Primary = benchmarkCase.Troops[0];
        _cases[index] = benchmarkCase;
    }

    void ClearCases()
    {
        foreach (var benchmarkCase in _cases)
            if (benchmarkCase != null && benchmarkCase.Root != null) Destroy(benchmarkCase.Root);
        _cases.Clear();
    }

    void ClearExistingTroops()
    {
        foreach (var troop in FindObjectsByType<Troop>(FindObjectsSortMode.None))
            if (troop != null) Destroy(troop.gameObject);
    }

    // --- Camera ---

    void Focus(int index)
    {
        if (_cases.Count == 0) return;
        _active = ((index % _cases.Count) + _cases.Count) % _cases.Count;
        Vector2 centre = _cases[_active].Origin + new Vector2(10f, 0f);
        PositionCamera(centre);
    }

    void PositionCamera(Vector2 centre)
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        Transform cameraTransform = camera.transform;
        if (camera.orthographic)
        {
            camera.orthographicSize = ViewHalfHeight;
            cameraTransform.position = new Vector3(centre.x, centre.y, cameraTransform.position.z);
        }
        else
        {
            float distance = ViewHalfHeight / Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cameraTransform.position = new Vector3(centre.x, centre.y, -distance);
        }
    }

    // --- Diagnostics gizmos ---

    void SetGizmos(bool on)
    {
        _gizmos = on;
        ApplyGizmos();
    }

    void ApplyGizmos()
    {
        var diagnostics = FindFirstObjectByType<TroopDiagnostics>();
        if (diagnostics == null)
        {
            if (!_gizmos) return;
            diagnostics = gameObject.AddComponent<TroopDiagnostics>();
        }
        diagnostics.SetEnabled(_gizmos);
    }

    // --- Panel ---

    void OnGUI()
    {
        if (_activeButton == null)
            _activeButton = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

        var area = new Rect(Screen.width - PanelWidth - 10f, 10f, PanelWidth, Screen.height - 20f);
        GUILayout.BeginArea(area, GUI.skin.box);
        DrawStatus();
        DrawControls();
        DrawCaseList();
        DrawCaseDetail();
        DrawInspector();
        GUILayout.EndArea();
    }

    void DrawStatus()
    {
        string state = _paused ? (_stepping ? "PAUSED (step)" : "PAUSED") : "RUNNING";
        GUILayout.Label($"{state}   speed x{Speeds[_speedIndex]}   t = {Time.time:0.0} s   frame {Time.frameCount}");
        if (_obstacleLayer < 0)
            GUILayout.Label("Warning: no Impassable or Terrain layer. Wall cases will not block soldiers.");
    }

    void DrawControls()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(_paused ? "Resume (Space)" : "Pause (Space)")) TogglePause();
        if (GUILayout.Button("Step frame (.)")) StepFrame();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Slower (-)")) ChangeSpeed(-1);
        if (GUILayout.Button("Faster (=)")) ChangeSpeed(1);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rebuild all (R)")) BuildAll();
        bool gizmos = GUILayout.Toggle(_gizmos, "Gizmos (G)");
        if (gizmos != _gizmos) SetGizmos(gizmos);
        GUILayout.EndHorizontal();

        GUILayout.Label("Arrows change case. Click a player troop to inspect it.");
    }

    void DrawCaseList()
    {
        GUILayout.Space(4f);
        GUILayout.Label("Cases");
        _caseScroll = GUILayout.BeginScrollView(_caseScroll, GUILayout.Height(CaseListHeight));
        for (int i = 0; i < _cases.Count; i++)
        {
            if (_cases[i] == null) continue;
            GUIStyle style = i == _active ? _activeButton : GUI.skin.button;
            if (GUILayout.Button(_cases[i].Name, style)) Focus(i);
        }
        GUILayout.EndScrollView();
    }

    void DrawCaseDetail()
    {
        if (_cases.Count == 0 || _cases[_active] == null) return;
        BenchmarkCase benchmarkCase = _cases[_active];
        GUILayout.Space(4f);
        GUILayout.Label($"{benchmarkCase.Name}   elapsed {benchmarkCase.Elapsed:0.0} s");
        GUILayout.Label("Expect: " + benchmarkCase.Expectation);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Restart this case")) RebuildCase(_active);
        if (GUILayout.Button("Focus primary troop")) PositionCamera(benchmarkCase.Primary != null ? (Vector2)benchmarkCase.Primary.transform.position : benchmarkCase.Origin);
        GUILayout.EndHorizontal();
    }

    void DrawInspector()
    {
        GUILayout.Space(6f);
        GUILayout.Label("Inspect");
        Troop troop = Inspected();
        if (troop == null)
        {
            GUILayout.Label("No troop selected. The case's primary troop appears here when no troop is selected.");
            return;
        }

        int charging = 0;
        int living = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            living++;
            if (soldier.IsCharging) charging++;
        }

        GUILayout.Label($"{troop.name}  #{troop.GetInstanceID()}  team {troop.TeamId}  soldiers {troop.LivingSoldierCount}/{troop.SoldierCount}");
        GUILayout.Label($"State {troop.State}   moving {troop.IsMoving}   ordered move {troop.HasManualMoveOrder}   in melee {troop.InMelee}");
        GUILayout.Label($"Pivot velocity {Fmt(troop.PivotVelocity)}  |{troop.PivotVelocity.magnitude:0.00}|");
        GUILayout.Label($"Line push velocity {Fmt(troop.LinePushVelocity)}");
        GUILayout.Label($"Destination {Fmt(troop.Destination)}");
        GUILayout.Label($"Engaged enemy {(troop.EngagedEnemy != null ? troop.EngagedEnemy.name : "none")}   threat edge {troop.ThreatEdgeDistance:0.0}");
        GUILayout.Label($"Links {troop.LineLinks.Count}   free end {troop.HasFreeLineEnd}");
        GUILayout.Label($"Charging {charging}/{living}   ready attacks {troop.ReadyMeleeAttacks}   held {troop.HeldMeleeAttacks}");
        GUILayout.Label($"Shock {troop.Shock:0.0}/{troop.Morale:0}   fault pressure {troop.FaultPressure:0.00}   retreating {troop.IsFaultSlipping}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Wound past morale")) Wound(troop);
        if (GUILayout.Button("Centre camera")) PositionCamera(troop.transform.position);
        GUILayout.EndHorizontal();
    }

    // The first selected troop (any team), or the active case's primary troop.
    Troop Inspected()
    {
        foreach (var troop in FindObjectsByType<Troop>(FindObjectsSortMode.None))
            if (troop != null && troop.IsSelected) return troop;
        if (_cases.Count == 0 || _cases[_active] == null) return null;
        return _cases[_active].Primary;
    }

    // Applies enough damage to push the troop past its morale, so it breaks on its next update.
    void Wound(Troop troop)
    {
        Vector2 attacker = troop.EngagedEnemy != null
            ? (Vector2)troop.EngagedEnemy.transform.position
            : (Vector2)troop.transform.position + Vector2.down * 10f;
        int hits = Mathf.CeilToInt(troop.Morale / WoundDamagePerHit) + 1;
        int applied = 0;
        for (int i = 0; i < troop.Soldiers.Count && applied < hits; i++)
        {
            if (!troop.Soldiers[i].IsAlive) continue;
            troop.ApplyDamage(troop.Soldiers[i], WoundDamagePerHit, attacker);
            applied++;
        }
    }

    static int ObstacleLayer()
    {
        foreach (string name in new[] { "Impassable", "Impassable Terrain", "Terrain" })
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer >= 0) return layer;
        }
        return -1;
    }

    static string Fmt(Vector2 v) => $"({v.x:0.00}, {v.y:0.00})";
}
