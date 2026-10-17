using UnityEditor;
using UnityEngine;

// Inspectors for the three toggleable troop modules. Each shows its serialized fields plus live read-only state
// while the game is running.
public abstract class TroopModuleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying) return;

        using (new EditorGUI.DisabledScope(true))
            DrawRuntimeState();
        Repaint();
    }

    protected abstract void DrawRuntimeState();
}

[CustomEditor(typeof(TroopRoster))]
public sealed class TroopRosterEditor : TroopModuleEditor
{
    protected override void DrawRuntimeState()
    {
        var roster = (TroopRoster)target;
        EditorGUILayout.IntField("Soldiers", roster.Count);
        EditorGUILayout.IntField("Living", roster.LivingCount);
    }
}

[CustomEditor(typeof(TroopFormationState))]
public sealed class TroopFormationStateEditor : TroopModuleEditor
{
    protected override void DrawRuntimeState()
    {
        var state = (TroopFormationState)target;
        var formation = state.Formation;
        EditorGUILayout.Toggle("Has Line", formation != null);
        EditorGUILayout.Toggle("Threat Facing", state.IsThreatFacing);
        if (formation != null)
        {
            EditorGUILayout.Vector2Field("Start", formation.Start);
            EditorGUILayout.Vector2Field("End", formation.End);
            EditorGUILayout.Vector2Field("Facing", formation.Facing);
        }
        EditorGUILayout.Vector2Field("Threat Heading", state.Heading);
        EditorGUILayout.FloatField("Threat Distance", state.ThreatDistance);
    }
}

[CustomEditor(typeof(TroopMorale))]
public sealed class TroopMoraleEditor : TroopModuleEditor
{
    protected override void DrawRuntimeState()
    {
        var morale = (TroopMorale)target;
        EditorGUILayout.FloatField("Morale", morale.Value);
        EditorGUILayout.FloatField("Shock", morale.Shock);
        EditorGUILayout.FloatField("Fault Pressure", morale.FaultPressure);
        EditorGUILayout.FloatField("Break Cooldown", morale.BreakCooldownRemaining);
        EditorGUILayout.Toggle("Pulse Locked", morale.IsPulseLocked);
    }
}