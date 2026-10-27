using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

// Live status of the selected troops (right-hand panel) and movement/combat gizmos. Add it to one GameObject in a battle
// scene. F3 toggles the panel, F5 adds the debug details (line push, barrier share, links, break counts), F4 toggles
// gizmos. Gizmos are drawn in the Scene view, or in the Game view with Gizmos on.
// The panel shows, per selected troop: formation, activity (Idle, Moving, Pursuing, Holding line, Engaged, Retreating,
// Routed), Effective Morale, Shock, velocity, soldiers, and each nearby or engaged enemy.
// Gizmos show each troop's footprint, the face each opponent attacks (label at the face, spec §4.8) and the footprint
// gap of each engagement.
[DisallowMultipleComponent]
public sealed class TroopDebugOverlay : MonoBehaviour
{
    const float TensionColourRange = 1.5f;   // tension at which a link is drawn fully red
    const float JointRadius = 0.2f;
    const float ChargeMarkerRadius = 0.12f;
    const float OverlayFontSize = 32;
    const float MinOverlayFontSize = 14;
    const float MovingSpeedSquared = 0.0025f;   // pivot speed (0.05 u/s) above which a troop counts as moving

    static readonly Color PivotColour = Color.cyan;
    static readonly Color PushColour = Color.yellow;
    static readonly Color EngagementColour = Color.magenta;
    static readonly Color BarrierColour = new(1f, 0.5f, 0f);
    static readonly Color ChargeColour = Color.red;
    static readonly Color ClashColour = new(1f, 0.35f, 0.1f);

    [SerializeField] bool _showOverlay = true;
    [SerializeField] bool _showGizmos = true;
    [SerializeField] bool _showDetails;
    [SerializeField] bool _gizmosSelectedOnly;
    [SerializeField, Min(0f)] float _vectorScale = 0.5f;     // world units per unit of velocity
    [SerializeField, Min(0f)] float _barrierScale = 10f;     // penetration is ~0.3 units, so it is drawn larger

