using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// How long a melee lasts with the project's real data: two troops of the Spearman character (Resources/Troop and
// Character Profiles/Characters/Generic/Spearman) on the project's Battle Tuning assets. Time is counted from the frame the
// troops first engage to the frame one of them routs (or dies). Seeded, so every run is the same.
// The target (SU-106 to SU-108) is about 3 minutes. SpearmenMelee_LastsAboutThreeMinutes guards it; the lever sweeps are
// explicit (run them by name) and print their results in the test message.
[Category("Measurement")]
public sealed class MeleeDurationTests
{
    const float FrameTime = 1f / 30f;
    const float MaxSeconds = 600f;
    const string SpearmanCharacter = "Troop and Character Profiles/Characters/Generic/Spearman";

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    // A copy of the project's Battle Tuning, so a variant can switch rules without touching the assets.
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

    void SetRule(string property, bool value)
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

    // The Spearman troop's characters with the melee weapon's damage changed by damageChange (a runtime copy; the assets
    // are untouched).
    List<CharacterProfile> Spearmen(int damageChange)
    {
        var spearman = Resources.Load<CharacterProfile>(SpearmanCharacter);
        Assert.IsNotNull(spearman, "Spearman character not found");
        if (damageChange == 0) return new List<CharacterProfile> { spearman };
        CharacterProfile character = Object.Instantiate(spearman);
        WeaponProfile weapon = Object.Instantiate(spearman.MeleeWeapon);
        _keep.Add(character);
        _keep.Add(weapon);
        var weaponData = new SerializedObject(weapon);
        weaponData.FindProperty("_damage").intValue += damageChange;
        weaponData.ApplyModifiedPropertiesWithoutUndo();
        var characterData = new SerializedObject(character);
        characterData.FindProperty("_equipment._meleeWeapon").objectReferenceValue = weapon;
        characterData.ApplyModifiedPropertiesWithoutUndo();
        return new List<CharacterProfile> { character };
    }

    readonly List<Object> _keep = new();

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

    public struct Fight
    {
        public float EngagedAt, EndedAt;
        public string Result;
        public int AttackerAlive, DefenderAlive, AttackerRetreats, DefenderRetreats, AttackerCharges, DefenderCharges;
        public float Duration => EndedAt - EngagedAt;
    }

    // The left troop attacks the right one (Pursue, so Assault on contact) and keeps attacking, as a player would: after
    // a retreat, or once the enemy has fallen back out of charge range, it is ordered to attack again. bothAttack: the
    // right troop does the same instead of standing.
    Fight Run(int seed, bool bothAttack, IReadOnlyList<CharacterProfile> characters)
    {
        NewWorld(seed);
        Troop left = Spawn(characters, "Left Spearmen", 0, new Vector2(-6f, 0f), Vector2.right);
        Troop right = Spawn(characters, "Right Spearmen", 1, new Vector2(6f, 0f), Vector2.left);
        int leftCharges = 0, rightCharges = 0;
        var leftCharging = new HashSet<SoldierAgentState>();
        var rightCharging = new HashSet<SoldierAgentState>();
        KeepAttacking(left, right);
        if (bothAttack) KeepAttacking(right, left);

        var fight = new Fight { EngagedAt = -1f, EndedAt = -1f, Result = "timeout" };
        int frames = Mathf.CeilToInt(MaxSeconds / FrameTime);
        for (int i = 0; i < frames; i++)
        {
            _world.SimulateFrame(FrameTime);
            KeepAttacking(left, right);
            if (bothAttack) KeepAttacking(right, left);
            float t = (i + 1) * FrameTime;
            bool leftGone = left == null || left.IsRouted || left.LivingSoldierCount == 0;
            bool rightGone = right == null || right.IsRouted || right.LivingSoldierCount == 0;
            if (fight.EngagedAt < 0f && left != null && left.IsEngaged) fight.EngagedAt = t;
            leftCharges += NewCharges(left, leftCharging);
            rightCharges += NewCharges(right, rightCharging);
            if (leftGone || rightGone)
            {
                fight.EndedAt = t;
                fight.Result = leftGone && rightGone ? "both" : leftGone ? "left routed" : "right routed";
                break;
            }
        }
        if (fight.EndedAt < 0f) fight.EndedAt = MaxSeconds;
        if (fight.EngagedAt < 0f) fight.EngagedAt = fight.EndedAt;
        fight.AttackerAlive = left != null ? left.LivingSoldierCount : 0;
        fight.DefenderAlive = right != null ? right.LivingSoldierCount : 0;
        fight.AttackerRetreats = left != null ? left.MoraleState.RetreatCount : -1;
        fight.DefenderRetreats = right != null ? right.MoraleState.RetreatCount : -1;
        fight.AttackerCharges = leftCharges;
        fight.DefenderCharges = rightCharges;
        return fight;
    }

