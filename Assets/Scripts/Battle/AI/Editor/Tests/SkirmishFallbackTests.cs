using System.Collections.Generic;
using LeveragedWars.Battle.AI;
using NUnit.Framework;
using UnityEngine;

// Skirmish fall back (Mission 1 playtest: the archers fell back straight through the cavalry charging them). Falling
// back never routes through or into the threat: in melee the troop stands and fights, a blocked route goes by an escape
// spot first, and nothing changes when the way is clear. Driven with small fakes of the AI interfaces, no real troops.
[Category("AI")]
public sealed class SkirmishFallbackTests
{
    static readonly Vector3 FallbackPoint = new Vector3(0f, 10f, 0f);
    const float Tolerance = 0.01f;

    sealed class FakeTroop : IAITroop
    {
        static int nextId;

        public FakeTroop(AITeam team, Vector3 position, bool ranged, float frontage)
        {
            Id = ++nextId;
            Team = team;
            Position = position;
            IsRanged = ranged;
            Frontage = frontage;
        }

        public int Id { get; }
        public string DisplayName => $"Fake {Id}";
        public AITeam Team { get; }
        public bool IsAlive => true;
        public bool IsRouting => false;
        public Vector3 Position { get; set; }
        public Vector3 Forward => Vector3.right;
        public Vector3 Velocity => Vector3.zero;
        public float Frontage { get; }
        public float RunSpeed => 4f;
        public bool IsRanged { get; }
        public float MaxRange => IsRanged ? 20f : 0f;
        public bool HasAmmo => IsRanged;
        public bool IsInMelee { get; set; }
        public IAITroop MeleeOpponent { get; set; }

        public void OrderHold(Vector3 position, Vector3 facing) { }
        public void OrderMove(Vector3 position, Vector3 facing, bool run) { }
        public void OrderAttack(IAITroop target) { }
        public void OrderCharge(IAITroop target) { }
        public void OrderFallBack(Vector3 position) { }
    }

    sealed class FakeBattlefield : IAIBattlefield
    {
        public readonly List<IAITroop> All = new List<IAITroop>();
        public IReadOnlyList<IAITroop> Troops => All;
        public IReadOnlyList<IAIObjective> Objectives => new List<IAIObjective>();
    }

    GameObject marker;
    FakeBattlefield field;
    AIBlackboard blackboard;
    Detachment detachment;
    FakeTroop archer;
    FakeTroop threat;
    AITroopAgent agent;
    float time;

    [TearDown]
    public void TearDown()
    {
        if (marker != null) Object.DestroyImmediate(marker);
    }

    // Mission 1 style: an archer (frontage 4) skirmishing from a fallback point, one enemy troop.
    SkirmishDefinition SetUp(Vector3 archerAt, Vector3 threatAt, float threatFrontage = 6f, float leash = 30f, float margin = 2f)
    {
        marker = new GameObject("ArcherFallback");
        marker.transform.position = FallbackPoint;
        var def = new SkirmishDefinition
        {
            fallbackPoint = new AnchorReference { kind = AnchorKind.Transform, transform = marker.transform },
            minimumRange = 7f,
            resumeMultiplier = 1.5f,
            maxDistanceFromFallback = leash,
            routeClearanceMargin = margin,
            escapeDistance = 8f,
        };

        archer = new FakeTroop(AITeam.Enemy, archerAt, true, 4f);
        threat = new FakeTroop(AITeam.Player, threatAt, false, threatFrontage);
        field = new FakeBattlefield();
        field.All.Add(archer);
        field.All.Add(threat);

        time = 0f;
        blackboard = new AIBlackboard(AITeam.Enemy, time);
        blackboard.Refresh(field, time);
        detachment = new Detachment("Archers", AITeam.Enemy, 0.5f);
        agent = blackboard.GetOrCreateAgent(archer);
        detachment.AddAgent(agent);
        blackboard.AddDetachment(detachment);
        detachment.CaptureStart();
        detachment.SetFormation(def, blackboard);
        return def;
    }

    void Tick()
    {
        time += 0.25f;
        blackboard.Refresh(field, time);
        detachment.Tick();
    }

    static void AssertAt(Vector3 expected, Vector3 actual, string message)
    {
        Assert.AreEqual(expected.x, actual.x, Tolerance, message + " (x)");
        Assert.AreEqual(expected.y, actual.y, Tolerance, message + " (y)");
    }

    [Test]
    public void ThreatBehind_WayClear_FallsBackToTheFallbackPoint()
    {
        SetUp(Vector3.zero, new Vector3(0f, -5f, 0f));
        Tick();
        Assert.AreEqual(AIOrderKind.FallBack, agent.Requested.Kind);
        AssertAt(FallbackPoint, agent.Requested.Position, "unchanged when nothing is in the way");
    }

