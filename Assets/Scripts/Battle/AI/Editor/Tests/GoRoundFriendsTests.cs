using System.Collections.Generic;
using LeveragedWars.Battle.AI;
using NUnit.Framework;
using UnityEngine;

// Going round friends (SU-179, Movie_024: the AI's huskarls pushed through their own spear chain at the enemy fighting
// it). A melee troop whose straight way to its target crosses a friendly troop goes round the friends instead, by the
// nearer end; nothing changes when the way is clear or wide enough between friends. Geometry on plain footprints, and the
// troop layer on small fakes of the AI interfaces, no real troops.
[Category("AI")]
public sealed class GoRoundFriendsTests
{
    const float Clearance = 1.5f;
    const float SpearWidth = 5.4f;   // a 16-soldier troop's natural line length
    const float SpearHalfDepth = 1.5f;

    // Four spear troops end to end along y = 0 (x from -10.8 to 10.8), facing +y, as the AI's chain stands.
    static List<AIFootprint> Chain()
    {
        var chain = new List<AIFootprint>();
        for (int i = 0; i < 4; i++)
            chain.Add(new AIFootprint(new Vector3((i - 1.5f) * SpearWidth, 0f, 0f), Vector3.up, SpearWidth * 0.5f, SpearHalfDepth));
        return chain;
    }

    // Follows the detours from a start to the target as a troop would, waypoint by waypoint. Every leg must stay out of
    // every friend's footprint. Returns the corners taken.
    static List<Vector3> Walk(Vector3 from, Vector3 to, List<AIFootprint> friends)
    {
        var corners = new List<Vector3>();
        Vector3 at = from;
        for (int step = 0; step < 8; step++)
        {
            if (!AIApproach.TryFindDetour(at, to, friends, Clearance, out Vector3 waypoint)) break;
            AssertClear(at, waypoint, friends, $"leg {step} to {waypoint}");
            corners.Add(waypoint);
            at = waypoint;
        }
        Assert.Less(corners.Count, 8, "the way round ends");
        AssertClear(at, to, friends, "last leg to the target");
        return corners;
    }

    static void AssertClear(Vector3 a, Vector3 b, List<AIFootprint> friends, string what)
    {
        foreach (AIFootprint f in friends)
            Assert.IsFalse(AIApproach.SegmentEntry(a, b, f, 0f, out _), $"{what} does not go through the friend at {f.Centre}");
    }

    // The huskarls' case: behind the middle of the chain, the target an enemy fighting the chain's front. The way goes
    // round one end of the chain (rear corner, then front corner), never between its troops, then straight in.
    [Test]
    public void BehindTheChain_GoesRoundAnEndOfIt_NotThroughIt()
    {
        List<AIFootprint> chain = Chain();
        Vector3 reserve = new Vector3(1f, -9f, 0f);
        Vector3 enemy = new Vector3(0.3f, 4.5f, 0f); // its pivot a few units ahead of the chain's front

        Assert.IsTrue(AIApproach.TryFindDetour(reserve, enemy, chain, Clearance, out Vector3 first), "the straight way is blocked");
        float chainEnd = 2f * SpearWidth;
        Assert.Greater(Mathf.Abs(first.x), chainEnd + Clearance, "the first corner is beyond an end of the chain");
        Assert.Less(first.y, -SpearHalfDepth - Clearance, "and behind it");

        List<Vector3> corners = Walk(reserve, enemy, chain);
        Assert.AreEqual(2, corners.Count, "round the rear corner, then the front corner");
        Assert.Greater(corners[1].y, SpearHalfDepth + Clearance, "the second corner is ahead of the chain");
        Assert.AreEqual(Mathf.Sign(corners[0].x), Mathf.Sign(corners[1].x), "both on the same end");
    }

    [Test]
    public void BehindOneEnd_GoesRoundThatEnd()
    {
        List<AIFootprint> chain = Chain();
        Assert.IsTrue(AIApproach.TryFindDetour(new Vector3(7f, -9f, 0f), new Vector3(2f, 4.5f, 0f), chain, Clearance,
            out Vector3 right));
        Assert.Greater(right.x, 0f, "behind the right half: round the right end");
        Assert.IsTrue(AIApproach.TryFindDetour(new Vector3(-7f, -9f, 0f), new Vector3(-2f, 4.5f, 0f), chain, Clearance,
            out Vector3 left));
        Assert.Less(left.x, 0f, "behind the left half: round the left end");
    }