    // Orders an attack when the troop is free (not routed, retreating or already attacking) and out of charge range.
    static void KeepAttacking(Troop troop, Troop enemy)
    {
        if (troop == null || enemy == null || troop.IsRouted || troop.IsFaultSlipping || troop.IsPursuing) return;
        if (ChargeCoordinator.CanStartCharges(troop)) return;
        troop.Pursue(enemy);
    }

    // Charges started this frame: soldiers charging now that were not charging last frame.
    static int NewCharges(Troop troop, HashSet<SoldierAgentState> charging)
    {
        if (troop == null) return 0;
        int started = 0;
        foreach (var soldier in troop.Soldiers)
        {
            if (soldier.IsCharging) { if (charging.Add(soldier)) started++; }
            else charging.Remove(soldier);
        }
        return started;
    }

    string Report(string title, bool bothAttack, int seeds, out float average, IReadOnlyList<CharacterProfile> characters)
    {
        var text = new StringBuilder();
        text.AppendLine($"MELEE DURATION {title}");
        float total = 0f;
        for (int seed = 1; seed <= seeds; seed++)
        {
            Fight f = Run(seed * 7919, bothAttack, characters);
            total += f.Duration;
            text.AppendLine($"  seed {seed}: engaged {f.EngagedAt:F1}s, ended {f.EndedAt:F1}s, melee {f.Duration:F1}s, {f.Result}; " +
                $"alive L{f.AttackerAlive} R{f.DefenderAlive}, retreats L{f.AttackerRetreats} R{f.DefenderRetreats}, " +
                $"charges L{f.AttackerCharges} R{f.DefenderCharges}");
        }
        average = total / seeds;
        text.AppendLine($"  average melee {average:F1}s ({average / 60f:F2} min)");
        return text.ToString();
    }

    // The project's data and tuning as they are: the melee should last minutes, not seconds (SU-108, SU-112). The range
    // is wide because a 4-seed average still moves by a minute with the random streams.
    [Test]
    public void SpearmenMelee_LastsAboutThreeMinutes()
    {
        List<CharacterProfile> spearmen = Spearmen(0);
        string report = Report("project data", false, 4, out float average, spearmen);
        Debug.Log(report);
        Assert.That(average, Is.InRange(120f, 300f), "2 to 5 minutes from engagement to rout\n" + report);
    }

    // Each lever on and off (front rank charges SU-106, stunned soldiers SU-107) with the weapon damage changed by
    // damageChange from the assets (SU-108 already took 3 off).
    [Explicit("Tuning sweep: run by name")]
    [TestCase(false, false, 0)]
    [TestCase(true, false, 0)]
    [TestCase(false, true, 0)]
    [TestCase(true, true, 0)]
    [TestCase(true, true, 3)]
    [TestCase(true, true, -1)]
    public void SpearmenMelee_Levers(bool frontRank, bool stun, int damageChange) =>
        Levers(frontRank, stun, damageChange, 8, false);

    // Weapon damage one higher than the assets (-2 instead of -3 from the original values), 45 s charge window.
    [Explicit("Tuning sweep: run by name")]
    [Test]
    public void SpearmenMelee_SweepDamagePlusOne() => Levers(true, true, 1, 6, false);

    // The assets' damage with a 40 s charge window instead of 45 s.
    [Explicit("Tuning sweep: run by name")]
    [Test]
    public void SpearmenMelee_SweepChargeWindow40()
    {
        SetFloat("_chargeWindowSeconds", 40f);
        Levers(true, true, 0, 6, false);
    }

    void SetFloat(string property, float value)
    {
        foreach (var section in _tuning.Sections)
        {
            var serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null) continue;
            found.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
    }

    [Explicit("Tuning sweep: run by name")]
    [TestCase(8, false)]
    [TestCase(4, true)]
    public void SpearmenMelee_AllLevers(int seeds, bool bothAttack) => Levers(true, true, 0, seeds, bothAttack);

    void Levers(bool frontRank, bool stun, int damageChange, int seeds, bool bothAttack)
    {
        SetRule("_frontRankChargesOnly", frontRank);
        SetRule("_stunAtZeroHp", stun);
        try
        {
            string title = $"front rank {(frontRank ? "on" : "off")}, stun {(stun ? "on" : "off")}, damage {damageChange:+0;-0;0}, " +
                $"charge window {TroopTuning.Active.ChargeWindowSeconds:0} s";
            if (bothAttack) title += ", both attack";
            string attack = Report(title, bothAttack, seeds, out float average, Spearmen(damageChange));
            Debug.Log(attack);
            Assert.Pass(attack);
        }
        finally
        {
            foreach (var o in _keep) if (o != null) Object.DestroyImmediate(o);
            _keep.Clear();
        }
    }
}
