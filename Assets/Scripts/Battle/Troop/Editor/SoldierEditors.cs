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

// Troops > Check Weapon Skills (SU-140, Q-2): logs every character under Resources/Troop and Character Profiles that
// carries a weapon it has no skill in. A warning, not an error: such soldiers still deploy and fight with skill 0.
public static class WeaponSkillCheck
{
    const string ProfilesFolder = "Assets/Resources/Troop and Character Profiles";

    [MenuItem("Troops/Check Weapon Skills")]
    public static void CheckWeaponSkills()
    {
        int characters = 0, unskilled = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:CharacterProfile", new[] { ProfilesFolder }))
        {
            var character = AssetDatabase.LoadAssetAtPath<CharacterProfile>(AssetDatabase.GUIDToAssetPath(guid));
            if (character == null) continue;
            characters++;
            var weapons = character.UnskilledWeapons();
            if (weapons.Count > 0) unskilled++;
            foreach (WeaponProfile weapon in weapons)
                Debug.LogWarning(character.UnskilledWeaponWarning(weapon), character);
        }
        Debug.Log($"Check Weapon Skills: {unskilled} of {characters} characters carry a weapon they have no skill in.");
    }
}
