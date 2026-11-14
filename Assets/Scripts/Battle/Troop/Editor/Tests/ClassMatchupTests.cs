using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Class match-ups with the project's real data: whole troops of the generic characters (Resources/Troop and Character
// Profiles/Characters/Generic) fighting on a copy of the project's Battle Tuning. Unlike a single clash, this counts the
// soldier numbers, frontage, morale pools and retreats. Seeded, so every run is the same. Explicit: run by name; each
// case prints its result in the test message.
[Category("Measurement")]
public sealed class ClassMatchupTests
{
    const float FrameTime = 1f / 30f;
    const float MaxSeconds = 600f;
    const string Generic = "Troop and Character Profiles/Characters/Generic/";

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
        Clear();
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
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
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
    }

    static List<CharacterProfile> Character(string name)
    {
        var character = Resources.Load<CharacterProfile>(Generic + name);
        Assert.IsNotNull(character, $"{name} character not found");
        return new List<CharacterProfile> { character };
    }

    void Clear()
    {
        TroopObjects.FlushPending();
        TestScene.Destroy(_created);
        _world = null;
    }

    void NewWorld(int seed)
    {
        Clear();
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(seed);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    Troop Spawn(IReadOnlyList<CharacterProfile> characters, string name, int team, Vector2 position, Vector2 facing)
    {
        var root = new GameObject(name);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg));
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, characters);
        return troop;
    }

    struct Fight
    {
        public float EngagedAt, EndedAt;
        public string Result;
        public int AttackerAlive, DefenderAlive, AttackerStart, DefenderStart, AttackerRetreats, DefenderRetreats;
        public bool AttackerWon => Result == "defender routed";
    }

    // The attacker attacks the defender head-on (Pursue, so Assault on contact) and keeps attacking after each retreat,
    // as a player would. The defender stands.
    Fight Run(int seed, string attackerName, string defenderName)
    {
        NewWorld(seed);
        Troop attacker = Spawn(Character(attackerName), attackerName, 0, new Vector2(-8f, 0f), Vector2.right);
        Troop defender = Spawn(Character(defenderName), defenderName, 1, new Vector2(8f, 0f), Vector2.left);
        var fight = new Fight { EngagedAt = -1f, EndedAt = -1f, Result = "timeout" };
        int frames = Mathf.CeilToInt(MaxSeconds / FrameTime);
        for (int i = 0; i < frames; i++)
        {
            KeepAttacking(attacker, defender);
            _world.SimulateFrame(FrameTime);
            float t = (i + 1) * FrameTime;
            if (i == 0)
            {
                fight.AttackerStart = attacker.LivingSoldierCount;
                fight.DefenderStart = defender.LivingSoldierCount;
            }
            bool attackerGone = attacker == null || attacker.IsRouted || attacker.LivingSoldierCount == 0;
            bool defenderGone = defender == null || defender.IsRouted || defender.LivingSoldierCount == 0;
            if (fight.EngagedAt < 0f && attacker != null && attacker.IsEngaged) fight.EngagedAt = t;
            if (attackerGone || defenderGone)
            {
                fight.EndedAt = t;
                fight.Result = attackerGone && defenderGone ? "both" : attackerGone ? "attacker routed" : "defender routed";
                break;
            }
        }
        if (fight.EndedAt < 0f) fight.EndedAt = MaxSeconds;
        if (fight.EngagedAt < 0f) fight.EngagedAt = fight.EndedAt;
        fight.AttackerAlive = attacker != null ? attacker.LivingSoldierCount : 0;
        fight.DefenderAlive = defender != null ? defender.LivingSoldierCount : 0;
        fight.AttackerRetreats = attacker != null ? attacker.MoraleState.RetreatCount : -1;
        fight.DefenderRetreats = defender != null ? defender.MoraleState.RetreatCount : -1;
        return fight;
    }

    static void KeepAttacking(Troop troop, Troop enemy)
    {
        if (troop == null || enemy == null || troop.IsRouted || troop.IsFaultSlipping || troop.IsPursuing) return;
        if (ChargeCoordinator.CanStartCharges(troop)) return;
        troop.Pursue(enemy);
    }

    string Report(string attackerName, string defenderName, int seeds)
    {
        var text = new StringBuilder();
        text.AppendLine($"MATCHUP {attackerName} attacks {defenderName} head-on");
        int wins = 0;
        float melee = 0f;
        for (int seed = 1; seed <= seeds; seed++)
        {
            Fight f = Run(seed * 7919, attackerName, defenderName);
            if (f.AttackerWon) wins++;
            melee += f.EndedAt - f.EngagedAt;
            text.AppendLine($"  seed {seed}: {f.Result} after {f.EndedAt - f.EngagedAt:F1}s of melee; alive " +
                $"{attackerName} {f.AttackerAlive}/{f.AttackerStart}, {defenderName} {f.DefenderAlive}/{f.DefenderStart}; " +
                $"retreats {f.AttackerRetreats} / {f.DefenderRetreats}");
        }
        text.AppendLine($"  {attackerName} won {wins}/{seeds}, average melee {melee / seeds:F1}s");
        return text.ToString();
    }

    void Measure(string attackerName, string defenderName, int seeds)
    {
        string report = Report(attackerName, defenderName, seeds);
        Debug.Log(report);
        Assert.Pass(report);
    }

    [Explicit("Measurement: run by name")]
    [TestCase("Cav", "Spearman")]
    [TestCase("Huskarl", "Spearman")]
    [TestCase("Cav", "Archer")]
    [TestCase("Skirmisher", "Archer")]
    public void HeadOn(string attacker, string defender) => Measure(attacker, defender, 8);

}