    readonly StringBuilder _text = new(256);

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f3Key.wasPressedThisFrame) _showOverlay = !_showOverlay;
            if (keyboard.f4Key.wasPressedThisFrame) _showGizmos = !_showGizmos;
            if (keyboard.f5Key.wasPressedThisFrame) _showDetails = !_showDetails;
        }
        TroopDebugSnapshot.Enabled = isActiveAndEnabled && (_showOverlay || _showGizmos);
    }

    void OnDisable()
    {
        TroopDebugSnapshot.Enabled = false;
        TroopDebugSnapshot.Clear();
    }

    // --- Overlay ---

    void OnGUI()
    {
        if (!_showOverlay || !Application.isPlaying) return;

        _text.Clear();
        int shown = 0;
        foreach (Troop troop in FindObjectsByType<Troop>(FindObjectsSortMode.None))
        {
            if (troop == null || !troop.IsSelected) continue;
            if (shown > 0) _text.Append("\n\n");
            AppendTroop(troop);
            shown++;
        }

        string message = shown == 0 ? "Troop status (F3): select a troop to see its status." : _text.ToString();
        float panelWidth = Screen.width * 0.25f;
        float panelHeight = Screen.height * 0.75f;
        var panel = new Rect(Screen.width - panelWidth - 10f, 10f, panelWidth, panelHeight);

        GUI.Box(panel, GUIContent.none);
        var inner = new Rect(panel.x + 12f, panel.y + 12f, panel.width - 24f, panel.height - 24f);
        GUIStyle style = new(GUI.skin.label) { fontSize = Mathf.RoundToInt(OverlayFontSize), wordWrap = true, richText = true };
        // Shrink the text until it fits the panel (several selected troops, or the F5 details).
        var content = new GUIContent(message);
        while (style.fontSize > MinOverlayFontSize && style.CalcHeight(content, inner.width) > inner.height)
            style.fontSize -= 2;
        GUI.Label(inner, content, style);
    }

    void AppendTroop(Troop troop)
    {
        int charging = 0;
        int following = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            if (soldier.IsCharging) charging++;
            if (soldier.IsFollowing) following++;
        }
        int links = 0;
        for (int end = 0; end < 2; end++) if (troop.IsLinkActive(end)) links++;

        _text.Append("<b>").Append(troop.name).Append("</b>   team ").Append(troop.TeamId).Append('\n');
        _text.Append("Formation  ").Append(FormationText(troop, links)).Append('\n');
        _text.Append("Status  ").Append(StatusText(troop, charging)).Append('\n');

        float initial = troop.InitialEffectiveMorale;
        _text.Append("Morale  EM ").Append(troop.EffectiveMorale.ToString("0"))
             .Append(" / ").Append(initial.ToString("0"));
        if (initial > 0f) _text.Append(" (").Append((troop.EffectiveMorale / initial * 100f).ToString("0")).Append("%)");
        _text.Append('\n');

        // A break needs Shock above Effective Morale (spec §5.3).
        _text.Append("Shock  ").Append(troop.Shock.ToString("0.0"))
             .Append("  (breaks above ").Append(troop.EffectiveMorale.ToString("0"))
             .Append(", decays ").Append(troop.MoraleState.DecayPerSecond.ToString("0.0")).Append("/s)").Append('\n');

        Vector2 velocity = troop.PivotVelocity;
        _text.Append("Velocity  ").Append(velocity.magnitude.ToString("0.00")).Append(" u/s  ").Append(Fmt(velocity)).Append('\n');

        _text.Append("Soldiers  ").Append(troop.LivingSoldierCount).Append('/').Append(troop.SoldierCount)
             .Append("   charging ").Append(charging).Append("   following ").Append(following);

        foreach (var record in troop.Engagements)
        {
            if (record.Opponent == null) continue;
            _text.Append('\n').Append(record.Engaged ? "Engaged with " : "Near ").Append(record.Opponent.name)
                 .Append("  face ").Append(record.Face)
                 .Append("  gap ").Append(record.Gap.ToString("0.0"));
        }

        if (!_showDetails) return;
        _text.Append('\n').Append("unit state ").Append(troop.State)
             .Append("   line push v ").Append(Fmt(troop.LinePushVelocity));
        _text.Append('\n').Append("assault ").Append(troop.IsAssaulting ? troop.AssaultTimeRemaining.ToString("0.0") + " s" : "no")
             .Append("   barrier share ").Append(BarrierShareText(troop));
        _text.Append('\n').Append("links ").Append(links).Append(troop.ChainSuppressed ? " (suppressed)" : "")
             .Append("   retreats ").Append(troop.MoraleState.RetreatCount)
             .Append("   surrounded ").Append(troop.MoraleState.SurroundedBreakCount);
    }

    // The formation state (spec §3.1) with what drives it.
    static string FormationText(Troop troop, int links)
    {
        FormationMode mode = troop.FormationMode;
        switch (mode)
        {
            case FormationMode.Line:
            case FormationMode.Chain:
                string text = mode + "  length " + troop.LineLength.ToString("0.0");
                if (mode == FormationMode.Chain) text += links == 1 ? "  1 link" : "  " + links + " links";
                if (troop.HasManualLineOrder) text += "  (ordered)";
                else if (troop.HasThreatFacingFormation) text += "  (facing threat)";
                return text;
            case FormationMode.Column:
                return "Column (marching)";
            case FormationMode.Rout:
                return "Rout (scattered)";
            default:
                return "Idle (blob)";
        }
    }

    // What the troop is doing, most important first.
    static string StatusText(Troop troop, int charging)
    {
        if (troop.IsRouted) return "Routed";
        if (troop.IsRetreating) return "Retreating";
        if (troop.IsFaultSlipping) return "Falling back (chain)";
        if (troop.IsEngaged)
        {
            string text = "Engaged";
            Troop enemy = troop.EngagedEnemy;
            if (enemy != null) text += " with " + enemy.name;
            if (troop.IsAssaulting) text += ", assault " + troop.AssaultTimeRemaining.ToString("0.0") + " s";
            if (charging > 0) text += ", " + charging + " charging";
            return text;
        }
        if (troop.IsPursuing) return troop.PursuitTarget != null ? "Pursuing " + troop.PursuitTarget.name : "Pursuing";
        bool moving = troop.IsMoving || troop.PivotVelocity.sqrMagnitude > MovingSpeedSquared;
        if (moving) return "Moving";
        if (troop.HasManualLineOrder) return "Holding line";
        return "Idle";
    }

    // Share from the most recent barrier this troop is part of (this frame or the last).
    static string BarrierShareText(Troop troop)
    {
        int frame = Time.frameCount;
        var barriers = TroopDebugSnapshot.Barriers;
        for (int i = barriers.Count - 1; i >= 0; i--)
        {
            TroopDebugSnapshot.BarrierRecord record = barriers[i];
            if (record.Frame < frame - 1) continue;
            if (record.First == troop) return record.FirstShare.ToString("0.00");
            if (record.Second == troop) return record.SecondShare.ToString("0.00");
        }
        return "-";
    }

    // --- Gizmos ---

    void OnDrawGizmos()
    {
        if (!_showGizmos || !Application.isPlaying) return;

        foreach (Troop troop in FindObjectsByType<Troop>(FindObjectsSortMode.None))
        {
            if (troop == null || (_gizmosSelectedOnly && !troop.IsSelected)) continue;
            DrawTroop(troop);
        }
        DrawBarriers();
        DrawClashes();
    }

    void DrawTroop(Troop troop)
    {
        Vector3 pivot = troop.transform.position;

        Gizmos.color = PivotColour;
        Gizmos.DrawLine(pivot, pivot + (Vector3)(troop.PivotVelocity * _vectorScale));

        Gizmos.color = PushColour;
        Gizmos.DrawLine(pivot, pivot + (Vector3)(troop.LinePushVelocity * _vectorScale));

        TroopCombatGizmos.DrawFootprint(troop.Footprint, troop.ArmyColor);
        foreach (var record in troop.Engagements)
        {
            if (!record.Engaged || record.Opponent == null) continue;
            Gizmos.color = record.Face.IsFlanked() ? Color.red : EngagementColour;
            Vector3 face = troop.Footprint.FaceCenter(record.Face);
            Gizmos.DrawLine(face, record.Opponent.transform.position);
            Gizmos.DrawWireSphere(face, JointRadius * 1.5f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(face, $"{record.Face} ({record.Gap:0.0})");
#endif
        }

        for (int end = 0; end < 2; end++)
        {
            TroopEndpointLink link = troop.GetLineLink(end);
            if (link == null || link.Other == null || link.Pending) continue;
            Vector3 joint = link.Joint;
            float stretch = Mathf.Clamp01(link.Tension.magnitude / TensionColourRange);
            Gizmos.color = Color.Lerp(Color.green, Color.red, stretch);
            Gizmos.DrawWireSphere(joint, JointRadius);
            Gizmos.DrawLine(joint, joint + (Vector3)link.Tension);
        }

        Gizmos.color = ChargeColour;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive || !soldier.IsCharging || soldier.ChargeTargetTroop == null) continue;
            Vector2 from = troop.WorldPosition(soldier);
            Vector2 to = soldier.ChargeTargetTroop.WorldPosition(soldier.ChargeTarget);
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, ChargeMarkerRadius);
        }
    }

    void DrawBarriers()
    {
        int frame = Time.frameCount;
        Gizmos.color = BarrierColour;
        var barriers = TroopDebugSnapshot.Barriers;
        for (int i = 0; i < barriers.Count; i++)
        {
            TroopDebugSnapshot.BarrierRecord record = barriers[i];
            if (record.Frame < frame - 1 || record.First == null || record.Second == null) continue;
            Vector3 start = record.First.transform.position;
            Gizmos.DrawLine(start, start + (Vector3)(record.Axis * record.Penetration * _barrierScale));
        }
    }

    void DrawClashes()
    {
        float now = Time.time;
        var clashes = TroopDebugSnapshot.Clashes;
        for (int i = 0; i < clashes.Count; i++)
        {
            float age = (now - clashes[i].Time) / TroopDebugSnapshot.ClashMarkerLifetime;
            if (age < 0f || age > 1f) continue;
            Gizmos.color = new Color(ClashColour.r, ClashColour.g, ClashColour.b, 1f - age);
            Gizmos.DrawWireSphere(clashes[i].Position, 0.3f + 0.4f * age);
        }
    }

    static string Fmt(Vector2 v) => $"({v.x:0.00}, {v.y:0.00})";
}
