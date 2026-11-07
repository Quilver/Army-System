using System.Collections.Generic;
using UnityEngine;

// Charge scheduler (spec §4.3, §4.4). Each frame every soldier of a troop within the engagement gap of an enemy troop
// charges with probability 1 - exp(-rate x dt), rate = EffectiveAggression / ChargeWindowSeconds (Melee Tuning, 45 s,
// SU-112), so it makes about Aggression charges per window (Aggression + 5 during Assault). There is no neighbour contagion and no cooldown after a
// charge (Q-29, Q-30).
//   Gate (SU-95): the troop's footprint gap to an enemy troop is within EngagementGap (+ ChargeGapTolerance). Being
//   engaged (A_Distance) is no longer enough: engaged lines that stand off further apart do not charge.
//   Target: one of the ChargeCandidateCount nearest soldiers within the charge reach, at random, of an enemy troop
//   that passes the gate. The reach is A_Distance between the soldiers' edges (TroopTuning.ChargeRange).
//   Followers: the troop's Discipline nearest same-troop soldiers each follow with ChargeFollowProbability. They are
//   passive: they stay near the charger and support its clash, and return to the line when the charge ends.
//   Cancel: the target dies, the charger's troop routs, or ChargeTimeoutSeconds passes (no retargeting, Q-27). A retreat
//   does not cancel a charge: it is completed and the soldier rushes back to its troop (SU-43).
//   Front rank (SU-106, Melee Tuning FrontRankChargesOnly): only the front rank may start charges: one rank's worth of
//   soldiers (the line's soldiers per rank, or the troop's frontage outside a line), those standing furthest toward the
//   enemy troops it may charge. Soldiers out charging still hold their place in that count.
//   Stunned soldiers (SU-107) neither charge nor follow.
// Candidate search uses the contact index (a spatial query), not a loop over every soldier.
public sealed class ChargeCoordinator
{
    readonly SoldierContactIndex _index;
    readonly TroopRegistry _registry;
    readonly List<int> _nearby = new();
    readonly List<(int index, float distance)> _candidates = new();
    readonly List<(SoldierAgentState soldier, float distance)> _friends = new();
    readonly HashSet<Troop> _chargeable = new();
    readonly HashSet<SoldierAgentState> _frontRank = new();
    readonly List<(SoldierAgentState soldier, float forward)> _ranked = new();

    public ChargeCoordinator(SoldierContactIndex index, TroopRegistry registry)
    {
        _index = index;
        _registry = registry;
    }

    public void Update(float deltaTime)
    {
        TickActiveCharges(deltaTime);
        StartNewCharges(deltaTime);
    }

    void TickActiveCharges(float deltaTime)
    {
        foreach (var troop in _registry.Troops)
        {
            if (troop == null) continue;
            bool broken = troop.IsRouted;
            foreach (var soldier in troop.Soldiers)
            {
                if (soldier.ChargeRole == ChargeRole.None) continue;
                soldier.TickCharge(deltaTime);
                if (!soldier.IsAlive || broken || soldier.ChargeTimeRemaining <= 0f)
                {
                    soldier.CancelCharge();
                    continue;
                }

                if (soldier.ChargeRole == ChargeRole.Charger)
                {
                    Troop targetTroop = soldier.ChargeTargetTroop;
                    bool valid = targetTroop != null
                        && targetTroop.TeamId != troop.TeamId
                        && soldier.ChargeTarget != null
                        && soldier.ChargeTarget.IsAlive
                        && targetTroop.Roster.Contains(soldier.ChargeTarget);
                    if (!valid) soldier.CancelCharge();
                }
                else
                {
                    // A follower returns to the line once its charger's charge has ended.
                    SoldierAgentState leader = soldier.ChargeLeader;
                    if (leader == null || !leader.IsAlive || !leader.IsCharging) soldier.CancelCharge();
                }
            }
        }
    }

    void StartNewCharges(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        TroopTuning tuning = TroopTuning.Active;
        float now = Time.time;
        foreach (var troop in _registry.Troops)
        {
            if (troop == null || troop.IsRouted || troop.IsFaultSlipping) continue;
            if (!CollectChargeable(troop, tuning.ChargeGap, _chargeable)) continue;
            bool frontRankOnly = tuning.FrontRankChargesOnly;
            if (frontRankOnly) CollectFrontRank(troop, _chargeable, _frontRank, _ranked);
            ITroopRandom random = troop.TroopRandom;
            IReadOnlyList<SoldierAgentState> soldiers = troop.Soldiers;
            for (int i = 0; i < soldiers.Count; i++)
            {
                SoldierAgentState soldier = soldiers[i];
                if (!soldier.IsAlive || soldier.Character == null || soldier.IsCommitted || soldier.IsReturning
                    || soldier.IsStunned) continue;
                if (frontRankOnly && !_frontRank.Contains(soldier)) continue;

                float chance = ChargeRules.ChargeProbability(troop.EffectiveAggression(soldier.Character),
                    tuning.ChargeWindowSeconds, deltaTime);
                if (random.Value() >= chance) continue;

                Vector2 position = troop.WorldPosition(soldier);
                if (!TryPickTarget(troop, position, tuning.ChargeRange, tuning.ChargeCandidateCount, _chargeable, random,
                        out SoldierContact target))
                    continue;

                soldier.BeginCharge(target.Soldier, target.Troop, tuning.ChargeTimeoutSeconds, now);
                troop.MeleeMetrics.RecordChargeStarted();
                PickFollowers(troop, soldier, position, tuning, random, now);
            }
        }
    }