    [Test]
    public void AClearWay_NeedsNoDetour()
    {
        List<AIFootprint> chain = Chain();
        // Beside the chain's end, the target ahead of that end's flank.
        Assert.IsFalse(AIApproach.TryFindDetour(new Vector3(16f, -3f, 0f), new Vector3(14f, 6f, 0f), chain, Clearance, out _));
        // Already ahead of the chain.
        Assert.IsFalse(AIApproach.TryFindDetour(new Vector3(-3f, 7f, 0f), new Vector3(0f, 4.5f, 0f), chain, Clearance, out _));
    }

    // Two friends with a gap wider than twice the clearance (plus the troop) between them: the troop goes through the gap.
    // Close the gap below that and it goes round both.
    [Test]
    public void AWideGapBetweenFriends_IsUsed_ANarrowOneIsNot()
    {
        var wide = new List<AIFootprint>
        {
            new AIFootprint(new Vector3(-6f, 0f, 0f), Vector3.up, 2.7f, SpearHalfDepth),
            new AIFootprint(new Vector3(6f, 0f, 0f), Vector3.up, 2.7f, SpearHalfDepth),
        };
        Assert.IsFalse(AIApproach.TryFindDetour(new Vector3(0f, -8f, 0f), new Vector3(0f, 6f, 0f), wide, Clearance, out _),
            "a 6.6-unit gap: straight through it");

        var narrow = new List<AIFootprint>
        {
            new AIFootprint(new Vector3(-3.5f, 0f, 0f), Vector3.up, 2.7f, SpearHalfDepth),
            new AIFootprint(new Vector3(3.5f, 0f, 0f), Vector3.up, 2.7f, SpearHalfDepth),
        };
        Assert.IsTrue(AIApproach.TryFindDetour(new Vector3(0f, -8f, 0f), new Vector3(0f, 6f, 0f), narrow, Clearance,
            out Vector3 waypoint), "a 1.6-unit gap is too narrow");
        Assert.Greater(Mathf.Abs(waypoint.x), 6.2f + Clearance, "round the outside of both");
        Walk(new Vector3(0f, -8f, 0f), new Vector3(0f, 6f, 0f), narrow);
    }

    // A bent chain (the middle troops pushed back a little in the fight) is still gone round as a whole.
    [Test]
    public void ABentChain_IsGoneRoundAsAWhole()
    {
        var chain = new List<AIFootprint>
        {
            new AIFootprint(new Vector3(-8.1f, 0f, 0f), Vector3.up, 2.7f, SpearHalfDepth),
            new AIFootprint(new Vector3(-2.7f, -1.2f, 0f), AIGeometry.Rotate(Vector3.up, 10f), 2.7f, SpearHalfDepth),
            new AIFootprint(new Vector3(2.7f, -1.6f, 0f), AIGeometry.Rotate(Vector3.up, -8f), 2.7f, SpearHalfDepth),
            new AIFootprint(new Vector3(8.1f, -0.4f, 0f), Vector3.up, 2.7f, SpearHalfDepth),
        };
        List<Vector3> corners = Walk(new Vector3(0f, -10f, 0f), new Vector3(0.5f, 4.5f, 0f), chain);
        Assert.GreaterOrEqual(corners.Count, 2);
        Assert.Greater(Mathf.Abs(corners[0].x), 10.8f, "round an end, not between two troops");
    }

    // ---- The troop layer ----

    sealed class FakeTroop : IAITroop
    {
        static int nextId;

        public FakeTroop(AITeam team, Vector3 position, float frontage, Vector3 forward)
        {
            Id = ++nextId;
            Team = team;
            Position = position;
            Frontage = frontage;
            Forward = forward;
        }

