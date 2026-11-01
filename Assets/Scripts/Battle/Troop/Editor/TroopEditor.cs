using UnityEditor;
using UnityEngine;

// Troop Inspector: live morale (spec §5), formation state, engagement and melee/ranged statistics in Play mode.
[CustomEditor(typeof(Troop))]
public sealed class TroopEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying) return;

        var troop = (Troop)target;
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            EditorGUILayout.EnumPopup("Formation Mode", troop.FormationMode);
            EditorGUILayout.Toggle("Engaged", troop.IsEngaged);
            EditorGUILayout.FloatField("Assault (s)", troop.AssaultTimeRemaining);
            EditorGUILayout.Toggle("Chained", troop.IsChained);
            EditorGUILayout.Toggle("Chain Suppressed", troop.ChainSuppressed);
            EditorGUILayout.IntField("Move", troop.Move);
            EditorGUILayout.FloatField("L_nat", troop.NaturalLineLength);
            TroopMoraleEditor.DrawMorale(troop.MoraleState, troop);
        }

        DrawMeleeMetrics(troop);
        Repaint();
    }

    static void DrawMeleeMetrics(Troop troop)
    {
        MeleeMetrics.Summary s = troop.MeleeMetrics.Snapshot(troop.LivingSoldierCount);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Combat (last {s.Window:0} s)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Charges", s.Charges);
            EditorGUILayout.FloatField("Charges / soldier / window", s.ChargesPerSoldierPerWindow);
            EditorGUILayout.IntField("Followers joined", s.Followers);
            EditorGUILayout.IntField("Clashes", s.Clashes);
            EditorGUILayout.IntField("Duels won", s.DuelWins);
            EditorGUILayout.FloatField("Duel win rate (%)", s.DuelWinRate * 100f);
            EditorGUILayout.FloatField("Damage done", s.DamageDone);
            EditorGUILayout.FloatField("Damage received", s.DamageReceived);
            EditorGUILayout.IntField("Shots", s.Shots);
            EditorGUILayout.FloatField("Hit rate (%)", s.HitRate * 100f);
        }

        if (GUILayout.Button("Reset Combat Metrics"))
            troop.ResetMeleeMetrics();
    }
}