    // Soldiers in one rank: the troop's line's soldiers per rank, or its frontage when it is not in a line (SU-106).
    public static int FrontRankSize(Troop troop)
    {
        TroopFormationData formation = troop.Formation;
        if (formation == null) return Mathf.Max(1, troop.Frontage);
        return LineGeometry.SoldiersPerRank(Vector2.Distance(formation.Start, formation.End));
    }

    // The front rank facing each chargeable enemy troop (SU-106): the FrontRankSize living soldiers furthest along the
    // direction from this troop's pivot to that enemy's pivot. Charging and returning soldiers are ranked where they
    // stand, so they keep their place in the count while they are away.
    public static void CollectFrontRank(Troop troop, HashSet<Troop> enemies, HashSet<SoldierAgentState> front,
        List<(SoldierAgentState soldier, float forward)> ranked)
    {
        front.Clear();
        int size = FrontRankSize(troop);
        Vector2 pivot = troop.transform.position;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            Vector2 toward = (Vector2)enemy.transform.position - pivot;
            toward = toward.sqrMagnitude > 1e-6f ? toward.normalized : troop.Facing;
            ranked.Clear();
            foreach (var soldier in troop.Soldiers)
                if (soldier.IsAlive) ranked.Add((soldier, Vector2.Dot(troop.WorldPosition(soldier) - pivot, toward)));
            ranked.Sort((a, b) => b.forward.CompareTo(a.forward));
            int count = Mathf.Min(size, ranked.Count);
            for (int i = 0; i < count; i++) front.Add(ranked[i].soldier);
        }
    }

    // Whether the troop may start charges: some enemy troop is within chargeGap of it (footprint gap, this frame's
    // engagement records). Fills chargeable with those enemy troops, the only ones its soldiers charge.
    public static bool CollectChargeable(Troop troop, float chargeGap, HashSet<Troop> chargeable)
    {
        chargeable?.Clear();
        if (troop == null || troop.IsRouted || troop.IsFaultSlipping) return false;
        bool any = false;
        foreach (var record in troop.Engagements)
        {
            if (record.Opponent == null || record.Gap > chargeGap) continue;
            chargeable?.Add(record.Opponent);
            any = true;
        }
        return any;
    }

    // Whether soldiers of the troop may start charges this frame (the gate above).
    public static bool CanStartCharges(Troop troop) => CollectChargeable(troop, TroopTuning.Active.ChargeGap, null);

    // A random pick among the nearest candidateCount soldiers of the chargeable enemy troops within range.
    bool TryPickTarget(Troop troop, Vector2 position, float range, int candidateCount, HashSet<Troop> chargeable,
        ITroopRandom random, out SoldierContact target)
    {
        target = default;
        _index.GatherWithin(position, range, _nearby);
        _candidates.Clear();
        var contacts = _index.Contacts;
        foreach (int i in _nearby)
        {
            SoldierContact contact = contacts[i];
            if (contact.Troop == null || contact.Troop.TeamId == troop.TeamId || !contact.Soldier.IsAlive) continue;
            if (!chargeable.Contains(contact.Troop)) continue;
            _candidates.Add((i, (contact.Position - position).sqrMagnitude));
        }
        if (_candidates.Count == 0) return false;

        _candidates.Sort((a, b) => a.distance.CompareTo(b.distance));
        int pool = Mathf.Min(Mathf.Max(1, candidateCount), _candidates.Count);
        int pick = Mathf.Min(pool - 1, Mathf.FloorToInt(random.Value() * pool));
        target = contacts[_candidates[pick].index];
        return true;
    }

    // The Discipline nearest living, uncommitted soldiers of the same troop (Q-9) each follow with the follow chance.
    void PickFollowers(Troop troop, SoldierAgentState charger, Vector2 chargerPosition, TroopTuning tuning,
        ITroopRandom random, float now)
    {
        int count = Mathf.Max(0, troop.Discipline);
        if (count == 0) return;
        _friends.Clear();
        foreach (var soldier in troop.Soldiers)
        {
            if (soldier == charger || !soldier.IsAlive || soldier.IsCommitted || soldier.IsReturning || soldier.IsStunned) continue;
            _friends.Add((soldier, (troop.WorldPosition(soldier) - chargerPosition).sqrMagnitude));
        }
        _friends.Sort((a, b) => a.distance.CompareTo(b.distance));
        int limit = Mathf.Min(count, _friends.Count);
        for (int i = 0; i < limit; i++)
        {
            if (random.Value() >= tuning.ChargeFollowProbability) continue;
            _friends[i].soldier.BeginFollow(charger, tuning.ChargeTimeoutSeconds, now);
            troop.MeleeMetrics.RecordFollowerJoined();
        }
    }
}
