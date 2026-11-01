using UnityEngine;
using UnityEngine.InputSystem;

// Movement and combat gizmos. Add it to one GameObject in a battle scene. F4 toggles the gizmos, which are drawn in the
// Scene view, or in the Game view with Gizmos on. (The on-screen status panel was removed; the troop cards drawn by
// TroopCardHud show the selected and hovered troops.)
// Gizmos show each troop's footprint, the face each opponent attacks (label at the face, spec §4.8) and the footprint
// gap of each engagement.
[DisallowMultipleComponent]
public sealed class TroopDebugOverlay : MonoBehaviour
{
    const float TensionColourRange = 1.5f;   // tension at which a link is drawn fully red
    const float JointRadius = 0.2f;
    const float ChargeMarkerRadius = 0.12f;

    static readonly Color PivotColour = Color.cyan;
    static readonly Color PushColour = Color.yellow;
    static readonly Color EngagementColour = Color.magenta;
    static readonly Color BarrierColour = new(1f, 0.5f, 0f);
    static readonly Color ChargeColour = Color.red;
    static readonly Color ClashColour = new(1f, 0.35f, 0.1f);

    [SerializeField] bool _showGizmos = true;
    [SerializeField] bool _gizmosSelectedOnly;
    [SerializeField, Min(0f)] float _vectorScale = 0.5f;     // world units per unit of velocity
    [SerializeField, Min(0f)] float _barrierScale = 10f;     // penetration is ~0.3 units, so it is drawn larger

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.f4Key.wasPressedThisFrame) _showGizmos = !_showGizmos;
        }
        TroopDebugSnapshot.Enabled = isActiveAndEnabled && _showGizmos;
    }

    void OnDisable()
    {
        TroopDebugSnapshot.Enabled = false;
        TroopDebugSnapshot.Clear();
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
}
