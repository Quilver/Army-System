using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Feints (SU-164): a charge that ends on contact without a duel, rolled at FeintsPerCharge x the charge rate on a random
// stream of its own. Edit mode scenarios on the spec defaults, stepped through TroopCombatWorld.SimulateFrame.
[Category("Scenario")]
public sealed class FeintTests
{
    const float FrameTime = 1f / 30f;

    readonly List<Object> _created = new();
    TroopCombatWorld _world;
    TroopTuning _tuning;

    [SetUp]
    public void SetUp()
    {
        _tuning = TroopTuning.CreateDefaults();
        TroopTuning.OverrideForTests(_tuning);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(12345);
        _world.Initialize();
        _created.Add(_world.gameObject);
    }

    [TearDown]
    public void TearDown()
    {
        TestScene.Destroy(_created);
        TroopTuning.OverrideForTests(null);
        _tuning?.DestroySections();
        _tuning = null;
    }

    // Sets a float tunable; false when no section has the field.
    bool SetFloat(string property, float value)
    {
        foreach (var section in _tuning.Sections)
        {
            var serialized = new SerializedObject(section);
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null) continue;
            found.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
        return false;
    }

    CharacterProfile Character(int aggression = 5, int maxHealth = 15, int morale = 5, int discipline = CombatStatLimits.Default,
        RangedWeaponProfile ranged = null, int numbers = 12)
    {
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(weapon);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, numbers, morale, discipline,
            new CharacterEquipment(weapon, ranged, null), new SoldierStats(5, 5, 5, 5, 5, aggression, maxHealth));
        _created.Add(profile);
        return profile;
    }

    Troop CreateTroop(string name, int team, Vector2 position, CharacterProfile character, int frontage = 6,
        int siblingIndex = -1)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        if (siblingIndex >= 0) root.transform.SetSiblingIndex(siblingIndex);
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, frontage);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    static string Fingerprint(Troop troop)
    {
        if (troop == null) return "gone";
        MeleeMetrics.Summary summary = troop.MeleeMetrics.Snapshot(troop.LivingSoldierCount);
        int hp = 0;
        foreach (var soldier in troop.Soldiers) hp += soldier.IsAlive ? soldier.CurrentHp : 0;
        return $"alive {troop.LivingSoldierCount}, hp {hp}, charges {summary.Charges}, followers {summary.Followers}, " +
            $"clashes {summary.Clashes}, duels won {summary.DuelWins}, damage {summary.DamageDone:0}, shock {troop.Shock:0.###}";
    }

    // With FeintsPerCharge 0 a seeded melee between two troops without orders (so no Assault) is exactly what it was
    // before feints existed. The expected text was recorded with the code before SU-164, so the later daze (SU-182) is
    // switched off too.
    [Test]
    public void FeintsOff_SeededMelee_MatchesThePreFeintResult()
    {
        SetFloat("_feintsPerCharge", 0f);
        SetFloat("_dazedSeconds", 0f);
        CharacterProfile keen = Character(aggression: 8, maxHealth: 30, morale: 12);
        // A troop's random streams come from its place in the hierarchy, so each is put in a fixed place before it is
        // configured: objects other tests leave in the scene cannot change its streams.
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), keen, siblingIndex: 1);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), keen, siblingIndex: 14);
        Assert.AreEqual(1, left.transform.GetSiblingIndex());
        Assert.AreEqual(14, right.transform.GetSiblingIndex());
        Simulate(30f);
        string actual = "L " + Fingerprint(left) + " | R " + Fingerprint(right);
        Debug.Log("FEINTS OFF FINGERPRINT " + actual);
        Assert.AreEqual(PreFeintFingerprint, actual);
    }

    const string PreFeintFingerprint =
        "L alive 12, hp 339, charges 19, followers 40, clashes 26, duels won 21, damage 199, shock 7.896 | " +
        "R alive 11, hp 217, charges 20, followers 48, clashes 26, duels won 5, damage 31, shock 62.522";

    // Over a long melee the troops start about FeintsPerCharge (3) feints for every real charge.
    [Test]
    public void Feints_StartAtAboutThreeTimesTheChargeRate()
    {
        CharacterProfile tough = Character(aggression: 4, maxHealth: 60, morale: 20, numbers: 20);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), tough, frontage: 10);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), tough, frontage: 10);
        Simulate(0.2f);
        Assert.IsTrue(left.IsEngaged && right.IsEngaged);
        left.ResetMeleeMetrics();
        right.ResetMeleeMetrics();
        Simulate(180f);
        int charges = left.MeleeMetrics.TotalCharges + right.MeleeMetrics.TotalCharges;
        int feints = left.MeleeMetrics.TotalFeintsStarted + right.MeleeMetrics.TotalFeintsStarted;
        Debug.Log($"Feints {feints}, charges {charges}, ratio {(float)feints / charges:0.00}");
        Assert.Greater(charges, 30);
        Assert.AreEqual(TroopTuning.Active.FeintsPerCharge, (float)feints / charges, 0.6f);
        Assert.Greater(left.MeleeMetrics.TotalFeints + right.MeleeMetrics.TotalFeints, 0, "Feints reach contact");
    }

    // A feint that reaches its target: both soldiers recoil and the feint ends, with no duel, damage, Shock or clash
    // cooldown. It is logged as "Parried" and raises SoldierFeinted.
    [Test]
    public void Feint_OnContact_EndsWithoutDuelDamageShockOrCooldown()
    {
        // One soldier a side, so nothing else is in contact when the feint lands.
        CharacterProfile calm = Character(aggression: 0, maxHealth: 30, morale: 20, numbers: 1);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), calm);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), calm);
        Simulate(0.5f);

        SoldierAgentState feinter = Foremost(left, 1f);
        SoldierAgentState target = Foremost(right, -1f);
        int hpBefore = TotalHp(left) + TotalHp(right);
        float shockLeft = left.Shock, shockRight = right.Shock;
        int logged = CombatLog.TotalRecorded;
        int raised = 0;
        void OnFeint(Troop a, Troop b, Vector2 p) => raised++;
        BattleEvents.SoldierFeinted += OnFeint;
        try
        {
            feinter.BeginFeint(target, right, 5f, 0f);
            Assert.IsTrue(feinter.IsCharging && feinter.IsFeinting);
            for (int i = 0; i < 150 && left.MeleeMetrics.TotalFeints == 0; i++) _world.SimulateFrame(FrameTime);
        }
        finally
        {
            BattleEvents.SoldierFeinted -= OnFeint;
        }

        Assert.AreEqual(1, left.MeleeMetrics.TotalFeints, "The feint reached contact");
        Assert.AreEqual(1, right.MeleeMetrics.TotalFeints);
        Assert.AreEqual(0, left.MeleeMetrics.TotalClashes + right.MeleeMetrics.TotalClashes, "No duel");
        Assert.IsFalse(feinter.IsCharging, "The feint has ended");
        Assert.IsFalse(feinter.IsReturning, "A feint leaves no rush back");
        Assert.AreEqual(hpBefore, TotalHp(left) + TotalHp(right), "No damage");
        Assert.AreEqual(shockLeft, left.Shock, 1e-4f, "No Shock");
        Assert.AreEqual(shockRight, right.Shock, 1e-4f, "No Shock");
        foreach (var soldier in left.Soldiers) Assert.AreEqual(0f, soldier.ClashCooldownRemaining, "No clash cooldown");
        foreach (var soldier in right.Soldiers) Assert.AreEqual(0f, soldier.ClashCooldownRemaining, "No clash cooldown");
        foreach (var soldier in right.Soldiers) Assert.IsFalse(soldier.IsStunned, "No stun");
        Assert.AreEqual(1, raised, "SoldierFeinted raised once");
        Assert.AreEqual(logged + 1, CombatLog.TotalRecorded, "One log entry");
        CombatLogEntry entry = CombatLog.Get(CombatLog.Count - 1);
        Assert.AreEqual(CombatLogKind.Feint, entry.Kind);
        Assert.AreEqual(feinter.Id, entry.Attacker.SoldierId);
        StringAssert.Contains("Parried", CombatLog.Format(entry));
    }

    // A feint takes no followers: over a melee with every follower chance at 1, no soldier ever follows a feinting one,
    // while real charges still take followers.
    [Test]
    public void Feints_TakeNoFollowers()
    {
        SetFloat("_chargeFollowProbability", 1f);
        CharacterProfile tough = Character(aggression: 10, maxHealth: 40, morale: 20, discipline: 10);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), tough);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), tough);
        Simulate(0.5f);
        left.ResetMeleeMetrics();
        right.ResetMeleeMetrics();

        int followersOfFeints = 0;
        for (int i = 0; i < Mathf.CeilToInt(30f / FrameTime) && left != null && right != null; i++)
        {
            _world.SimulateFrame(FrameTime);
            foreach (var troop in new[] { left, right })
            foreach (var soldier in troop.Soldiers)
                if (soldier.IsFollowing && soldier.ChargeLeader.IsFeinting) followersOfFeints++;
        }

        Assert.Greater(left.MeleeMetrics.TotalFeintsStarted + right.MeleeMetrics.TotalFeintsStarted, 5, "Feints happen");
        Assert.Greater(left.MeleeMetrics.Snapshot(left.LivingSoldierCount).Followers
            + right.MeleeMetrics.Snapshot(right.LivingSoldierCount).Followers, 0, "Real charges still take followers");
        Assert.AreEqual(0, followersOfFeints, "Nobody follows a feint");
    }

    // A feint by a soldier with a Fire & Engage bow fires no volley and spends no ammunition: those volleys come with a
    // new engagement or a retreat (SU-90, SU-151), never with a charge or a feint.
    [Test]
    public void Feint_SpendsNoAmmunition()
    {
        SetFloat("_chargeFollowProbability", 1f);
        var bow = RangedWeaponProfile.CreateRuntimeProfile("Test Bow", RangedWeaponClass.Bow, 1, 20f, 2, 5,
            new[] { new WeaponSpecialRuleEntry(WeaponSpecialRule.FireAndEngage) });
        _created.Add(bow);
        CharacterProfile archers = Character(aggression: 0, maxHealth: 30, morale: 20, discipline: 10, ranged: bow, numbers: 3);
        CharacterProfile calm = Character(aggression: 0, maxHealth: 30, morale: 20, numbers: 1);
        Troop left = CreateTroop("Left", 0, new Vector2(-2f, 0f), archers);
        Troop right = CreateTroop("Right", 1, new Vector2(2f, 0f), calm);
        Simulate(0.5f);
        var ammunition = new Dictionary<SoldierAgentState, int>();
        foreach (var soldier in left.Soldiers) ammunition[soldier] = soldier.AmmunitionLeft;
        int volleys = left.RangedFire.Volleys;

        SoldierAgentState feinter = Foremost(left, 1f);
        feinter.BeginFeint(Foremost(right, -1f), right, 5f, 0f);
        int followers = 0;
        for (int i = 0; i < 150 && left.MeleeMetrics.TotalFeints == 0; i++)
        {
            _world.SimulateFrame(FrameTime);
            foreach (var soldier in left.Soldiers) if (soldier.IsFollowing) followers++;
        }

        Assert.AreEqual(1, left.MeleeMetrics.TotalFeints, "The feint reached contact");
        Assert.AreEqual(0, followers, "Nobody followed the feint");
        Assert.AreEqual(volleys, left.RangedFire.Volleys, "No volley");
        foreach (var pair in ammunition)
            Assert.AreEqual(pair.Value, pair.Key.AmmunitionLeft, "No ammunition spent");
    }

    static SoldierAgentState Foremost(Troop troop, float direction)
    {
        SoldierAgentState best = null;
        float bestX = float.NegativeInfinity;
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            float x = troop.WorldPosition(soldier).x * direction;
            if (x <= bestX) continue;
            bestX = x;
            best = soldier;
        }
        return best;
    }

    static int TotalHp(Troop troop)
    {
        int hp = 0;
        foreach (var soldier in troop.Soldiers) hp += soldier.IsAlive ? soldier.CurrentHp : 0;
        return hp;
    }
}
