using UnityEditor;
using UnityEngine;

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
            EditorGUILayout.FloatField("Shock", troop.Shock);
            EditorGUILayout.FloatField("Fault Pressure", troop.FaultPressure);
        }

        DrawMeleeMetrics(troop);
        Repaint();
    }

    static void DrawMeleeMetrics(Troop troop)
    {
        MeleeMetrics.Summary s = troop.MeleeMetrics.Snapshot(troop.LivingSoldierCount);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Melee (last {s.Window:0} s)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField("Attacks / s (troop)", s.AttacksPerSecond);
            EditorGUILayout.FloatField("Attacks / s (per soldier)", s.AttacksPerSecondPerSoldier);
            EditorGUILayout.FloatField("Hit Rate (%)", s.HitRate * 100f);
            EditorGUILayout.IntField("Clashes", s.Clashes);
            EditorGUILayout.IntField("Attacks", s.Attacks);
            EditorGUILayout.IntField("Hits", s.Hits);
            EditorGUILayout.FloatField("Damage Done", s.DamageDone);
            EditorGUILayout.FloatField("Damage Done / s", s.DamageDonePerSecond);
            EditorGUILayout.FloatField("Damage Received", s.DamageReceived);
            EditorGUILayout.FloatField("Damage Received / s", s.DamageReceivedPerSecond);
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Ready Attacks (now)", troop.ReadyMeleeAttacks);
            EditorGUILayout.IntField("Held Attacks (now)", troop.HeldMeleeAttacks);
        }

        if (GUILayout.Button("Reset Melee Metrics"))
            troop.ResetMeleeMetrics();
    }
}
