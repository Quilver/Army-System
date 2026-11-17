using System.Collections.Generic;
using LeveragedWars.Battle.AI;
using NUnit.Framework;
using UnityEngine;

// Chain Line (AI vs AI level): the spearmen stand end to end in one chain, linked by order links, advance in bounds that
// wait for the chain to stand formed, and fight the opponent in front of each troop. Driven with small fakes of the AI
// interfaces, no real troops: a fake keeps its line order and its links the way the troop system would report them.
[Category("AI")]
public sealed class ChainLineTests
{
    const float Width = 5.4f;   // a 16-soldier troop's natural line length
    const float Tolerance = 0.01f;

    sealed class FakeTroop : IAITroop, IAIChainTroop
    {
        static int nextId;

        public FakeTroop(AITeam team, Vector3 position)
        {
            Id = ++nextId;
            Team = team;
            Position = position;
        }

        public int Id { get; }
        public string DisplayName => $"Fake {Id}";
        public AITeam Team { get; }
        public bool IsAlive { get; set; } = true;
        public bool IsRouting => false;
        public Vector3 Position { get; set; }
        public Vector3 Forward => Vector3.up;
        public Vector3 Velocity => Vector3.zero;
        public float Frontage => Width;
        public float RunSpeed => 4f;
        public bool IsRanged => false;
        public float MaxRange => 0f;
        public bool HasAmmo => false;
        public bool IsInMelee { get; set; }
        public IAITroop MeleeOpponent { get; set; }

        public IAITroop Attacking;
        public int LinksMade;
        public readonly Dictionary<IAIChainTroop, AIChainLink> RightLinks = new Dictionary<IAIChainTroop, AIChainLink>();

        public void OrderHold(Vector3 position, Vector3 facing)
        {
            HasLineOrder = true;
            Attacking = null;
        }

        public void OrderMove(Vector3 position, Vector3 facing, bool run) => HasLineOrder = false;
        public void OrderAttack(IAITroop target) { HasLineOrder = false; Attacking = target; }
        public void OrderCharge(IAITroop target) => OrderAttack(target);
        public void OrderFallBack(Vector3 position) => HasLineOrder = false;

        public float NaturalLineLength => Width;
        public bool HasLineOrder { get; private set; }

        public AIChainLink RightLinkTo(IAIChainTroop other) =>
            RightLinks.TryGetValue(other, out AIChainLink link) ? link : AIChainLink.None;

        public bool LinkRightTo(IAIChainTroop other)
        {
            if (!HasLineOrder || !other.HasLineOrder) return false;
            RightLinks[other] = AIChainLink.Pending;
            LinksMade++;
            return true;
        }
    }

    sealed class FakeBattlefield : IAIBattlefield
    {
        public readonly List<IAITroop> All = new List<IAITroop>();
        public IReadOnlyList<IAITroop> Troops => All;
        public IReadOnlyList<IAIObjective> Objectives => new List<IAIObjective>();
    }

    FakeBattlefield field;
    AIBlackboard blackboard;
    Detachment detachment;
    ChainLineFormation chain;
    readonly List<FakeTroop> spears = new List<FakeTroop>();
    readonly List<FakeTroop> enemies = new List<FakeTroop>();
    readonly TroopReflexSettings reflexes = new TroopReflexSettings();
    float time;

    // Four spearmen in a rough line centred on (0, 0.5) (given out of order), and the enemy.
    ChainLineDefinition SetUp(params Vector3[] enemyPositions)
    {
        spears.Clear();
        enemies.Clear();
        field = new FakeBattlefield();
        foreach (float x in new[] { 3f, -8f, 8f, -3f })
            spears.Add(new FakeTroop(AITeam.Enemy, new Vector3(x, 0.5f, 0f)));
        foreach (Vector3 p in enemyPositions) AddEnemy(p);
        field.All.AddRange(spears);

        var def = new ChainLineDefinition();
        time = 0f;
        blackboard = new AIBlackboard(AITeam.Enemy, time);
        blackboard.Refresh(field, time);
        detachment = new Detachment("Spears", AITeam.Enemy, 0.5f);
        foreach (FakeTroop t in spears) detachment.AddAgent(blackboard.GetOrCreateAgent(t));
        blackboard.AddDetachment(detachment);
        detachment.CaptureStart();
        detachment.SetFormation(def, blackboard);
        chain = (ChainLineFormation)detachment.Formation;
        return def;
    }

    // One formation tick, then the agents issue what was asked (as AIDirector does).
    void Tick(float seconds = 0.25f)
    {
        time += seconds;
        blackboard.Refresh(field, time);
        detachment.Tick();
        foreach (AITroopAgent a in blackboard.Agents) a.Tick(blackboard, reflexes);
    }

    FakeTroop AddEnemy(Vector3 position)
    {
        var enemy = new FakeTroop(AITeam.Player, position);
        enemies.Add(enemy);
        field.All.Add(enemy);
        return enemy;
    }

