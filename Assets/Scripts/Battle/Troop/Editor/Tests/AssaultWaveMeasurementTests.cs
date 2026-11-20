using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// The Assault wave with the project's real data (Movie_025, SU-183): Prince Julian's cavalry pursues the generic Spearmen,
// head-on and corner-on, on a copy of the project's Battle Tuning, with Assault Predict Alignment off and on. Prints when
// each of Julian's soldiers made its first Assault charge, in seconds after the Assault opened ("--": not within 6 s).
// Explicit: run by name.
[Category("Measurement")]
public sealed class AssaultWaveMeasurementTests
{
    const float FrameTime = 1f / 30f;
    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        TroopTuning.OverrideForTests(null);
        TroopTuning project = TroopTuning.Active;
        _tuning = TroopTuning.CreateDefaults();
        for (int i = 0; i < project.Sections.Count; i++) EditorUtility.CopySerialized(project.Sections[i], _tuning.Sections[i]);
        TroopTuning.OverrideForTests(_tuning);
    }

    [TearDown]
    public void TearDown()
    {
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    Troop Spawn(CharacterProfile character, string name, int team, Vector2 position, Vector2 facing)
    {
        var root = new GameObject(name);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg));
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character });
        return troop;
    }

    void SetBool(string property, bool value)
    {
        foreach (var section in _tuning.Sections)
        {
            var serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null) continue;
            found.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TroopTuning.OverrideForTests(_tuning);
            return;
        }
        Assert.Fail(property);
    }

    string RunCase(bool predict, bool cornerOn)
    {
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        _created.Clear();
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(25);
        _world.Initialize();
        _created.Add(_world.gameObject);
        SetBool("_assaultPredictAlignment", predict);
        var julian = Resources.Load<CharacterProfile>("Troop and Character Profiles/Characters/Campaign/Mission 1/Julian");
        var spear = Resources.Load<CharacterProfile>("Troop and Character Profiles/Characters/Generic/Spearman");
        Troop defender = Spawn(spear, "Spearmen", 1, Vector2.zero, Vector2.down);
        Troop attacker = Spawn(julian, "Julian", 0, cornerOn ? new Vector2(9f, -12f) : new Vector2(0f, -14f), Vector2.up);
        attacker.Pursue(defender);
        var sb = new StringBuilder();
        sb.Append(cornerOn ? "corner-on, " : "head-on,   ").Append(predict ? "predicted: " : "old:       ");
        float opened = float.NaN;
        var first = new Dictionary<SoldierAgentState, float>();
        bool wheeled = false;
        for (int i = 0; i < 30 * 14; i++)
        {
            _world.SimulateFrame(FrameTime);
            float time = (i + 1) * FrameTime;
            if (attacker.IsAssaulting && float.IsNaN(opened)) opened = time;
            if (attacker.IsAssaultWheeling) wheeled = true;
            foreach (var s in attacker.Soldiers)
                if (s.IsAssaultCharging && !first.ContainsKey(s)) first[s] = time;
            if (!float.IsNaN(opened) && time - opened > 6f) break;
        }
        sb.Append($"opened {opened:F2} s, wheeled {wheeled} | ");
        int k = 0;
        foreach (var s in attacker.Soldiers)
        {
            sb.Append(first.TryGetValue(s, out float t) ? $"s{k}:{t - opened:F2} " : $"s{k}:-- ");
            k++;
        }
        return sb.ToString();
    }

    [Test, Explicit]
    public void JulianIntoSpearmen_FirstAssaultCharges()
    {
        var sb = new StringBuilder();
        foreach (bool corner in new[] { false, true })
        foreach (bool predict in new[] { false, true })
            sb.AppendLine(RunCase(predict, corner));
        TestContext.WriteLine(sb.ToString());
        Assert.Pass(sb.ToString());
    }
}
