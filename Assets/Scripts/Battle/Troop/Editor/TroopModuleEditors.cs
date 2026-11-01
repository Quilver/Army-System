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
        DrawSoldierHealth(roster);
    }

    static bool _showSoldiers = true;

    // One HP bar per soldier (spec §4.6): id, character, current / max HP, bleed and bruise.
    static void DrawSoldierHealth(TroopRoster roster)
    {
        // Drawn enabled inside the read-only runtime block, so the foldout can be clicked (the bars cannot be edited).
        bool wasEnabled = GUI.enabled;
        GUI.enabled = true;
        _showSoldiers = EditorGUILayout.Foldout(_showSoldiers, "Soldier HP", true);
        if (!_showSoldiers)
        {
            GUI.enabled = wasEnabled;
            return;
        }

        EditorGUI.indentLevel++;
        foreach (var soldier in roster.Soldiers)
        {
            string name = soldier.Character != null ? soldier.Character.CharacterName : "Soldier";
            string state = soldier.IsAlive ? "" : "   DEAD";
            Rect rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            SoldierHealthGUI.DrawBar(rect, soldier, "#" + soldier.Id + " " + name + "   " + SoldierHealthGUI.HpText(soldier) + state);
        }
        EditorGUI.indentLevel--;
        GUI.enabled = wasEnabled;
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
        EditorGUILayout.Toggle("Threat Within S_Distance", state.HasThreat);
        EditorGUILayout.Toggle("Threat Facing", state.IsThreatFacing);
        if (formation != null)
        {
            EditorGUILayout.Vector2Field("Start (right)", formation.Start);
            EditorGUILayout.Vector2Field("End (left)", formation.End);
            EditorGUILayout.Vector2Field("Facing", formation.Facing);
        }
        EditorGUILayout.Vector2Field("Threat Heading", state.Heading);
        EditorGUILayout.FloatField("Threat Distance", state.ThreatDistance);
    }
}

// Morale (spec §5): EffectiveMorale, the initial value, Shock and its decay, the break counts, and the case that would
// apply if the troop broke now.
[CustomEditor(typeof(TroopMorale))]
public sealed class TroopMoraleEditor : TroopModuleEditor
{
    protected override void DrawRuntimeState()
    {
        var morale = (TroopMorale)target;
        DrawMorale(morale, morale.GetComponent<Troop>());
    }

    public static void DrawMorale(TroopMorale morale, Troop troop)
    {
        if (morale == null) return;
        bool surrounded = troop != null && troop.IsSurrounded();
        EditorGUILayout.LabelField("Morale", EditorStyles.boldLabel);
        EditorGUILayout.FloatField("Effective Morale", morale.EffectiveMorale);
        EditorGUILayout.FloatField("Initial Effective Morale", morale.InitialEffectiveMorale);
        EditorGUILayout.FloatField("Shock", morale.Shock);
        EditorGUILayout.FloatField("Shock Decay / s", morale.DecayPerSecond);
        EditorGUILayout.IntField("Retreats", morale.RetreatCount);
        EditorGUILayout.IntField("Surrounded Breaks", morale.SurroundedBreakCount);
        EditorGUILayout.Toggle("Surrounded", surrounded);
        EditorGUILayout.EnumPopup("Case If Broken Now", morale.CaseIfBroken(surrounded));
        if (troop != null) EditorGUILayout.Toggle("Routed", troop.IsRouted);
    }
}