        public int Id { get; }
        public string DisplayName => $"Fake {Id}";
        public AITeam Team { get; }
        public bool IsAlive => true;
        public bool IsRouting => false;
        public Vector3 Position { get; set; }
        public Vector3 Forward { get; }
        public Vector3 Velocity => Vector3.zero;
        public float Frontage { get; }
        public float RunSpeed => 4f;
        public bool IsRanged => false;
        public float MaxRange => 0f;
        public bool HasAmmo => false;
        public bool IsInMelee { get; set; }
        public IAITroop MeleeOpponent { get; set; }

        public AIOrderKind LastOrder;
        public Vector3 LastPosition;
        public IAITroop LastTarget;

        public void OrderHold(Vector3 position, Vector3 facing) { LastOrder = AIOrderKind.Hold; LastPosition = position; }
        public void OrderMove(Vector3 position, Vector3 facing, bool run) { LastOrder = AIOrderKind.Move; LastPosition = position; }
        public void OrderAttack(IAITroop target) { LastOrder = AIOrderKind.Attack; LastTarget = target; }
        public void OrderCharge(IAITroop target) { LastOrder = AIOrderKind.Charge; LastTarget = target; }
        public void OrderFallBack(Vector3 position) { LastOrder = AIOrderKind.FallBack; LastPosition = position; }
    }

    sealed class FakeBattlefield : IAIBattlefield
    {
        public readonly List<IAITroop> All = new List<IAITroop>();
        public IReadOnlyList<IAITroop> Troops => All;
        public IReadOnlyList<IAIObjective> Objectives => new List<IAIObjective>();
    }

    // A chain of four friendly spear troops along y = 0 facing the enemy (+y), the enemy just ahead of it, and a reserve
    // troop at the given place, told to attack that enemy. Returns the reserve after one troop tick.
    static FakeTroop Reserve(Vector3 at, TroopReflexSettings settings, out AITroopAgent agent, out FakeTroop enemy)
    {
        var field = new FakeBattlefield();
        for (int i = 0; i < 4; i++)
            field.All.Add(new FakeTroop(AITeam.Enemy, new Vector3((i - 1.5f) * SpearWidth, 0f, 0f), SpearWidth, Vector3.up));
        enemy = new FakeTroop(AITeam.Player, new Vector3(0.3f, 4.5f, 0f), SpearWidth, Vector3.down);
        var reserve = new FakeTroop(AITeam.Enemy, at, 4.5f, Vector3.up);
        field.All.Add(enemy);
        field.All.Add(reserve);
        var blackboard = new AIBlackboard(AITeam.Enemy, 0f);
        blackboard.Refresh(field, 0f);
        agent = blackboard.GetOrCreateAgent(reserve);
        agent.Attack(enemy);
        agent.Tick(blackboard, settings);
        return reserve;
    }

    [Test]
    public void AReserveSentAtAnEnemyBeyondItsOwnChain_MovesRoundIt()
    {
        FakeTroop reserve = Reserve(new Vector3(1f, -9f, 0f), new TroopReflexSettings(), out AITroopAgent agent, out _);
        Assert.AreEqual(AIOrderKind.Move, reserve.LastOrder, "it moves to the way round instead of attacking");
        Assert.AreEqual("go round", agent.Reflex);
        Assert.Greater(Mathf.Abs(reserve.LastPosition.x), 2f * SpearWidth + Clearance, "to a corner beyond the chain's end");
    }

    [Test]
    public void WithGoRoundFriendsOff_TheReserveAttacksStraight()
    {
        FakeTroop reserve = Reserve(new Vector3(1f, -9f, 0f), new TroopReflexSettings { goRoundFriends = false }, out _,
            out FakeTroop enemy);
        Assert.AreEqual(AIOrderKind.Attack, reserve.LastOrder);
        Assert.AreSame(enemy, reserve.LastTarget);
    }

    [Test]
    public void AReserveWithAClearWay_AttacksStraight()
    {
        FakeTroop reserve = Reserve(new Vector3(15f, 3f, 0f), new TroopReflexSettings(), out AITroopAgent agent,
            out FakeTroop enemy);
        Assert.AreEqual(AIOrderKind.Attack, reserve.LastOrder, "beside the chain's end the way is clear");
        Assert.AreSame(enemy, reserve.LastTarget);
        Assert.IsNull(agent.Reflex);
    }
}
