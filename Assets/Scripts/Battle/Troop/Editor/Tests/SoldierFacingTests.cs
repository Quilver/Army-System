using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Soldier facing in melee (SoldierFacingAnimator, SU-158): soldiers of an engaged troop that have an enemy soldier within
// direct reach face the nearest one; the rest face the troop's front, whatever their velocity. A troop that is not
// engaged swings its soldiers up to 30° toward where they walk, as before (SU-154). Stepped in Edit mode like
// ChainAndRangedScenarioTests; the facing is then driven directly with the positions held still.
[Category("Scenario")]
public sealed class SoldierFacingTests
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
        // No charges: a charger faces its charge target instead, which these tests do not look at.
        UseTuning("_chargeWindowSeconds", 1e6f);
        _world = new GameObject("Test Combat World").AddComponent<TroopCombatWorld>();
        _world.SetSeed(777);
        _world.Initialize();
        _created.Add(_world.gameObject);
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

    void UseTuning(string property, float value)
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

    Troop CreateTroop(string name, int team, Vector2 position)
    {
        MeleeWeaponProfile melee = MeleeWeaponProfile.CreateRuntimeProfile("Test Sword", MeleeWeaponClass.Sword, 0, 0, null);
        _created.Add(melee);
        var profile = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, 12, 5, CombatStatLimits.Default,
            new CharacterEquipment(melee, null, null), new SoldierStats(5, 5, 5, 5, 5, 5, 15, null));
        _created.Add(profile);
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { profile }, 10);
        return troop;
    }

    void Simulate(float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames; i++) _world.SimulateFrame(FrameTime);
    }

    // Drives the facing of every living soldier of troop for the given time, positions held still.
    static void Face(Troop troop, Dictionary<SoldierAgentState, SoldierFacingTarget> targets, float seconds)
    {
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int f = 0; f < frames; f++)
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAlive) continue;
                if (!targets.TryGetValue(soldier, out var target)) targets[soldier] = target = new SoldierFacingTarget();
                SoldierFacingAnimator.Update(troop, soldier, target, FrameTime);
            }
    }

    // Distance from soldier to the nearest living soldier of enemy, and that soldier's position.
    static float Nearest(Troop troop, SoldierAgentState soldier, Troop enemy, out Vector2 nearest)
    {
        Vector2 position = troop.WorldPosition(soldier);
        float best = float.MaxValue;
        nearest = position;
        foreach (var other in enemy.Soldiers)
        {
            if (!other.IsAlive) continue;
            Vector2 at = enemy.WorldPosition(other);
            float distance = Vector2.Distance(position, at);
            if (distance >= best) continue;
            best = distance;
            nearest = at;
        }
        return best;
    }

    // A defender in line facing +x, attacked in the rear by a troop behind one end of it: the soldiers at that end are
    // within direct reach of the attackers, those at the far end are not.
    Troop RearAttacked(out Troop attacker)
    {
        Troop defender = CreateTroop("Defender", 0, Vector2.zero);
        defender.OrderLine(Vector2.zero, Vector2.right, defender.NaturalLineLength);
        attacker = CreateTroop("Attacker", 1, new Vector2(-6f, 4f));
        Simulate(1f);
        Assert.IsTrue(defender.IsEngaged, "the attacker is within A_Distance");
        Assert.IsTrue(defender.HasActiveLine);
        Assert.AreEqual(1, defender.Engagements.Count);
        Assert.AreEqual(TroopFace.Rear, defender.Engagements[0].Face, "attacked in the rear");
        return defender;
    }

    // Living soldiers of troop clearly within (or clearly beyond) direct reach of enemy.
    static List<SoldierAgentState> ByReach(Troop troop, Troop enemy, bool within)
    {
        float reach = SoldierFacingAnimator.EngagedReach(TroopTuning.Active);
        var result = new List<SoldierAgentState>();
        foreach (var soldier in troop.Soldiers)
        {
            if (!soldier.IsAlive) continue;
            float distance = Nearest(troop, soldier, enemy, out _);
            if (within ? distance < reach - 0.1f : distance > reach + 0.1f) result.Add(soldier);
        }
        return result;
    }

    // SU-158 (a): soldiers within direct reach of the attackers turn to the nearest one; the rest keep the troop's front.
    [Test]
    public void EngagedOnItsRear_SoldiersWithinReachFaceTheAttackers_OthersKeepTheFront()
    {
        Troop defender = RearAttacked(out Troop attacker);
        List<SoldierAgentState> within = ByReach(defender, attacker, true);
        List<SoldierAgentState> beyond = ByReach(defender, attacker, false);
        Assert.IsNotEmpty(within, "some soldiers are within reach");
        Assert.IsNotEmpty(beyond, "some soldiers are out of reach");

        Face(defender, new Dictionary<SoldierAgentState, SoldierFacingTarget>(), 1f);

        Vector2 front = defender.Formation.Facing.normalized;
        foreach (var soldier in within)
        {
            Nearest(defender, soldier, attacker, out Vector2 enemy);
            Vector2 toEnemy = enemy - defender.WorldPosition(soldier);
            Assert.Less(Vector2.Angle(soldier.Facing, toEnemy), 2f, "faces its nearest attacker");
            Assert.Greater(Vector2.Angle(soldier.Facing, front), 90f, "turned toward the rear");
        }
        foreach (var soldier in beyond)
            Assert.Less(Vector2.Angle(soldier.Facing, front), 0.5f, "out of reach: keeps the troop's front");
    }

    // SU-158 (b): an engaged soldier's facing does not swing with its velocity, whether or not it has an enemy to face.
    [Test]
    public void Engaged_FacingDoesNotChangeWhenVelocityFlips()
    {
        Troop defender = RearAttacked(out Troop attacker);
        SoldierAgentState near = ByReach(defender, attacker, true)[0];
        SoldierAgentState far = ByReach(defender, attacker, false)[0];
        var targets = new Dictionary<SoldierAgentState, SoldierFacingTarget>();

        near.Velocity = far.Velocity = new Vector2(0f, 3f);
        Face(defender, targets, 1f);
        Vector2 nearFacing = near.Facing;
        Vector2 farFacing = far.Facing;

        near.Velocity = far.Velocity = new Vector2(0f, -3f);
        Face(defender, targets, 0.5f);
        Assert.Less(Vector2.Angle(nearFacing, near.Facing), 0.5f, "facing its enemy, not its velocity");
        Assert.Less(Vector2.Angle(farFacing, far.Facing), 0.5f, "facing the front, not its velocity");
        Assert.Less(Vector2.Angle(defender.Formation.Facing, far.Facing), 0.5f);
    }

    // SU-158: a charger faces its charge target, even one well out of direct reach.
    [Test]
    public void Charger_FacesItsChargeTarget()
    {
        Troop defender = RearAttacked(out Troop attacker);
        SoldierAgentState charger = ByReach(defender, attacker, false)[0];
        SoldierAgentState target = null;
        float furthest = 0f;
        foreach (var enemy in attacker.Soldiers)
        {
            float distance = Vector2.Distance(defender.WorldPosition(charger), attacker.WorldPosition(enemy));
            if (distance <= furthest) continue;
            furthest = distance;
            target = enemy;
        }
        charger.BeginCharge(target, attacker, 3f, 0f);

        Face(defender, new Dictionary<SoldierAgentState, SoldierFacingTarget>(), 1f);

        Vector2 toTarget = attacker.WorldPosition(target) - defender.WorldPosition(charger);
        Assert.Less(Vector2.Angle(charger.Facing, toTarget), 1f);
    }

    // SU-158 (c): a troop that is not engaged behaves as before (SU-154): in a line its soldiers face the front, swung
    // up to 30° toward where they walk.
    [Test]
    public void Unengaged_LineSoldiersStillSwingTowardTheirVelocity()
    {
        Troop troop = CreateTroop("Lone", 0, Vector2.zero);
        troop.OrderLine(Vector2.zero, Vector2.right, troop.NaturalLineLength);
        Simulate(1f);
        Assert.IsFalse(troop.IsEngaged);
        Assert.IsTrue(troop.HasActiveLine);
        Vector2 front = troop.Formation.Facing.normalized;
        SoldierAgentState soldier = troop.Soldiers[0];
        var targets = new Dictionary<SoldierAgentState, SoldierFacingTarget>();

        foreach (float sign in new[] { 1f, -1f })
        {
            soldier.Velocity = new Vector2(0f, 3f * sign);
            Face(troop, targets, 1f);
            float expected = Mathf.Clamp(Vector2.SignedAngle(front, troop.WorldVelocity(soldier)), -30f, 30f);
            Assert.AreEqual(expected, Vector2.SignedAngle(front, soldier.Facing), 0.5f);
            Assert.AreEqual(30f, Mathf.Abs(expected), 0.01f, "swung the full 30° toward its walk");
        }
    }
}