    // Every troop walks to where it was told, and every link the formation made comes into force.
    void StandFormed()
    {
        for (int i = 0; i < chain.ChainOrder.Count; i++)
            ((FakeTroop)chain.ChainOrder[i].Troop).Position = chain.Slots[i];
        foreach (FakeTroop t in spears)
            foreach (IAIChainTroop other in new List<IAIChainTroop>(t.RightLinks.Keys))
                t.RightLinks[other] = AIChainLink.Linked;
    }

    [Test]
    public void FormsEndToEnd_LeftToRight_FacingTheEnemy()
    {
        SetUp(new Vector3(0f, 30f, 0f));
        Tick();

        Assert.AreEqual(4, chain.ChainOrder.Count);
        Assert.AreEqual(0f, Vector3.Angle(Vector3.up, chain.LineFacing), 0.5f, "faces the enemy");
        float[] expectedX = { -1.5f * Width, -0.5f * Width, 0.5f * Width, 1.5f * Width };
        float centreX = chain.LineCentre.x;
        for (int i = 0; i < 4; i++)
        {
            AITroopAgent a = chain.ChainOrder[i];
            Assert.AreEqual(AIOrderKind.Line, a.Requested.Kind, "holds a place in the chain");
            Assert.AreEqual(centreX + expectedX[i], a.Requested.Position.x, Tolerance, $"place {i} end to end");
            Assert.AreEqual(chain.LineCentre.y, a.Requested.Position.y, Tolerance, $"place {i} on the line");
        }
        for (int i = 0; i + 1 < 4; i++)
            Assert.Less(chain.ChainOrder[i].Position.x, chain.ChainOrder[i + 1].Position.x, "kept left to right as they stood");
    }

    [Test]
    public void LinksEachPairOfNeighbours_OnceBothHaveTheirLineOrders()
    {
        SetUp(new Vector3(0f, 30f, 0f));
        Tick(); // the formation asks; the agents issue the line orders after it
        int before = 0;
        foreach (FakeTroop t in spears) before += t.LinksMade;
        Assert.AreEqual(0, before, "no link before the line orders are out");

        Tick();
        for (int i = 0; i + 1 < 4; i++)
        {
            var left = (IAIChainTroop)chain.ChainOrder[i].Troop;
            var right = (IAIChainTroop)chain.ChainOrder[i + 1].Troop;
            Assert.AreEqual(AIChainLink.Pending, left.RightLinkTo(right), $"troop {i} right wing to troop {i + 1}");
        }
        Assert.AreEqual(AIChainLink.None, ((IAIChainTroop)chain.ChainOrder[3].Troop).RightLinkTo((IAIChainTroop)chain.ChainOrder[0].Troop),
            "the ends stay free");
    }

    [Test]
    public void AdvancesOneBound_OnlyOnceFormedAndLinked()
    {
        ChainLineDefinition def = SetUp(new Vector3(0f, 40f, 0f));
        Tick();
        Tick();
        Vector3 start = chain.LineCentre;

        // In place, but the links are still pending: no bound.
        for (int i = 0; i < 4; i++) ((FakeTroop)chain.ChainOrder[i].Troop).Position = chain.Slots[i];
        Tick();
        Assert.AreEqual(start.y, chain.LineCentre.y, Tolerance, "waits for the links");

        StandFormed();
        Tick();
        Assert.AreEqual(start.y + def.boundDistance, chain.LineCentre.y, Tolerance, "one bound towards the enemy");
        Assert.AreEqual(ChainLineFormation.ChainPhase.Advancing, chain.Phase);
        foreach (AITroopAgent a in chain.ChainOrder)
            Assert.AreEqual(chain.LineCentre.y, a.Requested.Position.y, Tolerance, "the whole line moves");
    }

    [Test]
    public void WaitsForAStraggler_UntilTheTimeout()
    {
        ChainLineDefinition def = SetUp(new Vector3(0f, 40f, 0f));
        Tick();
        Tick();
        StandFormed();
        Vector3 start = chain.LineCentre;
        ((FakeTroop)chain.ChainOrder[2].Troop).Position += new Vector3(0f, -6f, 0f); // held up

        Tick();
        Assert.AreEqual(start.y, chain.LineCentre.y, Tolerance, "waits for the straggler");
        Tick(def.waitTimeout);
        Assert.Greater(chain.LineCentre.y, start.y + 1f, "goes on without it after the timeout");
    }

    [Test]
    public void StopsShortOfTheTarget()
    {
        // The target is Stop Distance + 3 ahead of the line.
        ChainLineDefinition def = SetUp(new Vector3(0f, 0.5f + new ChainLineDefinition().stopDistance + 3f, 0f));
        def.attackRange = 2f; // nobody in reach: only the bounds are tested
        Tick();
        Tick();
        StandFormed();
        Vector3 start = chain.LineCentre;
        Tick();
        Assert.AreEqual(3f, chain.LineCentre.y - start.y, 0.05f, "only up to Stop Distance short of the target");
        StandFormed();
        Tick();
        Assert.AreEqual(3f, chain.LineCentre.y - start.y, 0.05f, "and no further");
    }