    [Test]
    public void ThreatBetween_MovesAwayFromIt_NotThroughIt()
    {
        SetUp(Vector3.zero, new Vector3(0f, 4f, 0f));
        Tick();
        Assert.AreEqual(AIOrderKind.FallBack, agent.Requested.Kind);
        Vector3 spot = agent.Requested.Position;
        Assert.Greater(AIGeometry.FlatDistance(spot, FallbackPoint), 1f, "not the fallback point behind the threat");
        AssertAt(new Vector3(0f, -8f, 0f), spot, "Escape Distance straight away from the threat");
        Assert.GreaterOrEqual(AIGeometry.DistanceToSegment(threat.Position, archer.Position, spot), 4f - Tolerance,
            "the way there never closes on the threat");
    }

    [Test]
    public void ThreatToOneSide_EscapesOnTheOtherSide_ThenHeadsForTheFallbackPointOnceClear()
    {
        // Small clearance (half-widths 2 + 1, no margin = 3) so a sidestep clears the route.
        SetUp(Vector3.zero, new Vector3(1f, 3f, 0f), threatFrontage: 2f, margin: 0f);
        Tick();

        Assert.AreEqual(AIOrderKind.FallBack, agent.Requested.Kind);
        Vector3 spot = agent.Requested.Position;
        Assert.Less(spot.x, -1f, "escapes on the side away from the threat (it stands to the right of the route)");
        Assert.GreaterOrEqual(AIGeometry.DistanceToSegment(threat.Position, spot, FallbackPoint), 3f,
            "from the escape spot the way to the fallback point keeps clear of the threat");

        archer.Position = spot;
        Tick();
        Assert.AreEqual(AIOrderKind.FallBack, agent.Requested.Kind);
        AssertAt(FallbackPoint, agent.Requested.Position, "heads for the fallback point once the way is clear");
    }

    [Test]
    public void InMelee_StandsAndFights_NoFallBack()
    {
        SetUp(Vector3.zero, new Vector3(0f, 4f, 0f));
        archer.IsInMelee = true;
        archer.MeleeOpponent = threat;
        Tick();
        Assert.AreEqual(AIOrderKind.Attack, agent.Requested.Kind, "fights back instead of falling back into the attacker");
        Assert.AreSame(threat, agent.Requested.Target);
        Assert.AreEqual(DetachmentState.Engaged, detachment.State);
    }

    [Test]
    public void ThreatBetween_AtTheLeash_EscapeSpotStaysInsideTheLeashAndClearOfTheThreat()
    {
        const float leash = 12f;
        SetUp(new Vector3(0f, -1f, 0f), new Vector3(0f, 3f, 0f), leash: leash);
        Tick();
        // Straight away is clamped back to the archer's feet by the leash, so it slides round the leash instead.
        Assert.AreEqual(AIOrderKind.FallBack, agent.Requested.Kind);
        Vector3 spot = agent.Requested.Position;
        Assert.LessOrEqual(AIGeometry.FlatDistance(spot, FallbackPoint), leash + Tolerance, "inside the leash");
        Assert.Greater(AIGeometry.FlatDistance(spot, FallbackPoint), 1f, "not the fallback point behind the threat");
        Assert.Greater(AIGeometry.FlatDistance(spot, archer.Position), 1.5f, "an actual move, not standing still");
        // Sliding round the leash may pass the threat at almost the current distance (4), never closer than that.
        Assert.GreaterOrEqual(AIGeometry.DistanceToSegment(threat.Position, archer.Position, spot), 3.85f,
            "the way there never closes on the threat");
        Assert.Greater(AIGeometry.FlatDistance(spot, threat.Position), 4f, "ends further from the threat");
    }

    [Test]
    public void Geometry_DistanceToSegmentAndClampToCircle()
    {
        Assert.AreEqual(2f, AIGeometry.DistanceToSegment(new Vector3(2f, 5f, 0f), Vector3.zero, new Vector3(0f, 10f, 0f)), 1e-4f);
        Assert.AreEqual(5f, AIGeometry.DistanceToSegment(new Vector3(3f, -4f, 0f), Vector3.zero, new Vector3(0f, 10f, 0f)), 1e-4f,
            "beyond the start: distance to the start");
        AssertAt(new Vector3(0f, 3f, 0f), AIGeometry.ClampToCircle(new Vector3(0f, 8f, 0f), Vector3.zero, 3f), "pulled in");
        AssertAt(new Vector3(1f, 1f, 0f), AIGeometry.ClampToCircle(new Vector3(1f, 1f, 0f), Vector3.zero, 3f), "inside: unchanged");
        AssertAt(new Vector3(0f, 1f, 0f), AIGeometry.Rotate(Vector3.right, 90f), "anticlockwise");
    }
}
