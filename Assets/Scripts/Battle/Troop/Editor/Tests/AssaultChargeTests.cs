using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// The Assault (SU-165): a troop with an active order that becomes engaged has every soldier that may charge make one
// charge, without a roll, as it comes into reach; once per soldier per Assault. Edit mode scenarios on the spec
// defaults with the project's Formation distances (as AssaultWheelScenarioTests), stepped through SimulateFrame.
[Category("Scenario")]
public sealed class AssaultChargeTests
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
        _world.SetSeed(1650);
        _world.Initialize();
        _created.Add(_world.gameObject);
        UseTuning("_sDistance", 5f);
        UseTuning("_aDistance", 2f);
        UseTuning("_engagementGap", 1.5f);
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

    void UseTuning(string property, bool value)
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
        Assert.Fail($"No Battle Tuning section has the field {property}");
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
            TroopTuning.OverrideForTests(_tuning);
            return;
        }
        Assert.Fail($"No Battle Tuning section has the field {property}");
    }

    CharacterProfile Character(int soldiers, int aggression, bool lance = false, int maxHealth = 40, int morale = 10)
    {
        var rules = lance ? new[] { new WeaponSpecialRuleEntry(WeaponSpecialRule.CouchedLance) } : null;
        var weapon = MeleeWeaponProfile.CreateRuntimeProfile(lance ? "Test Lance" : "Test Sword",
            lance ? MeleeWeaponClass.Spear : MeleeWeaponClass.Sword, lance ? 2 : 0, 0, rules);
        _created.Add(weapon);
        var character = CharacterProfile.CreateRuntimeProfile("Test", null, null, null, soldiers, morale,
            CombatStatLimits.Default, new CharacterEquipment(weapon, null, null),
            new SoldierStats(5, 5, 5, 5, 5, aggression, maxHealth));
        _created.Add(character);
        return character;
    }

    // A troop standing in a placed line whose front rank is centred on lineCenter (SU-101, SU-161).
    Troop PlacedLine(string name, int team, Vector2 lineCenter, Vector2 facing, CharacterProfile character, int frontage)
    {
        var root = new GameObject(name);
        root.transform.position = lineCenter;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, frontage);
        troop.PlaceAt(lineCenter, facing, troop.NaturalLineLength, holdLine: true);
        return troop;
    }

    Troop Unplaced(string name, int team, Vector2 position, CharacterProfile character, int frontage)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        _created.Add(root);
        var troop = root.AddComponent<Troop>();
        troop.Configure(team, new List<CharacterProfile> { character }, frontage);
        return troop;
    }

    // Records every soldier's Assault charges: when each started (seconds from the start of the run).
    sealed class AssaultLog
    {
        readonly Dictionary<SoldierAgentState, List<float>> _starts = new();
        readonly HashSet<SoldierAgentState> _charging = new();
        public float OpenedAt = float.NaN;
        public int Opened;
        bool _wasOpen;

        public void Observe(Troop troop, float time)
        {
            if (troop.IsAssaulting && !_wasOpen)
            {
                Opened++;
                if (float.IsNaN(OpenedAt)) OpenedAt = time;
            }
            _wasOpen = troop.IsAssaulting;
            foreach (var soldier in troop.Soldiers)
            {
                if (!soldier.IsAssaultCharging) { _charging.Remove(soldier); continue; }
                if (!_charging.Add(soldier)) continue;
                if (!_starts.TryGetValue(soldier, out var list)) _starts[soldier] = list = new List<float>();
                list.Add(time);
            }
        }

        public int Chargers => _starts.Count;
        public int MostByOneSoldier
        {
            get
            {
                int most = 0;
                foreach (var list in _starts.Values) most = Mathf.Max(most, list.Count);
                return most;
            }
        }
        public int ChargedWithin(float seconds)
        {
            int count = 0;
            foreach (var list in _starts.Values) if (list[0] - OpenedAt <= seconds) count++;
            return count;
        }
        public float LatestFirstCharge
        {
            get
            {
                float latest = float.NegativeInfinity;
                foreach (var list in _starts.Values) latest = Mathf.Max(latest, list[0] - OpenedAt);
                return latest;
            }
        }
    }

    AssaultLog Run(Troop attacker, float seconds)
    {
        var log = new AssaultLog();
        int frames = Mathf.CeilToInt(seconds / FrameTime);
        for (int i = 0; i < frames && attacker != null; i++)
        {
            _world.SimulateFrame(FrameTime);
            log.Observe(attacker, (i + 1) * FrameTime);
        }
        return log;
    }

    // Two lines of the same width, head-on; the left one pursues the right one. Aggression 0, so every charge in the run
    // is an Assault charge.
    (Troop attacker, Troop defender) HeadOn(int soldiers, int frontage, int aggression = 0)
    {
        UseTuning("_feintsPerCharge", 0f);
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.left, Character(soldiers, 0), frontage);
        Troop attacker = PlacedLine("Attacker", 0, new Vector2(-6f, 0f), Vector2.right, Character(soldiers, aggression), frontage);
        attacker.Pursue(defender);
        return (attacker, defender);
    }

    // SU-165 without the chain reaction (SU-171 off).
    [Test]
    public void OrderedTroop_EngagingHeadOn_EveryFrontRankSoldierChargesAtOnce()
    {
        UseTuning("_assaultChainReaction", false);
        var (attacker, defender) = HeadOn(12, 6);
        AssaultLog log = Run(attacker, 6f);
        Assert.IsFalse(float.IsNaN(log.OpenedAt), "The pursuer's Assault opens on contact");
        int front = ChargeCoordinator.FrontRankSize(attacker);
        TestContext.WriteLine($"Assault opened at {log.OpenedAt:F2} s; {log.ChargedWithin(0.5f)} of {front} front-rank " +
            $"soldiers charged within 0.5 s, {log.Chargers} in all");
        Assert.GreaterOrEqual(log.ChargedWithin(0.5f), front, "The whole front rank charges in the first frames");
    }

    [Test]
    public void EachSoldier_MakesOneAssaultChargePerAssault()
    {
        var (attacker, _) = HeadOn(12, 6);
        AssaultLog log = Run(attacker, 10f);
        Assert.AreEqual(1, log.Opened, "One Assault in this fight");
        Assert.GreaterOrEqual(log.Chargers, ChargeCoordinator.FrontRankSize(attacker));
        Assert.AreEqual(1, log.MostByOneSoldier, "No soldier makes a second Assault charge");
    }

    // A single rank: once every soldier has made its charge the Assault closes, though the troops are still engaged.
    [Test]
    public void Assault_ClosesWhenEveryLivingSoldierHasCharged()
    {
        var (attacker, _) = HeadOn(6, 6);
        bool closedWhileEngaged = false;
        for (int i = 0; i < Mathf.CeilToInt(6f / FrameTime) && !closedWhileEngaged; i++)
        {
            _world.SimulateFrame(FrameTime);
            closedWhileEngaged = attacker.AssaultChargesMade > 0 && !attacker.IsAssaulting && attacker.IsEngaged;
        }
        Assert.IsTrue(closedWhileEngaged, "The Assault closes once all have charged");
        Assert.GreaterOrEqual(attacker.AssaultChargesMade, attacker.LivingSoldierCount);
    }

    // A pursuer that meets a line corner-on (SU-163) charges with the few soldiers in reach; the rest make their Assault
    // charge as the wheel brings them into reach.
    [Test]
    public void SoldiersComingIntoReachLater_MakeTheirAssaultChargeThen()
    {
        UseTuning("_assaultChainReaction", false);
        UseTuning("_feintsPerCharge", 0f);
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.left, Character(12, 0), 10);
        Vector2 heading = new(Mathf.Cos(-45f * Mathf.Deg2Rad), Mathf.Sin(-45f * Mathf.Deg2Rad));
        Vector2 tangent = new(-heading.y, heading.x);
        float length = LineGeometry.NaturalLength(6);
        Vector2 leadingEnd = new(-1.75f, 1.5f);
        Troop attacker = PlacedLine("Attacker", 0, leadingEnd - tangent * (length * 0.5f), heading, Character(8, 0), 6);
        attacker.Pursue(defender);

        AssaultLog log = Run(attacker, 8f);
        Assert.IsFalse(float.IsNaN(log.OpenedAt), "The pursuer's Assault opens on contact");
        TestContext.WriteLine($"{log.ChargedWithin(0.2f)} charged within 0.2 s, {log.Chargers} in all, the last " +
            $"{log.LatestFirstCharge:F2} s after the Assault opened");
        Assert.GreaterOrEqual(log.ChargedWithin(0.2f), 1, "The soldiers in reach at contact charge at once");
        Assert.Greater(log.Chargers, log.ChargedWithin(0.2f), "More soldiers charge as they come into reach");
        Assert.Greater(log.LatestFirstCharge, 0.5f, "Some charge well after contact");
        Assert.AreEqual(1, log.MostByOneSoldier, "Each once");
    }

    // SU-171: head-on, the chain reaction. One soldier (the spark) charges on contact; the rest of the front rank follow
    // once the troop has stood square for Assault Chain Settle Seconds, one after another outward from the spark.
    [Test]
    public void ChainReaction_HeadOn_OneSparkThenTheFrontRankInTurn()
    {
        UseTuning("_assaultOneWave", false);
        var (attacker, _) = HeadOn(12, 6);
        var firsts = new Dictionary<SoldierAgentState, (float time, Vector2 position)>();
        float opened = float.NaN;
        for (int i = 0; i < Mathf.CeilToInt(6f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            float time = (i + 1) * FrameTime;
            if (attacker.IsAssaulting && float.IsNaN(opened)) opened = time;
            foreach (var soldier in attacker.Soldiers)
                if (soldier.IsAssaultCharging && !firsts.ContainsKey(soldier))
                    firsts[soldier] = (time, attacker.WorldPosition(soldier));
        }
        Assert.IsFalse(float.IsNaN(opened), "The pursuer's Assault opens on contact");
        var ordered = new List<(float time, Vector2 position)>(firsts.Values);
        ordered.Sort((a, b) => a.time.CompareTo(b.time));
        Vector2 sparkAt = ordered[0].position;
        ordered.Sort((a, b) => a.time != b.time ? a.time.CompareTo(b.time)
            : Mathf.Abs(a.position.y - sparkAt.y).CompareTo(Mathf.Abs(b.position.y - sparkAt.y)));
        int front = ChargeCoordinator.FrontRankSize(attacker);
        float settle = TroopTuning.Active.AssaultChainSettleSeconds;
        TestContext.WriteLine($"opened {opened:F2} s; first charges at " +
            string.Join(", ", ordered.ConvertAll(c => $"{c.time - opened:F2}")));
        Assert.GreaterOrEqual(ordered.Count, front, "The whole front rank makes its Assault charge");
        Assert.Greater(ordered[1].time - ordered[0].time, settle - FrameTime * 1.5f,
            "Nobody else charges until the troop has stood square for the settle time");
        Assert.Greater(ordered[front - 1].time, ordered[1].time, "The rest charge one after another, not at once");
        Vector2 spark = ordered[0].position;
        Vector2 axis = Vector2.up; // the attacker's line runs along y
        for (int i = 2; i < front; i++)
            Assert.GreaterOrEqual(Mathf.Abs(Vector2.Dot(ordered[i].position - spark, axis)) + 0.6f,
                Mathf.Abs(Vector2.Dot(ordered[i - 1].position - spark, axis)), "The reaction spreads outward from the spark");
        Assert.Less(ordered[front - 1].time - opened, 3f, "The whole front rank has charged within 3 s");
    }

    // SU-171: corner-on, the soldiers in reach wait while the troop wheels: after the spark no Assault charge starts on a
    // frame the troop is wheeling (before the Assault Chain Max Wait).
    [Test]
    public void ChainReaction_WaitsForTheWheel()
    {
        UseTuning("_assaultOneWave", false);
        UseTuning("_feintsPerCharge", 0f);
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.left, Character(12, 0), 10);
        Vector2 heading = new(Mathf.Cos(-45f * Mathf.Deg2Rad), Mathf.Sin(-45f * Mathf.Deg2Rad));
        Vector2 tangent = new(-heading.y, heading.x);
        float length = LineGeometry.NaturalLength(6);
        Vector2 leadingEnd = new(-1.75f, 1.5f);
        Troop attacker = PlacedLine("Attacker", 0, leadingEnd - tangent * (length * 0.5f), heading, Character(8, 0), 6);
        attacker.Pursue(defender);

        var started = new HashSet<SoldierAgentState>();
        float sparkAt = float.NaN;
        bool wheeled = false;
        int duringWheel = 0;
        for (int i = 0; i < Mathf.CeilToInt(8f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            float time = (i + 1) * FrameTime;
            int fresh = 0;
            foreach (var soldier in attacker.Soldiers)
                if (soldier.IsAssaultCharging && started.Add(soldier)) fresh++;
            if (fresh > 0 && float.IsNaN(sparkAt))
            {
                sparkAt = time;
                Assert.AreEqual(1, fresh, "One spark");
                continue;
            }
            if (attacker.IsAssaultWheeling) wheeled = true;
            if (fresh > 0 && attacker.IsAssaultWheeling
                && time - sparkAt < TroopTuning.Active.AssaultChainMaxWaitSeconds) duringWheel += fresh;
        }
        TestContext.WriteLine($"spark at {sparkAt:F2} s, {started.Count} charged in all");
        Assert.IsFalse(float.IsNaN(sparkAt), "The spark charges");
        Assert.IsTrue(wheeled, "The attacker wheels");
        Assert.AreEqual(0, duringWheel, "No Assault charge while it wheels");
        Assert.Greater(started.Count, 1, "The rest charge once it is square");
    }

    // Records, for each soldier, the time of its first Assault charge (seconds from the start of the run), and whether the
    // troop was wheeling on that frame.
    Dictionary<SoldierAgentState, float> FirstAssaultCharges(Troop attacker, float seconds, out float opened,
        out int duringWheel)
    {
        var firsts = new Dictionary<SoldierAgentState, float>();
        opened = float.NaN;
        duringWheel = 0;
        for (int i = 0; i < Mathf.CeilToInt(seconds / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            float time = (i + 1) * FrameTime;
            if (attacker.IsAssaulting && float.IsNaN(opened)) opened = time;
            foreach (var soldier in attacker.Soldiers)
            {
                if (!soldier.IsAssaultCharging || firsts.ContainsKey(soldier)) continue;
                firsts[soldier] = time;
                if (attacker.IsAssaultWheeling && time - opened < TroopTuning.Active.AssaultChainMaxWaitSeconds)
                    duringWheel++;
            }
        }
        return firsts;
    }

    // SU-174, SU-175: head-on, the whole front rank makes its Assault charge together, on contact, without a wait.
    [Test]
    public void OneWave_HeadOn_TheWholeFrontRankChargesTogetherOnContact()
    {
        var (attacker, _) = HeadOn(12, 6);
        var firsts = FirstAssaultCharges(attacker, 6f, out float opened, out _);
        Assert.IsFalse(float.IsNaN(opened), "The pursuer's Assault opens on contact");
        var times = new List<float>(firsts.Values);
        times.Sort();
        int front = ChargeCoordinator.FrontRankSize(attacker);
        TestContext.WriteLine($"opened {opened:F2} s; first charges at " +
            string.Join(", ", times.ConvertAll(t => $"{t - opened:F2}")));
        Assert.GreaterOrEqual(times.Count, front, "The whole front rank makes its Assault charge");
        Assert.LessOrEqual(times[0] - opened, FrameTime * 1.5f, "No wait: the first charges go on contact");
        Assert.LessOrEqual(times[front - 1] - times[0], FrameTime * 1.5f, "The front rank goes together");
    }

    // Two troops engaged without orders (not placed in a line either, which counts as a Line order): charges are the
    // ordinary rolled ones; neither assaults.
    [Test]
    public void TroopsWithoutOrders_GetNoAssault()
    {
        UseTuning("_feintsPerCharge", 0f);
        Troop right = Unplaced("Right", 1, new Vector2(1.5f, 0f), Character(12, 10), 6);
        Troop left = Unplaced("Left", 0, new Vector2(-1.5f, 0f), Character(12, 10), 6);
        Assert.IsFalse(left.HasActiveOrder || right.HasActiveOrder);
        int ordinary = 0;
        bool engaged = false;
        for (int i = 0; i < Mathf.CeilToInt(8f / FrameTime); i++)
        {
            _world.SimulateFrame(FrameTime);
            if (left.IsEngaged) engaged = true;
            Assert.IsFalse(left.IsAssaulting || right.IsAssaulting, "No order, no Assault");
            foreach (var troop in new[] { left, right })
            foreach (var soldier in troop.Soldiers)
            {
                Assert.IsFalse(soldier.IsAssaultCharging, "No Assault charges");
                if (soldier.IsCharging) ordinary++;
            }
        }
        Assert.IsTrue(engaged, "The troops engage");
        Assert.Greater(ordinary, 0, "Ordinary charges still happen");
    }

    // Couched Lance (SU-148, SU-165): the lancers' Assault charges double their damage; their later ordinary charges in
    // the same melee do not.
    [Test]
    public void CouchedLance_DoublesOnTheAssaultCharge_NotOnALaterOrdinaryCharge()
    {
        Troop defender = PlacedLine("Defender", 1, Vector2.zero, Vector2.left, Character(12, 0, maxHealth: 60, morale: 20), 6);
        Troop attacker = PlacedLine("Lancers", 0, new Vector2(-6f, 0f), Vector2.right,
            Character(12, 12, lance: true, maxHealth: 60, morale: 20), 6);
        attacker.Pursue(defender);
        Run(attacker, 25f);

        int assaultHits = 0, assaultDoubled = 0, ordinaryHits = 0, ordinaryDoubled = 0;
        for (int i = 0; i < CombatLog.Count; i++)
        {
            CombatLogEntry entry = CombatLog.Get(i);
            if (entry.Kind != CombatLogKind.Clash || entry.Attacker.TeamId != 0 || !entry.AttackerWins) continue;
            if (!entry.AttackerPower.Charging || entry.Damage.Raw <= 0) continue;
            if (entry.AttackerPower.Assault) { assaultHits++; if (entry.Damage.LanceDoubled) assaultDoubled++; }
            else { ordinaryHits++; if (entry.Damage.LanceDoubled) ordinaryDoubled++; }
        }
        TestContext.WriteLine($"Assault-charge hits {assaultHits} ({assaultDoubled} doubled), ordinary-charge hits " +
            $"{ordinaryHits} ({ordinaryDoubled} doubled)");
        Assert.Greater(assaultHits, 0, "Some Assault charges hit");
        Assert.AreEqual(assaultHits, assaultDoubled, "Every Assault-charge hit doubles");
        Assert.Greater(ordinaryHits, 0, "Some later ordinary charges hit");
        Assert.AreEqual(0, ordinaryDoubled, "No ordinary charge doubles");
    }
}