    [Test]
    public void EachTroopAttacksTheOpponentInFront_TheRestHoldTheirPlaces()
    {
        // The chain forms facing an enemy far to the north; then one opponent comes up in front of the second troop from
        // the left (at x = -3) and one close to the first troop but out to its side.
        SetUp(new Vector3(0f, 200f, 0f));
        Tick();
        FakeTroop inFront = AddEnemy(new Vector3(-3f, 7f, 0f));
        AddEnemy(new Vector3(-15f, 1.5f, 0f));
        Tick();

        AITroopAgent second = chain.ChainOrder[1];
        Assert.AreEqual(AIOrderKind.Attack, second.Requested.Kind, "attacks the opponent in front");
        Assert.AreSame(inFront, second.Requested.Target);
        Assert.AreEqual(ChainLineFormation.ChainPhase.Fighting, chain.Phase);
        Assert.AreEqual(DetachmentState.Engaged, detachment.State);
        Assert.AreEqual(AIOrderKind.Line, chain.ChainOrder[0].Requested.Kind, "does not bend round to the opponent at its side");
        foreach (int i in new[] { 2, 3 })
            Assert.AreEqual(AIOrderKind.Line, chain.ChainOrder[i].Requested.Kind, $"troop {i} holds its place");
    }

    [Test]
    public void StandsItsGroundInMeleeWithTheOpponentInFront_StillLinked()
    {
        SetUp(new Vector3(0f, 200f, 0f));
        Tick();
        Tick(); // line orders out, links made
        AITroopAgent second = chain.ChainOrder[1];
        var troop = (FakeTroop)second.Troop;
        Vector3 metAt = troop.Position + new Vector3(0f, 3f, 0f); // it closed in on the enemy
        troop.Position = metAt;
        FakeTroop inFront = AddEnemy(metAt + new Vector3(0f, 4.5f, 0f));
        troop.IsInMelee = true;
        troop.MeleeOpponent = inFront;
        Tick();

        Assert.AreEqual(AIOrderKind.Line, second.Requested.Kind, "keeps a line order while it fights");
        Assert.AreEqual(metAt.x, second.Requested.Position.x, Tolerance, "where it met the enemy (x)");
        Assert.AreEqual(metAt.y, second.Requested.Position.y, Tolerance, "where it met the enemy (y)");
        Assert.AreEqual(ChainLineFormation.ChainPhase.Fighting, chain.Phase);
        Assert.IsTrue(troop.HasLineOrder, "the line order (and so its order links) is kept");
        Assert.AreEqual(AIChainLink.Pending, ((IAIChainTroop)chain.ChainOrder[0].Troop).RightLinkTo(troop), "still linked to its left");
        Assert.AreEqual(AIChainLink.Pending, troop.RightLinkTo((IAIChainTroop)chain.ChainOrder[2].Troop), "and to its right");
    }

    [Test]
    public void TurnsOnAnOpponentAtItsFlank_AndReformsOnceTheFightIsOver()
    {
        ChainLineDefinition def = SetUp(new Vector3(0f, 40f, 0f));
        Tick();
        FakeTroop attacker = AddEnemy(new Vector3(12f, 0.5f, 0f)); // on the right flank
        AITroopAgent end = chain.ChainOrder[3];
        ((FakeTroop)end.Troop).IsInMelee = true;
        ((FakeTroop)end.Troop).MeleeOpponent = attacker;
        Tick();
        Assert.AreEqual(AIOrderKind.Attack, end.Requested.Kind, "turns on it");
        Assert.AreSame(attacker, end.Requested.Target, "the troop at its flank it is in melee with");

        // The attacker is beaten off.
        field.All.Remove(attacker);
        ((FakeTroop)end.Troop).IsInMelee = false;
        ((FakeTroop)end.Troop).MeleeOpponent = null;
        Tick();
        Assert.AreEqual(ChainLineFormation.ChainPhase.Fighting, chain.Phase, "holds a moment before re-forming");
        Tick(def.regroupSeconds);
        Assert.AreEqual(ChainLineFormation.ChainPhase.Forming, chain.Phase, "re-forms");
        foreach (AITroopAgent a in chain.ChainOrder) Assert.AreEqual(AIOrderKind.Line, a.Requested.Kind);
    }

    [Test]
    public void ATroopThatFallsOut_ClosesTheGap()
    {
        SetUp(new Vector3(0f, 40f, 0f));
        Tick();
        Tick();
        FakeTroop lost = (FakeTroop)chain.ChainOrder[1].Troop;
        IAIChainTroop first = (IAIChainTroop)chain.ChainOrder[0].Troop;
        IAIChainTroop third = (IAIChainTroop)chain.ChainOrder[2].Troop;
        lost.IsAlive = false;
        Tick();
        Tick();
        Assert.AreEqual(3, chain.ChainOrder.Count);
        Assert.AreEqual(Width, AIGeometry.FlatDistance(chain.Slots[0], chain.Slots[1]), Tolerance, "end to end again");
        Assert.AreEqual(AIChainLink.Pending, first.RightLinkTo(third), "the troops either side link up");
    }
}
