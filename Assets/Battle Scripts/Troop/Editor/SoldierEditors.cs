using UnityEditor;
using UnityEngine;

// Soldier HP in the Inspector (spec §4.6). Select a soldier's sprite object ("Soldier <id>") in Play mode to see its
// HP; the Troop Roster component lists every soldier of its troop (TroopRosterEditor).
[CustomEditor(typeof(SoldierView))]
public sealed class SoldierViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var view = (SoldierView)target;
        SoldierAgentState soldier = view.Soldier;
        if (!Application.isPlaying || soldier == null)
        {
            EditorGUILayout.HelpBox("Shows the soldier's live HP in Play mode.", MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Troop", view.Troop, typeof(Troop), true);
            EditorGUILayout.ObjectField("Character", soldier.Character, typeof(CharacterProfile), false);
            EditorGUILayout.IntField("Soldier Id", soldier.Id);
            EditorGUILayout.EnumPopup("Life State", soldier.LifeState);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
        SoldierHealthGUI.DrawBar(EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 1.4f), soldier,
            "HP " + SoldierHealthGUI.HpText(soldier));
        using (new EditorGUI.DisabledScope(true))
        {
            SoldierHealth health = soldier.Health;
            EditorGUILayout.IntField("Current HP", health.CurrentHp);
            EditorGUILayout.IntField("Max HP", health.MaxHp);
            EditorGUILayout.IntField("Bleed (permanent)", health.Bleed);
            EditorGUILayout.IntField("Bruise (heals)", health.Bruise);
            EditorGUILayout.Toggle("Fatigued", soldier.LifeState == SoldierLifeState.Fatigued);
            EditorGUILayout.EnumPopup("Charge Role", soldier.ChargeRole);
        }
        Repaint();
    }
}

// Shared HP bar drawing for the soldier and roster inspectors.
public static class SoldierHealthGUI
{
    public static string HpText(SoldierAgentState soldier)
    {
        SoldierHealth health = soldier.Health;
        string text = health.CurrentHp + " / " + health.MaxHp;
        if (health.Bleed > 0 || health.Bruise > 0) text += "   (bleed " + health.Bleed + ", bruise " + health.Bruise + ")";
        return text;
    }

    public static void DrawBar(Rect rect, SoldierAgentState soldier, string label)
    {
        SoldierHealth health = soldier.Health;
        float fraction = health.MaxHp > 0 ? (float)health.CurrentHp / health.MaxHp : 0f;
        EditorGUI.ProgressBar(rect, fraction, label);
    }
}
